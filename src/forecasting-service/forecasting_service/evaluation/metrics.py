import math

import numpy as np


def mae(y_true: np.ndarray, y_pred: np.ndarray) -> float:
    return float(np.mean(np.abs(y_true - y_pred)))


def rmse(y_true: np.ndarray, y_pred: np.ndarray) -> float:
    return float(math.sqrt(np.mean(np.square(y_true - y_pred))))


def regression_metrics(y_true: np.ndarray, y_pred: np.ndarray, feature_names: tuple[str, ...]) -> dict:
    if y_true.shape != y_pred.shape:
        raise ValueError("y_true and y_pred must have the same shape.")
    results = {
        "overall_mae": mae(y_true, y_pred),
        "overall_rmse": rmse(y_true, y_pred),
        "per_variable": {},
        "per_horizon_day": {},
    }
    for index, feature in enumerate(feature_names):
        results["per_variable"][feature] = {
            "mae": mae(y_true[:, :, index], y_pred[:, :, index]),
            "rmse": rmse(y_true[:, :, index], y_pred[:, :, index]),
        }
    for day_index in range(y_true.shape[1]):
        results["per_horizon_day"][f"day_{day_index + 1}"] = {
            "mae": mae(y_true[:, day_index, :], y_pred[:, day_index, :]),
            "rmse": rmse(y_true[:, day_index, :], y_pred[:, day_index, :]),
        }
    return results
