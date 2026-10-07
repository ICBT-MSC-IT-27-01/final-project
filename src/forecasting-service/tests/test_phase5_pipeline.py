from pathlib import Path

import numpy as np
import pandas as pd
import pytest

from forecasting_service.artifacts import build_metadata
from forecasting_service.config.training_config import SplitConfig
from forecasting_service.evaluation.metrics import regression_metrics
from forecasting_service.ingestion.csv_ingestion import load_weather_csv
from forecasting_service.ingestion.profiling import create_quality_report
from forecasting_service.models.baseline import persistence_forecast
from forecasting_service.preprocessing.cleaner import clean_weather_data
from forecasting_service.preprocessing.scaler import MinMaxFeatureScaler
from forecasting_service.preprocessing.sequences import create_direct_multistep_sequences
from forecasting_service.preprocessing.sequences import create_sequences_for_target_period
from forecasting_service.preprocessing.split import chronological_split, date_based_split, verify_date_continuity


def _sample_daily_data(days: int = 60) -> pd.DataFrame:
    dates = pd.date_range("2020-01-01", periods=days, freq="D")
    return pd.DataFrame(
        {
            "Date": dates,
            "Rainfall": np.arange(days, dtype=float),
            "Temperature": 25 + np.arange(days, dtype=float) * 0.1,
            "Humidity": 70 + np.arange(days, dtype=float) * 0.05,
        }
    )


def test_input_schema_validation_rejects_missing_column(tmp_path: Path) -> None:
    path = tmp_path / "weather.csv"
    pd.DataFrame({"Date": ["2020-01-01"], "Rainfall": [1], "Temperature": [25]}).to_csv(path, index=False)

    with pytest.raises(ValueError, match="missing required source columns"):
        load_weather_csv(path)


def test_quality_report_detects_date_order_missing_and_duplicates() -> None:
    data = _sample_daily_data(5)
    data.loc[2, "Humidity"] = np.nan
    data = pd.concat([data, data.iloc[[1]]], ignore_index=True)

    report = create_quality_report(data)

    assert report.row_count == 6
    assert report.duplicate_dates == 1
    assert report.missing_values["Humidity"] == 1
    assert report.date_range_start == "2020-01-01"
    assert report.date_range_end == "2020-01-05"


def test_cleaner_interpolates_small_gaps_and_preserves_extremes() -> None:
    data = _sample_daily_data(8)
    data.loc[2, "Rainfall"] = np.nan
    data.loc[4, "Rainfall"] = 500

    cleaned, metadata = clean_weather_data(data, max_interpolation_gap_days=1)

    assert cleaned["Rainfall"].isna().sum() == 0
    assert 500 in cleaned["Rainfall"].to_list()
    assert metadata["max_interpolation_gap_days"] == 1


def test_chronological_split_preserves_order() -> None:
    train, validation, test, metadata = chronological_split(_sample_daily_data(100), SplitConfig(0.6, 0.2, 0.2))

    assert train["Date"].max() < validation["Date"].min()
    assert validation["Date"].max() < test["Date"].min()
    assert metadata["train"]["count"] == 60
    assert metadata["validation"]["count"] == 20
    assert metadata["test"]["count"] == 20


def test_sequence_creation_has_expected_lookback_horizon_shapes() -> None:
    values = _sample_daily_data(45)[["Rainfall", "Temperature", "Humidity"]]

    inputs, targets = create_direct_multistep_sequences(values, lookback_days=30, forecast_horizon_days=7)

    assert inputs.shape == (9, 30, 3)
    assert targets.shape == (9, 7, 3)


def test_scaler_fit_only_uses_training_data() -> None:
    train = pd.DataFrame({"Rainfall": [0, 10], "Temperature": [20, 30], "Humidity": [50, 60]})
    test = pd.DataFrame({"Rainfall": [20], "Temperature": [40], "Humidity": [70]})

    scaler = MinMaxFeatureScaler().fit(train)
    transformed = scaler.transform(test)

    assert scaler.scaler is not None
    assert scaler.scaler.data_max_.tolist() == [10, 30, 60]
    assert transformed[0, 0] == 2


def test_baseline_forecast_shape() -> None:
    inputs = np.ones((4, 30, 3))

    predictions = persistence_forecast(inputs, forecast_horizon_days=7)

    assert predictions.shape == (4, 7, 3)


def test_metric_calculations_include_allowed_outputs_only() -> None:
    y_true = np.zeros((2, 7, 3))
    y_pred = np.ones((2, 7, 3))

    metrics = regression_metrics(y_true, y_pred, ("Rainfall", "Temperature", "Humidity"))

    assert metrics["overall_mae"] == 1
    assert metrics["overall_rmse"] == 1
    assert set(metrics["per_variable"].keys()) == {"Rainfall", "Temperature", "Humidity"}
    assert "day_7" in metrics["per_horizon_day"]
    assert "mape" not in metrics


def test_metadata_creation_allows_missing_real_metrics() -> None:
    metadata = build_metadata(
        model_version="v1",
        dataset_path=None,
        dataset_profile={"status": "missing"},
        split_metadata={},
        config={"lookback_days": 30, "forecast_horizon_days": 7},
        preprocessing_metadata={},
        baseline_metrics=None,
        lstm_metrics=None,
    )

    assert metadata["model_version"] == "v1"
    assert metadata["baseline_metrics"] is None
    assert metadata["lstm_metrics"] is None


def test_approved_date_split_boundaries_and_counts() -> None:
    data = pd.DataFrame(
        {
            "Date": pd.date_range("2010-01-01", "2025-12-31", freq="D"),
            "Rainfall": 0.0,
            "Temperature": 25.0,
            "Humidity": 75.0,
        }
    )

    train, validation, test, metadata = date_based_split(
        data,
        train_start="2010-01-01",
        train_end="2021-12-31",
        validation_start="2022-01-01",
        validation_end="2023-12-31",
        test_start="2024-01-01",
        test_end="2025-12-31",
    )

    assert len(train) == 4383
    assert len(validation) == 730
    assert len(test) == 731
    assert train["Date"].max() < validation["Date"].min()
    assert validation["Date"].max() < test["Date"].min()
    assert metadata["train"]["start"] == "2010-01-01"
    assert metadata["test"]["end"] == "2025-12-31"


def test_date_continuity_reports_missing_dates() -> None:
    data = _sample_daily_data(10).drop(index=[3])

    continuity = verify_date_continuity(data, "2020-01-01", "2020-01-10")

    assert not continuity["is_continuous"]
    assert continuity["missing_count"] == 1
    assert continuity["missing_dates"] == ["2020-01-04"]


def test_target_period_sequences_keep_targets_inside_split_with_prior_context() -> None:
    data = _sample_daily_data(50)
    values = data[["Rainfall", "Temperature", "Humidity"]]

    inputs, targets, target_dates = create_sequences_for_target_period(
        values,
        data["Date"],
        lookback_days=30,
        forecast_horizon_days=7,
        target_start="2020-02-10",
        target_end="2020-02-19",
    )

    assert inputs.shape[1:] == (30, 3)
    assert targets.shape[1:] == (7, 3)
    assert target_dates[0][0] == "2020-02-10"
    assert target_dates[-1][-1] == "2020-02-19"


def test_metadata_records_phase5b_artifact_and_sequence_fields() -> None:
    metadata = build_metadata(
        model_version="v1",
        dataset_path="docs/anuradhapura_weather_2010_2025_era5.csv",
        dataset_profile={"row_count": 5844},
        split_metadata={"train": {"count": 4383}},
        config={"lookback_days": 30, "forecast_horizon_days": 7},
        preprocessing_metadata={"date_continuity": {"is_continuous": True}},
        baseline_metrics={"overall_mae": 1.0, "overall_rmse": 1.0},
        lstm_metrics={"overall_mae": 0.9, "overall_rmse": 0.9},
        artifact_paths={"model": "artifacts/v1.keras", "scaler": "artifacts/v1_scaler.joblib"},
        sequence_metadata={"test_sequence_count": 725},
    )

    assert metadata["artifact_paths"]["model"].endswith(".keras")
    assert metadata["artifact_paths"]["scaler"].endswith(".joblib")
    assert metadata["sequence_metadata"]["test_sequence_count"] == 725
    assert metadata["package_versions"]["keras"] is not None or metadata["package_versions"]["tensorflow"] is None
