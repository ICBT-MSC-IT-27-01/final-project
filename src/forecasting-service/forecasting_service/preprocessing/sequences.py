import numpy as np
import pandas as pd


def create_direct_multistep_sequences(
    values: pd.DataFrame | np.ndarray,
    lookback_days: int,
    forecast_horizon_days: int,
) -> tuple[np.ndarray, np.ndarray]:
    array = np.asarray(values, dtype=float)
    if array.ndim != 2:
        raise ValueError("values must be a 2D array.")
    if lookback_days <= 0 or forecast_horizon_days <= 0:
        raise ValueError("lookback and horizon must be positive.")

    inputs = []
    targets = []
    last_start = len(array) - lookback_days - forecast_horizon_days + 1
    for start in range(max(0, last_start)):
        input_end = start + lookback_days
        target_end = input_end + forecast_horizon_days
        inputs.append(array[start:input_end])
        targets.append(array[input_end:target_end])

    return np.asarray(inputs, dtype=float), np.asarray(targets, dtype=float)


def create_sequences_for_target_period(
    values: pd.DataFrame | np.ndarray,
    dates: pd.Series | np.ndarray,
    lookback_days: int,
    forecast_horizon_days: int,
    target_start: str,
    target_end: str,
) -> tuple[np.ndarray, np.ndarray, list[list[str]]]:
    array = np.asarray(values, dtype=float)
    date_index = pd.to_datetime(pd.Series(dates)).reset_index(drop=True)
    target_start_date = pd.Timestamp(target_start)
    target_end_date = pd.Timestamp(target_end)

    inputs = []
    targets = []
    target_dates: list[list[str]] = []
    last_start = len(array) - lookback_days - forecast_horizon_days + 1
    for start in range(max(0, last_start)):
        input_end = start + lookback_days
        target_end_index = input_end + forecast_horizon_days
        window_dates = date_index.iloc[input_end:target_end_index]
        if window_dates.iloc[0] < target_start_date or window_dates.iloc[-1] > target_end_date:
            continue
        inputs.append(array[start:input_end])
        targets.append(array[input_end:target_end_index])
        target_dates.append([date.date().isoformat() for date in window_dates])

    return np.asarray(inputs, dtype=float), np.asarray(targets, dtype=float), target_dates
