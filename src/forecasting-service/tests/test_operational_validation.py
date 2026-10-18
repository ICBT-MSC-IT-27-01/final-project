from datetime import date, timedelta

import pytest

from forecasting_service.evaluation.operational_validation import evaluate_run


def _forecast() -> dict:
    origin = date(2026, 10, 16)
    return {
        "forecastRunId": "test-run",
        "modelVersion": "v1",
        "forecastDate": origin.isoformat(),
        "createdAt": "2026-10-16T12:00:00Z",
        "dailyForecasts": [
            {
                "targetDate": (origin + timedelta(days=offset)).isoformat(),
                "rainfallMm": 2.0,
                "temperatureC": 28.0,
                "humidityPercent": 70.0,
            }
            for offset in range(1, 8)
        ],
    }


def _observations() -> dict:
    origin = date(2026, 10, 16)
    return {
        (origin + timedelta(days=offset)).isoformat(): {
            "Rainfall": 1.0 if offset else 0.0,
            "Temperature": 29.0 if offset else 27.0,
            "Humidity": 72.0 if offset else 65.0,
        }
        for offset in range(8)
    }


def _evaluate(forecast: dict, observations: dict, source_kind: str = "station") -> dict:
    return evaluate_run(
        forecast,
        observations,
        source_name="Example source",
        source_reference="example-record",
        source_kind=source_kind,
        location="Example Anuradhapura site",
    )


def test_complete_station_run_reports_separate_model_and_baseline_metrics() -> None:
    report = _evaluate(_forecast(), _observations())

    assert report["status"] == "COMPLETE_SINGLE_RUN"
    assert report["pairedTargetDays"] == 7
    assert report["metrics"]["Rainfall"]["model"]["mae"] == 1.0
    assert report["metrics"]["Rainfall"]["persistenceBaseline"]["mae"] == 1.0
    assert report["metrics"]["Temperature"]["model"]["rmse"] == 1.0
    assert report["metrics"]["Temperature"]["persistenceBaseline"]["rmse"] == 2.0


def test_incomplete_run_does_not_publish_accuracy_metrics() -> None:
    observations = _observations()
    del observations["2026-10-23"]

    report = _evaluate(_forecast(), observations)

    assert report["status"] == "INCOMPLETE"
    assert report["pairedTargetDays"] == 6
    assert report["missingDates"] == ["2026-10-23"]
    assert report["metrics"] is None


def test_reanalysis_is_not_labelled_as_independent_observation_validation() -> None:
    report = _evaluate(_forecast(), _observations(), "reanalysis")

    assert "not independent measured-observation validation" in report["interpretation"]


def test_nonconsecutive_forecast_dates_are_rejected() -> None:
    forecast = _forecast()
    forecast["dailyForecasts"][3]["targetDate"] = "2026-10-22"

    with pytest.raises(ValueError, match="consecutive"):
        _evaluate(forecast, _observations())


def test_late_issued_run_cannot_claim_full_forward_forecast_skill() -> None:
    forecast = _forecast()
    forecast["createdAt"] = "2026-10-17T15:38:02Z"

    report = _evaluate(forecast, _observations())

    assert report["status"] == "NOT_FULLY_FORWARD_ISSUED"
    assert report["notFullyForwardIssuedDates"] == ["2026-10-17"]
    assert report["metrics"] is None
