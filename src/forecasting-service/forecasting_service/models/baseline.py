import numpy as np


def persistence_forecast(inputs: np.ndarray, forecast_horizon_days: int) -> np.ndarray:
    if inputs.ndim != 3:
        raise ValueError("inputs must have shape (samples, lookback, features).")
    last_observation = inputs[:, -1:, :]
    return np.repeat(last_observation, forecast_horizon_days, axis=1)
