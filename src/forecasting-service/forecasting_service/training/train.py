from pathlib import Path
import random

import joblib
import numpy as np

from forecasting_service.artifacts import build_metadata, save_metadata
from forecasting_service.config.training_config import TrainingConfig
from forecasting_service.evaluation.metrics import regression_metrics
from forecasting_service.ingestion.csv_ingestion import load_weather_csv
from forecasting_service.ingestion.profiling import create_quality_report
from forecasting_service.models.baseline import persistence_forecast
from forecasting_service.models.lstm_model import build_lstm_model
from forecasting_service.preprocessing.cleaner import clean_weather_data
from forecasting_service.preprocessing.scaler import MinMaxFeatureScaler
from forecasting_service.preprocessing.sequences import create_sequences_for_target_period
from forecasting_service.preprocessing.split import date_based_split, verify_date_continuity


def train_from_config(config: TrainingConfig) -> dict:
    if config.dataset_path is None:
        raise ValueError("An approved dataset_path is required before final training.")
    config.validate()
    _set_seeds(config.random_seed)

    raw = load_weather_csv(config.dataset_path)
    profile = create_quality_report(raw, config.max_interpolation_gap_days).to_dict()
    cleaned, preprocessing_metadata = clean_weather_data(raw, config.max_interpolation_gap_days)
    continuity = verify_date_continuity(cleaned, config.train_start, config.test_end)
    if not continuity["is_continuous"]:
        raise ValueError(f"Dataset date continuity failed: {continuity}")
    train, validation, test, split_metadata = date_based_split(
        cleaned,
        train_start=config.train_start,
        train_end=config.train_end,
        validation_start=config.validation_start,
        validation_end=config.validation_end,
        test_start=config.test_start,
        test_end=config.test_end,
    )

    scaler = MinMaxFeatureScaler().fit(train[list(config.feature_columns)])
    full_scaled = scaler.transform(cleaned[list(config.feature_columns)])

    x_train, y_train, train_target_dates = create_sequences_for_target_period(
        full_scaled,
        cleaned["Date"],
        config.lookback_days,
        config.forecast_horizon_days,
        config.train_start,
        config.train_end,
    )
    x_validation, y_validation, validation_target_dates = create_sequences_for_target_period(
        full_scaled,
        cleaned["Date"],
        config.lookback_days,
        config.forecast_horizon_days,
        config.validation_start,
        config.validation_end,
    )
    x_test, y_test, test_target_dates = create_sequences_for_target_period(
        full_scaled,
        cleaned["Date"],
        config.lookback_days,
        config.forecast_horizon_days,
        config.test_start,
        config.test_end,
    )
    if min(len(x_train), len(x_validation), len(x_test)) == 0:
        raise ValueError("One or more splits is too small for the configured lookback/horizon.")

    baseline_predictions = persistence_forecast(x_test, config.forecast_horizon_days)
    y_test_original = _inverse_sequence(scaler, y_test)
    baseline_original = _inverse_sequence(scaler, baseline_predictions)
    baseline_metrics = regression_metrics(y_test_original, baseline_original, config.feature_columns)

    model = build_lstm_model(config)
    import tensorflow as tf

    callbacks = [
        tf.keras.callbacks.EarlyStopping(
            monitor="val_loss",
            patience=config.early_stopping_patience,
            restore_best_weights=True,
        )
    ]
    history = model.fit(
        x_train,
        y_train,
        validation_data=(x_validation, y_validation),
        epochs=config.epochs,
        batch_size=config.batch_size,
        callbacks=callbacks,
        shuffle=False,
        verbose=1,
    )
    lstm_predictions = model.predict(x_test)
    lstm_original = _inverse_sequence(scaler, lstm_predictions)
    lstm_metrics = regression_metrics(y_test_original, lstm_original, config.feature_columns)

    output_dir = Path(config.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)
    model_path = output_dir / f"{config.model_version}.keras"
    scaler_path = output_dir / f"{config.model_version}_scaler.joblib"
    metadata_path = output_dir / f"{config.model_version}_metadata.json"
    evaluation_path = output_dir / f"{config.model_version}_evaluation.json"
    history_path = output_dir / f"{config.model_version}_training_history.json"
    sample_path = output_dir / f"{config.model_version}_sample_forecast.json"
    model.save(model_path)
    joblib.dump(scaler.scaler, scaler_path)

    sample_forecast = _sample_forecast(test_target_dates[0], lstm_original[0], y_test_original[0], config.feature_columns)
    evaluation_results = {
        "baseline_metrics": baseline_metrics,
        "lstm_metrics": lstm_metrics,
        "test_target_date_range": {
            "first": test_target_dates[0][0],
            "last": test_target_dates[-1][-1],
        },
        "sample_historical_test_forecast": sample_forecast,
    }
    save_metadata(evaluation_results, evaluation_path)
    save_metadata(history.history, history_path)

    metadata = build_metadata(
        model_version=config.model_version,
        dataset_path=str(config.dataset_path),
        dataset_profile=profile,
        split_metadata=split_metadata,
        config=config.to_dict(),
        preprocessing_metadata={**preprocessing_metadata, "date_continuity": continuity},
        baseline_metrics=baseline_metrics,
        lstm_metrics=lstm_metrics,
        artifact_paths={
            "model": str(model_path),
            "scaler": str(scaler_path),
            "metadata": str(metadata_path),
            "evaluation": str(evaluation_path),
            "training_history": str(history_path),
            "sample_forecast": str(sample_path),
        },
        sequence_metadata={
            "train_sequence_count": len(x_train),
            "validation_sequence_count": len(x_validation),
            "test_sequence_count": len(x_test),
            "train_target_date_range": {"first": train_target_dates[0][0], "last": train_target_dates[-1][-1]},
            "validation_target_date_range": {"first": validation_target_dates[0][0], "last": validation_target_dates[-1][-1]},
            "test_target_date_range": {"first": test_target_dates[0][0], "last": test_target_dates[-1][-1]},
            "input_shape": list(x_train.shape[1:]),
            "output_shape": list(y_train.shape[1:]),
            "boundary_context_rule": "Inputs may include immediately preceding historical rows; targets remain inside the approved split date range.",
        },
    )
    save_metadata(metadata, metadata_path)
    save_metadata(sample_forecast, sample_path)
    return metadata


def _set_seeds(seed: int) -> None:
    random.seed(seed)
    np.random.seed(seed)
    try:
        import tensorflow as tf

        tf.random.set_seed(seed)
    except ImportError:
        pass


def _inverse_sequence(scaler: MinMaxFeatureScaler, values: np.ndarray) -> np.ndarray:
    sample_count, horizon, feature_count = values.shape
    flat = values.reshape(sample_count * horizon, feature_count)
    original = scaler.inverse_transform(flat)
    return original.reshape(sample_count, horizon, feature_count)


def _sample_forecast(
    target_dates: list[str],
    predictions: np.ndarray,
    actuals: np.ndarray,
    feature_columns: tuple[str, ...],
) -> list[dict]:
    rows = []
    for index, target_date in enumerate(target_dates):
        rows.append(
            {
                "target_date": target_date,
                "predicted": {
                    feature: float(predictions[index, feature_index])
                    for feature_index, feature in enumerate(feature_columns)
                },
                "actual": {
                    feature: float(actuals[index, feature_index])
                    for feature_index, feature in enumerate(feature_columns)
                },
            }
        )
    return rows
