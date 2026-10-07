from __future__ import annotations

from dataclasses import dataclass
from datetime import date, timedelta
import json
import math
from pathlib import Path
from typing import Callable, Iterable, Protocol

import joblib
import numpy as np


APPROVED_ARTIFACT_DIR = Path(__file__).resolve().parents[2] / "artifacts" / "v1_era5_2010_2025"
APPROVED_MODEL_PATH = APPROVED_ARTIFACT_DIR / "v1.keras"
APPROVED_SCALER_PATH = APPROVED_ARTIFACT_DIR / "v1_scaler.joblib"
APPROVED_METADATA_PATH = APPROVED_ARTIFACT_DIR / "v1_metadata.json"


class ForecastValidationError(ValueError):
    pass


class ForecastArtifactError(RuntimeError):
    pass


class ForecastModel(Protocol):
    def predict(self, values, verbose: int = 0): ...


@dataclass(frozen=True)
class WeatherObservation:
    observation_date: date
    rainfall: float
    temperature: float
    humidity: float


@dataclass(frozen=True)
class ForecastPrediction:
    forecast_date: date
    target_date: date
    rainfall: float
    temperature: float
    humidity: float
    model_version: str


class WeatherForecastPredictor:
    def __init__(
        self,
        *,
        model_path: Path = APPROVED_MODEL_PATH,
        scaler_path: Path = APPROVED_SCALER_PATH,
        metadata_path: Path = APPROVED_METADATA_PATH,
        model_loader: Callable[[Path], ForecastModel] | None = None,
        scaler_loader: Callable[[Path], object] | None = None,
    ) -> None:
        self.model_path = model_path
        self.scaler_path = scaler_path
        self.metadata_path = metadata_path
        self.metadata = self._load_metadata(metadata_path)
        self.model_version = str(self.metadata.get("model_version", "")).strip()
        self.feature_columns = tuple(self.metadata.get("config", {}).get("feature_columns", []))
        self.lookback_days = int(self.metadata.get("config", {}).get("lookback_days", 0))
        self.forecast_horizon_days = int(self.metadata.get("config", {}).get("forecast_horizon_days", 0))

        self._validate_metadata()
        self.model = (model_loader or _load_keras_model)(model_path)
        self.scaler = (scaler_loader or joblib.load)(scaler_path)

    def predict(self, observations: Iterable[WeatherObservation]) -> list[ForecastPrediction]:
        ordered = list(observations)
        self._validate_observations(ordered)
        values = np.asarray(
            [[_feature_value(observation, feature) for feature in self.feature_columns] for observation in ordered],
            dtype=float,
        )
        scaled = self.scaler.transform(values)
        prediction = self.model.predict(np.expand_dims(scaled, axis=0), verbose=0)
        prediction_array = np.asarray(prediction, dtype=float)
        expected_shape = (1, self.forecast_horizon_days, len(self.feature_columns))
        if prediction_array.shape != expected_shape:
            raise ForecastArtifactError(f"Model returned shape {prediction_array.shape}; expected {expected_shape}.")

        flat_prediction = prediction_array.reshape(self.forecast_horizon_days, len(self.feature_columns))
        original = self.scaler.inverse_transform(flat_prediction)
        forecast_date = ordered[-1].observation_date
        rows: list[ForecastPrediction] = []
        for index, row in enumerate(original):
            values_by_feature = {feature: float(row[feature_index]) for feature_index, feature in enumerate(self.feature_columns)}
            rows.append(
                ForecastPrediction(
                    forecast_date=forecast_date,
                    target_date=forecast_date + timedelta(days=index + 1),
                    rainfall=values_by_feature["Rainfall"],
                    temperature=values_by_feature["Temperature"],
                    humidity=values_by_feature["Humidity"],
                    model_version=self.model_version,
                )
            )
        return rows

    @staticmethod
    def _load_metadata(metadata_path: Path) -> dict:
        if not metadata_path.exists():
            raise ForecastArtifactError("Approved model metadata artifact is missing.")
        try:
            return json.loads(metadata_path.read_text(encoding="utf-8"))
        except json.JSONDecodeError as exc:
            raise ForecastArtifactError("Approved model metadata artifact is invalid.") from exc

    def _validate_metadata(self) -> None:
        if not self.model_path.exists():
            raise ForecastArtifactError("Approved Keras model artifact is missing.")
        if not self.scaler_path.exists():
            raise ForecastArtifactError("Approved scaler artifact is missing.")
        if self.model_version != "v1":
            raise ForecastArtifactError("Model metadata does not describe the approved v1 model.")
        if self.feature_columns != ("Rainfall", "Temperature", "Humidity"):
            raise ForecastArtifactError("Model metadata has an unexpected feature order.")
        if self.lookback_days != 30:
            raise ForecastArtifactError("Model metadata has an unexpected lookback length.")
        if self.forecast_horizon_days != 7:
            raise ForecastArtifactError("Model metadata has an unexpected forecast horizon.")

    def _validate_observations(self, observations: list[WeatherObservation]) -> None:
        if len(observations) != self.lookback_days:
            raise ForecastValidationError(f"Exactly {self.lookback_days} daily observations are required.")

        seen_dates: set[date] = set()
        previous_date: date | None = None
        for observation in observations:
            if observation.observation_date in seen_dates:
                raise ForecastValidationError("Observation dates must not contain duplicates.")
            seen_dates.add(observation.observation_date)
            if previous_date is not None and observation.observation_date != previous_date + timedelta(days=1):
                raise ForecastValidationError("Observations must be chronological daily values without gaps.")
            previous_date = observation.observation_date
            for value in (observation.rainfall, observation.temperature, observation.humidity):
                if not math.isfinite(value):
                    raise ForecastValidationError("Weather values must be finite numbers.")


def _load_keras_model(model_path: Path) -> ForecastModel:
    if not model_path.exists():
        raise ForecastArtifactError("Approved Keras model artifact is missing.")
    try:
        import tensorflow as tf
    except ImportError as exc:
        raise ForecastArtifactError("TensorFlow is required to load the approved model artifact.") from exc
    return tf.keras.models.load_model(model_path)


def _feature_value(observation: WeatherObservation, feature: str) -> float:
    return {
        "Rainfall": observation.rainfall,
        "Temperature": observation.temperature,
        "Humidity": observation.humidity,
    }[feature]
