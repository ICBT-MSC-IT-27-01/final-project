from pathlib import Path

import pandas as pd

from forecasting_service.ingestion.schema import ColumnMapping, REQUIRED_COLUMNS


def load_weather_csv(path: Path | str, mapping: ColumnMapping | None = None) -> pd.DataFrame:
    mapping = mapping or ColumnMapping()
    source_path = Path(path)
    if not source_path.exists():
        raise FileNotFoundError(f"Weather dataset was not found: {source_path}")

    raw = pd.read_csv(source_path)
    missing_columns = [column for column in mapping.as_source_columns() if column not in raw.columns]
    if missing_columns:
        raise ValueError(f"Dataset is missing required source columns: {missing_columns}")

    data = pd.DataFrame()
    data["Date"] = raw[mapping.date]
    data["Rainfall"] = raw[mapping.rainfall]
    data["Humidity"] = raw[mapping.humidity]

    if mapping.temperature and mapping.temperature in raw.columns:
        data["Temperature"] = raw[mapping.temperature]
    elif mapping.min_temperature and mapping.max_temperature:
        data["Temperature"] = (
            pd.to_numeric(raw[mapping.min_temperature], errors="coerce")
            + pd.to_numeric(raw[mapping.max_temperature], errors="coerce")
        ) / 2
    else:
        raise ValueError("Temperature or min/max temperature mapping is required.")

    return data[list(REQUIRED_COLUMNS)]
