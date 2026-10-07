from dataclasses import dataclass

import numpy as np
import pandas as pd
from sklearn.preprocessing import MinMaxScaler


@dataclass
class MinMaxFeatureScaler:
    scaler: MinMaxScaler | None = None

    def fit(self, training_values: pd.DataFrame | np.ndarray) -> "MinMaxFeatureScaler":
        values = _as_array(training_values)
        self.scaler = MinMaxScaler()
        self.scaler.fit(values)
        return self

    def transform(self, values: pd.DataFrame | np.ndarray) -> np.ndarray:
        self._ensure_fitted()
        return self.scaler.transform(_as_array(values))

    def inverse_transform(self, values: np.ndarray) -> np.ndarray:
        self._ensure_fitted()
        return self.scaler.inverse_transform(values)

    def _ensure_fitted(self) -> None:
        if self.scaler is None:
            raise RuntimeError("Scaler has not been fitted.")


def _as_array(values: pd.DataFrame | np.ndarray) -> np.ndarray:
    return np.asarray(values, dtype=float)
