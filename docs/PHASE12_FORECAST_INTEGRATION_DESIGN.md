# Phase 12 Forecast Integration Design

Status: DESIGN ONLY - NOT IMPLEMENTED

This document defines a future weather-source-to-forecast integration architecture for owner review. It does not implement a provider, a new backend API, scheduling, database writes, infrastructure changes, or model retraining.

## Current Implementation Baseline

### Python Forecasting Service

Existing endpoint:

```text
POST /forecast
```

Existing request:

```json
{
  "observations": [
    {
      "Date": "2026-09-01",
      "Temperature": 27.0,
      "Rainfall": 1.2,
      "Humidity": 80.0
    }
  ]
}
```

Rules verified from source:

- exactly 30 observations
- chronological daily dates
- no duplicate dates
- no gaps
- finite rainfall, temperature, and humidity values
- feature order: `Rainfall`, `Temperature`, `Humidity`
- scaler and model metadata must match approved `v1`
- output shape: seven rows with rainfall, temperature, humidity, target date, forecast date, and model version

### ASP.NET Core Forecast Integration

Existing endpoint:

```text
POST /api/forecasts
```

Existing behavior:

- requires `FeatureFlags:EnableWeatherModelIntegration`
- validates exactly 30 chronological daily observations
- calls Python `POST /forecast`
- requires exactly seven returned forecast rows
- creates one `ForecastRunId` per backend forecast generation
- persists seven `ForecastRecord` rows, one per target date
- uses the same `CreatedAt` timestamp for rows in one run

### ForecastRecord Persistence

Existing fields:

- `Id`
- `ForecastRunId`
- `ForecastDate`
- `TargetDate`
- `Rainfall`
- `Temperature`
- `Humidity`
- `ModelVersion`
- `CreatedAt`

Existing indexes/constraints:

- non-empty `ForecastRunId` check
- unique index on `(ForecastRunId, TargetDate)`
- index on `(ForecastDate, TargetDate)`

## Canonical Daily Observation Contract

Proposed future internal contract:

```json
{
  "observationDate": "YYYY-MM-DD",
  "district": "Anuradhapura District",
  "locationId": "anuradhapura-representative-point",
  "latitude": 8.3114,
  "longitude": 80.4037,
  "rainfall": {
    "value": 0.0,
    "unit": "mm/day",
    "isMissing": false
  },
  "meanTemperature": {
    "value": 27.0,
    "unit": "deg C",
    "isMissing": false
  },
  "meanHumidity": {
    "value": 80.0,
    "unit": "percent",
    "isMissing": false
  },
  "source": {
    "provider": "OWNER_APPROVED_PROVIDER",
    "productType": "reanalysis | station-observation | gridded-analysis",
    "dataset": "provider-specific product name",
    "retrievedAt": "UTC timestamp",
    "timeStandard": "UTC | local-time | local-solar-time",
    "provenanceUrl": "provider request or citation"
  },
  "quality": {
    "status": "Accepted | Missing | Rejected | RequiresReview",
    "messages": []
  }
}
```

This contract is proposed. It is not a database schema and is not implemented.

## Input Compatibility Rules

Required rules before calling the existing model:

1. Exactly 30 daily rows.
2. Dates are chronological and consecutive.
3. No duplicate dates.
4. Rainfall is daily precipitation accumulation in millimeters.
5. Temperature is daily mean near-surface air temperature in deg C.
6. Humidity is daily mean relative humidity in percent.
7. Humidity values must be 0 to 100 inclusive.
8. Rainfall must be non-negative.
9. Values must be finite numeric values.
10. Source/provider metadata must be retained before persistence or audit.
11. Timezone/daily-boundary conversion must be explicit.
12. Missing days should block model inference unless an owner-approved imputation rule exists.

No imputation rule is approved in Phase 12. Do not fill missing days silently.

## Proposed Future Workflow

```text
Owner-approved weather source
    ->
Weather source adapter
    ->
Canonical 30-day daily observation set
    ->
Validation and provenance capture
    ->
Existing Python /forecast endpoint
    ->
Seven-day AI prediction
    ->
Existing ASP.NET Core forecast persistence
    ->
Valid complete ForecastRunId
    ->
Read-only latest forecast retrieval
    ->
Angular public dashboard
```

## Component Responsibilities

| Component | Responsibility | Notes |
| --- | --- | --- |
| Weather source adapter | Fetch or import daily operational observations from approved provider | Not implemented. Must be provider-specific and testable. |
| Observation validator | Enforce 30-day, gap-free, finite-value, unit-compatible contract | Can be backend application service in future phase. |
| Python forecasting service | Load approved model/scaler and produce seven forecast rows | Already exists; do not retrain. |
| ASP.NET Core forecast service | Call Python service and persist seven forecast records | Already exists for supplied observations. |
| Forecast run selector | Select latest valid complete persisted run | Exists inside recommendation orchestration; can be reused conceptually for read-only forecast API. |
| Angular dashboard | Display stored latest forecast and limitations | Future read-only integration needed. |

## Manual Versus Scheduled Execution

### Manual Trigger

Manual triggering means an authorized operator supplies or imports the latest approved 30-day observation set and calls the existing forecast generation workflow.

Pros:

- lower implementation risk
- easier MSc demonstration control
- avoids background-job infrastructure
- easier to document provenance
- avoids accidental paid API calls

Cons:

- not automatic
- freshness depends on operator discipline
- less representative of production operations

### Scheduled Execution

Scheduled execution would periodically retrieve provider data and create a forecast run automatically.

Pros:

- better operational freshness
- closer to production workflow

Cons:

- requires scheduling/background-job infrastructure
- provider rate-limit/cost handling
- monitoring/retry/idempotency design
- higher risk of silent data-quality problems
- not currently approved

Phase 12 recommendation: manual trigger is sufficient for MSc demonstration, with scheduling deferred until owner explicitly approves infrastructure and provider integration.

## Idempotency And Duplicate-Run Design

Future duplicate prevention should be based on:

- provider identifier
- representative location/station identifier
- 30-day observation window start/end
- model version
- forecast date

Because the current database has no observation provenance table, full duplicate prevention may require future schema support. Until then, duplicate-run prevention can only be partially enforced by application checks against `ForecastRecord.ForecastRunId`, `ForecastDate`, `TargetDate`, `ModelVersion`, and `CreatedAt`.

## Failure Behavior

| Failure | Proposed behavior |
| --- | --- |
| Provider unavailable | Do not call LSTM; return safe service-unavailable message. |
| Missing daily observation | Block forecast generation; require owner-approved imputation before filling. |
| Invalid unit/value | Reject observation set with safe validation message. |
| Python service unavailable | Existing backend maps to service-unavailable. |
| Python malformed response | Existing backend maps to downstream error. |
| Persistence failure | Existing backend maps to safe persistence error. |
| Duplicate forecast run | Future service should return conflict or idempotent existing run response, pending design. |

## Security And Auditability

- Public users should not trigger weather-provider ingestion.
- Provider credentials, if any, must use secure configuration/User Secrets and must not be committed.
- Every forecast run should preserve model version and provider provenance.
- Logs must avoid secrets and connection strings.
- Forecast source limitations should be displayed to users and documented in dissertation limitations.

## Future Owner Approval Required

1. Provider selection.
2. Source adapter implementation.
3. Any credential configuration.
4. Any provider API calls in automated tests.
5. Any database schema change for provenance.
6. Automatic scheduling/background jobs.
7. New backend implementation work beyond this design.
