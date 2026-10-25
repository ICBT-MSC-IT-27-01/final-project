from dataclasses import dataclass

import numpy as np
import pandas as pd
from sklearn.preprocessing import MinMaxScaler


@dataclass
class MinMaxFeatureScaler:
    scaler: MinMaxScaler | None = None
    rainfall_transform: str = "none"

    def fit(self, training_values: pd.DataFrame | np.ndarray) -> "MinMaxFeatureScaler":
        values = self._transform_for_scaling(_as_array(training_values))
        self.scaler = MinMaxScaler()
        self.scaler.fit(values)
        return self

    def transform(self, values: pd.DataFrame | np.ndarray) -> np.ndarray:
        self._ensure_fitted()
        return self.scaler.transform(self._transform_for_scaling(_as_array(values)))

    def inverse_transform(self, values: np.ndarray) -> np.ndarray:
        self._ensure_fitted()
        original = self.scaler.inverse_transform(_as_array(values))
        if self.rainfall_transform == "log1p":
            original[:, 0] = np.expm1(original[:, 0])
        return original

    def _ensure_fitted(self) -> None:
        if self.scaler is None:
            raise RuntimeError("Scaler has not been fitted.")

    def _transform_for_scaling(self, values: np.ndarray) -> np.ndarray:
        if self.rainfall_transform not in {"none", "log1p"}:
            raise ValueError("rainfall_transform must be 'none' or 'log1p'.")
        transformed = values.copy()
        if self.rainfall_transform == "log1p":
            if np.any(transformed[:, 0] < 0):
                raise ValueError("Rainfall must be non-negative before log1p transformation.")
            transformed[:, 0] = np.log1p(transformed[:, 0])
        return transformed


def _as_array(values: pd.DataFrame | np.ndarray) -> np.ndarray:
    return np.asarray(values, dtype=float)
