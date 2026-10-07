from dataclasses import asdict, dataclass

import pandas as pd

from forecasting_service.ingestion.schema import REQUIRED_COLUMNS


@dataclass(frozen=True)
class DataQualityReport:
    row_count: int
    date_range_start: str | None
    date_range_end: str | None
    columns: list[str]
    duplicate_dates: int
    missing_values: dict[str, int]
    non_numeric_values: dict[str, int]
    suspicious_values: dict[str, int]
    long_missing_periods: dict[str, list[dict[str, str | int]]]
    descriptive_statistics: dict[str, dict[str, float]]

    def to_dict(self) -> dict:
        return asdict(self)


def create_quality_report(data: pd.DataFrame, max_gap_days: int = 3) -> DataQualityReport:
    missing_required = [column for column in REQUIRED_COLUMNS if column not in data.columns]
    if missing_required:
        raise ValueError(f"Dataset is missing required columns: {missing_required}")

    working = data.copy()
    working["Date"] = pd.to_datetime(working["Date"], errors="coerce")
    duplicate_dates = int(working["Date"].duplicated().sum())
    missing_values = {column: int(working[column].isna().sum()) for column in REQUIRED_COLUMNS}
    non_numeric_values: dict[str, int] = {}
    suspicious_values: dict[str, int] = {}
    descriptive_statistics: dict[str, dict[str, float]] = {}

    for column in REQUIRED_COLUMNS[1:]:
        numeric = pd.to_numeric(working[column], errors="coerce")
        non_numeric_values[column] = int(numeric.isna().sum() - working[column].isna().sum())
        suspicious_values[column] = _count_suspicious(column, numeric)
        descriptive_statistics[column] = {
            key: float(value)
            for key, value in numeric.describe().dropna().to_dict().items()
        }

    valid_dates = working["Date"].dropna()
    return DataQualityReport(
        row_count=int(len(working)),
        date_range_start=valid_dates.min().date().isoformat() if not valid_dates.empty else None,
        date_range_end=valid_dates.max().date().isoformat() if not valid_dates.empty else None,
        columns=list(data.columns),
        duplicate_dates=duplicate_dates,
        missing_values=missing_values,
        non_numeric_values=non_numeric_values,
        suspicious_values=suspicious_values,
        long_missing_periods=_find_long_missing_periods(working, max_gap_days),
        descriptive_statistics=descriptive_statistics,
    )


def _count_suspicious(column: str, values: pd.Series) -> int:
    if column == "Rainfall":
        return int((values < 0).sum())
    if column == "Humidity":
        return int(((values < 0) | (values > 100)).sum())
    if column == "Temperature":
        return int(((values < -20) | (values > 60)).sum())
    return 0


def _find_long_missing_periods(data: pd.DataFrame, max_gap_days: int) -> dict[str, list[dict[str, str | int]]]:
    periods: dict[str, list[dict[str, str | int]]] = {}
    ordered = data.sort_values("Date")
    for column in REQUIRED_COLUMNS[1:]:
        periods[column] = []
        missing_run: list[pd.Timestamp] = []
        for _, row in ordered.iterrows():
            if pd.isna(row[column]):
                missing_run.append(row["Date"])
                continue
            _append_run(periods[column], missing_run, max_gap_days)
            missing_run = []
        _append_run(periods[column], missing_run, max_gap_days)
    return periods


def _append_run(target: list[dict[str, str | int]], run: list[pd.Timestamp], max_gap_days: int) -> None:
    clean_run = [date for date in run if pd.notna(date)]
    if len(clean_run) > max_gap_days:
        target.append(
            {
                "start": min(clean_run).date().isoformat(),
                "end": max(clean_run).date().isoformat(),
                "length": len(clean_run),
            }
        )
