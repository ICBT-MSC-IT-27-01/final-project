import pandas as pd

from forecasting_service.ingestion.schema import REQUIRED_COLUMNS


def clean_weather_data(data: pd.DataFrame, max_interpolation_gap_days: int = 3) -> tuple[pd.DataFrame, dict]:
    missing_required = [column for column in REQUIRED_COLUMNS if column not in data.columns]
    if missing_required:
        raise ValueError(f"Dataset is missing required columns: {missing_required}")

    working = data.copy()
    working["Date"] = pd.to_datetime(working["Date"], errors="coerce")
    working = working.dropna(subset=["Date"])
    working = working.sort_values("Date")
    duplicate_dates = int(working["Date"].duplicated().sum())
    working = working.drop_duplicates(subset=["Date"], keep="last")

    invalid_counts: dict[str, int] = {}
    for column in REQUIRED_COLUMNS[1:]:
        working[column] = pd.to_numeric(working[column], errors="coerce")
        invalid_mask = _invalid_mask(column, working[column])
        invalid_counts[column] = int(invalid_mask.sum())
        working.loc[invalid_mask, column] = pd.NA

    missing_before = {column: int(working[column].isna().sum()) for column in REQUIRED_COLUMNS[1:]}
    working = working.set_index("Date").asfreq("D")
    for column in REQUIRED_COLUMNS[1:]:
        working[column] = working[column].interpolate(
            method="time",
            limit=max_interpolation_gap_days,
            limit_area="inside",
        )
    missing_after = {column: int(working[column].isna().sum()) for column in REQUIRED_COLUMNS[1:]}
    working = working.dropna(subset=list(REQUIRED_COLUMNS[1:])).reset_index()

    metadata = {
        "duplicate_dates_removed": duplicate_dates,
        "invalid_values_marked_missing": invalid_counts,
        "missing_before_interpolation": missing_before,
        "missing_after_interpolation": missing_after,
        "max_interpolation_gap_days": max_interpolation_gap_days,
        "outlier_policy": "Genuine extremes are preserved; only impossible or malformed values are marked missing.",
    }
    return working[list(REQUIRED_COLUMNS)], metadata


def _invalid_mask(column: str, values: pd.Series) -> pd.Series:
    if column == "Rainfall":
        return values < 0
    if column == "Humidity":
        return (values < 0) | (values > 100)
    if column == "Temperature":
        return (values < -20) | (values > 60)
    return pd.Series(False, index=values.index)
