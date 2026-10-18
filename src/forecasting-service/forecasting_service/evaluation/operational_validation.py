"""Compare one persisted seven-day run with dated external daily weather data.

This is an audit tool, not part of live inference. Model-based reanalysis may be
used for a labelled comparison, but it is not independent station validation.
"""

import argparse
import csv
import json
import math
from datetime import date, datetime, timedelta, timezone
from pathlib import Path


VARIABLES = (
    ("Rainfall", "rainfallMm"),
    ("Temperature", "temperatureC"),
    ("Humidity", "humidityPercent"),
)


def _number(value: object, label: str) -> float:
    try:
        number = float(value)
    except (TypeError, ValueError) as error:
        raise ValueError(f"{label} must be a number.") from error
    if not math.isfinite(number):
        raise ValueError(f"{label} must be finite.")
    return number


def load_observations(path: Path) -> dict[str, dict[str, float]]:
    with path.open(newline="", encoding="utf-8-sig") as stream:
        reader = csv.DictReader(stream)
        required = {"Date", *(variable for variable, _ in VARIABLES)}
        if not reader.fieldnames or not required.issubset(reader.fieldnames):
            raise ValueError("Observation CSV needs Date,Rainfall,Temperature,Humidity columns.")
        rows: dict[str, dict[str, float]] = {}
        for row in reader:
            day = date.fromisoformat(row["Date"])
            key = day.isoformat()
            if key in rows:
                raise ValueError(f"Duplicate observation date: {key}.")
            values = {variable: _number(row[variable], f"{key} {variable}") for variable, _ in VARIABLES}
            if values["Rainfall"] < 0 or not 0 <= values["Humidity"] <= 100:
                raise ValueError(f"Impossible rainfall or humidity value on {key}.")
            rows[key] = values
    return rows


def evaluate_run(
    forecast: dict,
    observations: dict[str, dict[str, float]],
    *,
    source_name: str,
    source_reference: str,
    source_kind: str,
    location: str,
) -> dict:
    if source_kind not in {"station", "reanalysis"}:
        raise ValueError("Source kind must be station or reanalysis.")
    if not all((source_name.strip(), source_reference.strip(), location.strip())):
        raise ValueError("Source name, reference and location are required.")
    days = forecast.get("dailyForecasts")
    if not isinstance(days, list) or len(days) != 7:
        raise ValueError("Forecast must contain exactly seven daily forecasts.")
    origin = date.fromisoformat(forecast["forecastDate"])
    issued_at = datetime.fromisoformat(forecast["createdAt"].replace("Z", "+00:00"))
    if issued_at.tzinfo is None:
        raise ValueError("Forecast creation time must include a timezone.")
    issued_local = issued_at.astimezone(timezone(timedelta(hours=5, minutes=30)))
    target_dates = [(origin + timedelta(days=offset)).isoformat() for offset in range(1, 8)]
    not_forward_dates = [day for day in target_dates if day <= issued_local.date().isoformat()]
    if [row.get("targetDate") for row in days] != target_dates:
        raise ValueError("Forecast dates must be consecutive days after the origin date.")
    for day in days:
        for _, forecast_key in VARIABLES:
            _number(day.get(forecast_key), f"{day['targetDate']} {forecast_key}")

    missing_dates = [day for day in [origin.isoformat(), *target_dates] if day not in observations]
    report = {
        "forecastRunId": forecast.get("forecastRunId"),
        "modelVersion": forecast.get("modelVersion"),
        "forecastOriginDate": origin.isoformat(),
        "issuedAtSriLanka": issued_local.isoformat(),
        "targetDates": target_dates,
        "notFullyForwardIssuedDates": not_forward_dates,
        "observationSource": {
            "name": source_name,
            "reference": source_reference,
            "kind": source_kind,
            "location": location,
        },
        "pairedTargetDays": len(target_dates) - len([day for day in target_dates if day in missing_dates]),
        "missingDates": missing_dates,
        "status": "INCOMPLETE" if missing_dates else "NOT_FULLY_FORWARD_ISSUED" if not_forward_dates else "COMPLETE_SINGLE_RUN",
        "interpretation": (
            "No accuracy metrics until all seven target days and the origin day are present."
            if missing_dates else
            "At least one target calendar day had begun when this forecast was issued; do not call this a full seven-day forward forecast."
            if not_forward_dates else
            "Single-run comparison with station observations; this does not establish general forecast skill."
            if source_kind == "station" else
            "Comparison with model-based reanalysis; this is not independent measured-observation validation."
        ),
        "metrics": None,
    }
    if missing_dates or not_forward_dates:
        return report

    metrics = {}
    for variable, forecast_key in VARIABLES:
        actual = [observations[day][variable] for day in target_dates]
        predicted = [_number(day[forecast_key], forecast_key) for day in days]
        baseline = [observations[origin.isoformat()][variable]] * 7
        metrics[variable] = {
            "unit": "mm/day" if variable == "Rainfall" else "°C" if variable == "Temperature" else "%",
            "model": _metrics(actual, predicted),
            "persistenceBaseline": _metrics(actual, baseline),
        }
    report["metrics"] = metrics
    return report


def _metrics(actual: list[float], predicted: list[float]) -> dict[str, float]:
    errors = [prediction - observation for prediction, observation in zip(predicted, actual)]
    return {
        "mae": sum(abs(error) for error in errors) / len(errors),
        "rmse": math.sqrt(sum(error * error for error in errors) / len(errors)),
    }


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--forecast-json", type=Path, required=True, help="Saved GET /api/forecasts/latest JSON")
    parser.add_argument("--observations", type=Path, required=True, help="Independent daily CSV")
    parser.add_argument("--source-name", required=True)
    parser.add_argument("--source-reference", required=True, help="Document or dataset identifier")
    parser.add_argument("--source-kind", choices=("station", "reanalysis"), required=True)
    parser.add_argument("--location", required=True, help="Observation site or model grid location")
    args = parser.parse_args()
    forecast = json.loads(args.forecast_json.read_text(encoding="utf-8-sig"))
    report = evaluate_run(
        forecast,
        load_observations(args.observations),
        source_name=args.source_name,
        source_reference=args.source_reference,
        source_kind=args.source_kind,
        location=args.location,
    )
    print(json.dumps(report, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
