# Phase 6 Backend Forecast Integration

## Objective

Phase 6 integrates the approved Python LSTM weather model with the ASP.NET Core backend through a clean service boundary.

The model predicts weather only:

- Rainfall
- Temperature
- Humidity

It does not predict crops, crop scores, rankings, suitability categories, or recommendations. Crop suitability remains Phase 7.

## Runtime Flow

```text
30 days of weather observations
    ->
Python Forecasting Service
    ->
Approved v1.keras model + v1_scaler.joblib
    ->
7-day weather forecast
    ->
ASP.NET Core Web API
    ->
Forecast response + ForecastRecord persistence
```

## Python Inference Service

Run from `src/forecasting-service`:

```powershell
python -m uvicorn forecasting_service.api:app --host 127.0.0.1 --port 8000
```

Endpoints:

- `GET /health`
- `POST /forecast`

The inference service loads:

- `src/forecasting-service/artifacts/v1_era5_2010_2025/v1.keras`
- `src/forecasting-service/artifacts/v1_era5_2010_2025/v1_scaler.joblib`
- `src/forecasting-service/artifacts/v1_era5_2010_2025/v1_metadata.json`

It validates metadata before inference, including model version `v1`, lookback `30`, horizon `7`, and feature order `Rainfall`, `Temperature`, `Humidity`.

## Python Request Contract

`POST /forecast` accepts:

```json
{
  "observations": [
    {
      "Date": "2025-01-01",
      "Temperature": 25.0,
      "Rainfall": 1.2,
      "Humidity": 80.0
    }
  ]
}
```

Exactly 30 daily observations are required. Dates must be chronological, unique, daily, and gap-free. Numeric values must be finite.

## Python Response Contract

The service returns:

```json
{
  "modelVersion": "v1",
  "forecastDate": "2025-01-30",
  "forecasts": [
    {
      "forecastDate": "2025-01-30",
      "targetDate": "2025-01-31",
      "temperature": 26.1,
      "rainfall": 2.3,
      "humidity": 81.0,
      "modelVersion": "v1"
    }
  ]
}
```

Exactly seven forecast rows are returned on success.

## ASP.NET Core Integration

Backend endpoint:

- `POST /api/forecasts`

The ASP.NET Core API receives a 30-day observation sequence, calls the Python forecasting service through an infrastructure `HttpClient`, validates the seven-day response, returns a backend DTO, and persists one `ForecastRecord` row per target date.

The integration follows the existing project structure:

- `Api`: thin controller
- `Application`: DTOs, service interfaces, result/error contracts
- `Infrastructure`: HTTP forecasting client, application service implementation, EF Core persistence
- `Domain`: existing `ForecastRecord` entity

## Configuration

Backend configuration uses:

- `FeatureFlags:EnableWeatherModelIntegration`
- `ForecastingService:BaseUrl`

If `EnableWeatherModelIntegration` is disabled, `/api/forecasts` returns a controlled service-unavailable response and does not call the Python service.

The Python service base URL is configuration-driven. Do not hard-code production URLs, credentials, or machine-specific paths.

## ForecastRecord Persistence

The existing `ForecastRecord` entity is used without schema changes:

- `ForecastDate`
- `TargetDate`
- `Rainfall`
- `Temperature`
- `Humidity`
- `ModelVersion`
- `CreatedAt`

Phase 6 does not create a forecast-history table, recommendation relationship, or `ForecastRecord` -> `Recommendation` foreign key.

## Testing

Python tests:

```powershell
cd src/forecasting-service
python -m pytest
```

Backend tests:

```powershell
cd src/backend
dotnet test
```

Tests use controlled fakes where appropriate and do not retrain the LSTM.

## Current Limitation

The final live recent-weather acquisition provider is unresolved. Phase 6 accepts the required 30-day observation sequence through the API boundary. A production/public workflow still requires an approved source for recent actual weather observations.
