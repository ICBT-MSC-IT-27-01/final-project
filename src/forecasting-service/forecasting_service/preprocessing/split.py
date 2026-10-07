import pandas as pd

from forecasting_service.config.training_config import SplitConfig


def chronological_split(data: pd.DataFrame, split: SplitConfig) -> tuple[pd.DataFrame, pd.DataFrame, pd.DataFrame, dict]:
    split.validate()
    ordered = data.sort_values("Date").reset_index(drop=True)
    row_count = len(ordered)
    train_end = int(row_count * split.train_ratio)
    validation_end = train_end + int(row_count * split.validation_ratio)
    if train_end <= 0 or validation_end <= train_end or validation_end >= row_count:
        raise ValueError("Dataset is too small for the configured chronological split.")

    train = ordered.iloc[:train_end].copy()
    validation = ordered.iloc[train_end:validation_end].copy()
    test = ordered.iloc[validation_end:].copy()
    metadata = {
        "train": _split_metadata(train),
        "validation": _split_metadata(validation),
        "test": _split_metadata(test),
    }
    return train, validation, test, metadata


def _split_metadata(data: pd.DataFrame) -> dict:
    dates = pd.to_datetime(data["Date"])
    return {
        "count": int(len(data)),
        "start": dates.min().date().isoformat(),
        "end": dates.max().date().isoformat(),
    }


def date_based_split(
    data: pd.DataFrame,
    *,
    train_start: str,
    train_end: str,
    validation_start: str,
    validation_end: str,
    test_start: str,
    test_end: str,
) -> tuple[pd.DataFrame, pd.DataFrame, pd.DataFrame, dict]:
    ordered = data.copy()
    ordered["Date"] = pd.to_datetime(ordered["Date"])
    ordered = ordered.sort_values("Date").reset_index(drop=True)

    train = _between(ordered, train_start, train_end)
    validation = _between(ordered, validation_start, validation_end)
    test = _between(ordered, test_start, test_end)
    if min(len(train), len(validation), len(test)) == 0:
        raise ValueError("One or more approved date splits is empty.")

    metadata = {
        "train": _split_metadata(train),
        "validation": _split_metadata(validation),
        "test": _split_metadata(test),
    }
    return train, validation, test, metadata


def verify_date_continuity(data: pd.DataFrame, start: str, end: str) -> dict:
    dates = pd.to_datetime(data["Date"])
    expected = pd.date_range(start=start, end=end, freq="D")
    present = pd.DatetimeIndex(dates.dropna().sort_values().unique())
    missing = expected.difference(present)
    unexpected = present.difference(expected)
    return {
        "expected_count": int(len(expected)),
        "present_count": int(len(present)),
        "missing_count": int(len(missing)),
        "unexpected_count": int(len(unexpected)),
        "missing_dates": [date.date().isoformat() for date in missing[:20]],
        "unexpected_dates": [date.date().isoformat() for date in unexpected[:20]],
        "is_continuous": len(missing) == 0 and len(unexpected) == 0,
    }


def _between(data: pd.DataFrame, start: str, end: str) -> pd.DataFrame:
    start_date = pd.Timestamp(start)
    end_date = pd.Timestamp(end)
    return data[(data["Date"] >= start_date) & (data["Date"] <= end_date)].copy()
