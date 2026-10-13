# Phase 13 Option B Operational Source

## Decision

Option B keeps the ERA5-derived Open-Meteo Historical Weather API path for historical research, training, and evaluation. It adds Open-Meteo Forecast API `past_days` as the candidate source for recent operational LSTM inputs.

The existing TensorFlow LSTM artifacts remain unchanged in:

- `src/forecasting-service/artifacts/v1_era5_2010_2025/`

## Source Separation

| Source | Adapter | Product identity | Intended purpose |
| --- | --- | --- | --- |
| Historical | `OpenMeteoWeatherSource` | `era5` | `HistoricalEvaluation`, research, training, and historical comparisons |
| Operational candidate | `OpenMeteoForecastPastDaysWeatherSource` | `forecast_api_past_days_auto` | Recent 30-day input candidate for controlled `Operational` runs |

Forecast API `past_days` data is not labelled ERA5 in the implementation. It is gridded model/API data for a coordinate and must not be described as measured district-average weather.

## Forecast API Contract

- Endpoint: `https://api.open-meteo.com/v1/forecast`
- Coordinates: latitude `8.312229`, longitude `80.413055`
- Timezone: `Asia/Colombo`
- Parameters: `past_days=30`, `forecast_days=0`
- Hourly variables: `temperature_2m`, `relative_humidity_2m`, `precipitation`
- Units: Celsius, percent relative humidity, millimetres precipitation
- Automatic HTTP redirects: disabled through the operational Open-Meteo typed client handler

HTTP 3xx responses fail closed and are not followed.

## Aggregation Rules

The adapter requires exactly 30 local calendar days ending yesterday in `Asia/Colombo`.

For each day:

- exactly 24 hourly records are required
- duplicate timestamps are rejected
- missing, out-of-window, current-day, or future timestamps are rejected
- rainfall is summed from hourly precipitation
- temperature is averaged from hourly temperature
- relative humidity is averaged from hourly relative humidity

No imputation, interpolation, provider fallback, retry loop, or date shifting is performed.

## Acceptance Gate

Three gates must pass before the administrator-triggered operational path can call the provider, Python service, or persistence layer:

- `FeatureFlags:EnableWeatherModelIntegration = true`
- `FeatureFlags:OperationalWeatherSource = "OpenMeteoForecastPastDays"`
- `FeatureFlags:AcceptOperationalWeatherSource = true`

Defaults remain fail-closed in committed configuration:

- `EnableWeatherModelIntegration = false`
- `OperationalWeatherSource = ""`
- `AcceptOperationalWeatherSource = false`

The acceptance flag is intentionally separate from technical validation. The final live D/E run used isolated process-level overrides for one MSc prototype verification run only.

## Final Live Verification

Final controlled D/E execution passed on 2026-10-13.

- ForecastRunId: `b2d45e38-72fd-4b3d-87b5-418891a485c2`
- Provider request count: 1
- Python inference count: 1
- Admin POST count: 1
- Database delta: `ForecastRun +1`, `ForecastRecord +7`, `Recommendation +0`
- `GET /api/forecasts/latest`: PASS
- Regression tests: backend, Angular, and Python PASS

The live operational source produced 30 validated daily inputs ending 2026-10-12 local date and the existing LSTM `v1` produced seven finite forecast outputs for 2026-10-13 through 2026-10-19 local dates.

## LSTM Compatibility Status

Verified technically:

- input length remains 30 daily observations
- feature order remains rainfall, temperature, humidity
- seven-day output contract remains unchanged
- existing Python client boundary is reused
- no retraining or artifact replacement occurred
- one live administrator-triggered run completed through the real provider, Python service, and SQL persistence path

Limited scientifically:

- the model remains ERA5-trained
- source distribution equivalence is not proven
- one or two recent samples do not establish full statistical compatibility
- paired ERA5 and Forecast API data over matched dates/seasons/grid points is still needed
- predictive accuracy is not verified

Operational source acceptance for this repository is therefore technical, development-only, and not a production scientific acceptance claim.

## Recommendation Integration

Recommendation orchestration uses the same eligible Operational run policy as `GET /api/forecasts/latest`. Existing tests verify exclusion of incomplete, malformed, `LegacyUnknown`, and `HistoricalEvaluation` runs.

The live database currently has the six approved crops but no configured crop environmental requirements, soil compatibility rows, or suitability configuration rows. A live recommendation evaluation would therefore be insufficient-evidence until administrator configuration is populated. The final D/E verification did not call the public recommendation endpoint because that endpoint persists `Recommendation` rows.

## Licensing and Attribution

Open-Meteo documentation reviewed during Phase 13 indicates free API use is non-commercial, API data is available under CC BY 4.0 with attribution, and commercial or higher-volume use requires the commercial offering or direct contact with Open-Meteo. This repository does not contain legal clearance for production use.

Suggested attribution:

> Weather data and model-derived weather variables are sourced from Open-Meteo and referenced upstream weather datasets under applicable attribution terms. This MSc prototype uses the data for academic research and evaluation; operational or commercial deployment requires review of Open-Meteo and upstream data-source terms.

