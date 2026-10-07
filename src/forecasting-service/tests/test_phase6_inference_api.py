from __future__ import annotations

from datetime import date, timedelta
import json
from pathlib import Path

import numpy as np
import pytest
from fastapi.testclient import TestClient

from forecasting_service.api import app, get_predictor
from forecasting_service.inference.predictor import (
    ForecastValidationError,
    WeatherForecastPredictor,
    WeatherObservation,
)


class IdentityScaler:
    def transform(self, values):
        return np.asarray(values, dtype=float)

    def inverse_transform(self, values):
        return np.asarray(values, dtype=float)


class FixedModel:
    def predict(self, values, verbose=0):
        assert values.shape == (1, 30, 3)
        rows = []
        for index in range(7):
            rows.append([10 + index, 25 + index, 80 + index])
        return np.asarray([rows], dtype=float)


@pytest.fixture
def predictor(tmp_path: Path) -> WeatherForecastPredictor:
    model_path = tmp_path / "v1.keras"
    scaler_path = tmp_path / "v1_scaler.joblib"
    metadata_path = tmp_path / "v1_metadata.json"
    model_path.write_text("test-model", encoding="utf-8")
    scaler_path.write_text("test-scaler", encoding="utf-8")
    metadata_path.write_text(
        json.dumps(
            {
                "model_version": "v1",
                "config": {
                    "feature_columns": ["Rainfall", "Temperature", "Humidity"],
                    "lookback_days": 30,
                    "forecast_horizon_days": 7,
                },
            }
        ),
        encoding="utf-8",
    )
    return WeatherForecastPredictor(
        model_path=model_path,
        scaler_path=scaler_path,
        metadata_path=metadata_path,
        model_loader=lambda _: FixedModel(),
        scaler_loader=lambda _: IdentityScaler(),
    )


def observations(days: int = 30) -> list[WeatherObservation]:
    start = date(2025, 1, 1)
    return [
        WeatherObservation(
            observation_date=start + timedelta(days=index),
            rainfall=float(index),
            temperature=25.0 + index,
            humidity=70.0 + index,
        )
        for index in range(days)
    ]


def request_payload(days: int = 30) -> dict:
    return {
        "observations": [
            {
                "Date": item.observation_date.isoformat(),
                "Temperature": item.temperature,
                "Rainfall": item.rainfall,
                "Humidity": item.humidity,
            }
            for item in observations(days)
        ]
    }


def test_predictor_returns_seven_day_response_with_model_version(predictor: WeatherForecastPredictor) -> None:
    forecast = predictor.predict(observations())

    assert len(forecast) == 7
    assert forecast[0].forecast_date == date(2025, 1, 30)
    assert forecast[0].target_date == date(2025, 1, 31)
    assert forecast[-1].target_date == date(2025, 2, 6)
    assert forecast[0].rainfall == 10
    assert forecast[0].temperature == 25
    assert forecast[0].humidity == 80
    assert forecast[0].model_version == "v1"


def test_predictor_rejects_incorrect_observation_count(predictor: WeatherForecastPredictor) -> None:
    with pytest.raises(ForecastValidationError, match="Exactly 30"):
        predictor.predict(observations(29))


def test_predictor_rejects_invalid_numeric_values(predictor: WeatherForecastPredictor) -> None:
    invalid = observations()
    invalid[3] = WeatherObservation(invalid[3].observation_date, float("nan"), 25.0, 70.0)

    with pytest.raises(ForecastValidationError, match="finite"):
        predictor.predict(invalid)


def test_predictor_rejects_date_gaps(predictor: WeatherForecastPredictor) -> None:
    invalid = observations()
    invalid[10] = WeatherObservation(date(2025, 1, 20), 1.0, 25.0, 70.0)

    with pytest.raises(ForecastValidationError, match="without gaps"):
        predictor.predict(invalid)


def test_health_endpoint_reports_model_version(predictor: WeatherForecastPredictor) -> None:
    app.dependency_overrides[get_predictor] = lambda: predictor
    try:
        client = TestClient(app)
        response = client.get("/health")
    finally:
        app.dependency_overrides.clear()

    assert response.status_code == 200
    assert response.json()["modelVersion"] == "v1"


def test_forecast_endpoint_returns_seven_forecast_rows(predictor: WeatherForecastPredictor) -> None:
    app.dependency_overrides[get_predictor] = lambda: predictor
    try:
        client = TestClient(app)
        response = client.post("/forecast", json=request_payload())
    finally:
        app.dependency_overrides.clear()

    assert response.status_code == 200
    body = response.json()
    assert body["modelVersion"] == "v1"
    assert len(body["forecasts"]) == 7
    assert body["forecasts"][0]["targetDate"] == "2025-01-31"


def test_forecast_endpoint_rejects_missing_variables(predictor: WeatherForecastPredictor) -> None:
    payload = request_payload()
    del payload["observations"][0]["Humidity"]
    app.dependency_overrides[get_predictor] = lambda: predictor
    try:
        client = TestClient(app)
        response = client.post("/forecast", json=payload)
    finally:
        app.dependency_overrides.clear()

    assert response.status_code == 422


def test_forecast_endpoint_rejects_invalid_observation_count(predictor: WeatherForecastPredictor) -> None:
    app.dependency_overrides[get_predictor] = lambda: predictor
    try:
        client = TestClient(app)
        response = client.post("/forecast", json=request_payload(29))
    finally:
        app.dependency_overrides.clear()

    assert response.status_code == 400


def test_forecast_endpoint_rejects_invalid_numeric_values(predictor: WeatherForecastPredictor) -> None:
    payload = request_payload()
    payload["observations"][0]["Rainfall"] = "NaN"
    app.dependency_overrides[get_predictor] = lambda: predictor
    try:
        client = TestClient(app)
        response = client.post("/forecast", json=payload)
    finally:
        app.dependency_overrides.clear()

    assert response.status_code == 422
