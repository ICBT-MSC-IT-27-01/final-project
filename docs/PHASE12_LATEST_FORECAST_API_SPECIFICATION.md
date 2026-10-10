# Phase 12 Latest Forecast API Specification

Status: DESIGN ONLY - NOT IMPLEMENTED

This document proposes a public read-only API for retrieving the latest valid complete persisted forecast run. No controller, service, DTO, route, test, migration, or Angular feature is implemented by this document.

## Proposed Endpoint

```text
GET /api/forecasts/latest
Authorization: Anonymous allowed
```

Rationale:

- Public users are allowed to access core forecast information.
- Endpoint is read-only.
- Forecast generation remains separate and is not triggered by this endpoint.

## Feature Flag Behavior

Proposed owner decision:

- `EnableWeatherModelIntegration = false` should block forecast generation through `POST /api/forecasts`.
- Read-only latest persisted forecast access may remain available even when generation is disabled, if owner approves.

Alternative:

- return `503 Feature disabled` when `EnableWeatherModelIntegration` is false.

This behavior requires owner approval before implementation.

## Response DTO

Proposed success response:

```json
{
  "forecastRunId": "00000000-0000-0000-0000-000000000000",
  "forecastDate": "YYYY-MM-DD",
  "targetStartDate": "YYYY-MM-DD",
  "targetEndDate": "YYYY-MM-DD",
  "modelVersion": "v1",
  "createdAt": "UTC timestamp",
  "freshness": {
    "status": "Fresh | Stale | Unknown",
    "ageHours": 0,
    "stalenessThresholdHours": 72
  },
  "source": {
    "provider": "Unknown until future provenance schema",
    "productType": "Unknown until future provenance schema",
    "limitations": [
      "Existing ForecastRecord rows do not store operational observation-source provenance."
    ]
  },
  "days": [
    {
      "targetDate": "YYYY-MM-DD",
      "rainfall": 1.2,
      "temperature": 27.0,
      "humidity": 80.0
    }
  ],
  "limitations": [
    "Forecast values are AI model outputs from the approved v1 LSTM.",
    "Forecast uncertainty is not quantified by the current model.",
    "Source observation provenance is limited unless future schema support is approved."
  ]
}
```

Freshness thresholds are proposed only. No threshold is approved by Phase 12.

## Complete-Run Eligibility

The endpoint should reuse Phase 8B-compatible latest-complete-run semantics:

1. `ForecastRunId` is non-empty.
2. Exactly seven records exist for the run.
3. Seven distinct `TargetDate` values exist.
4. Target dates are consecutive daily dates.
5. All records share one `ForecastDate`.
6. All records share one `CreatedAt`.
7. All records share one `ModelVersion` value, treating null consistently.
8. Rainfall is finite and non-negative.
9. Humidity is finite and between 0 and 100 inclusive.
10. Temperature is finite.
11. No mixed-run aggregation is allowed.
12. Latest valid run selection orders by `CreatedAt DESC`, then deterministic `ForecastRunId` string ascending.

## Error Responses

| Scenario | Proposed HTTP status | Safe response |
| --- | ---: | --- |
| No forecast records | 503 | `{ "message": "No complete forecast is available." }` |
| Only incomplete/invalid runs exist | 503 | `{ "message": "No complete forecast is available." }` |
| Forecast is stale | 200 with freshness status or 409/503 if owner wants hard blocking | Owner decision required |
| Database unavailable | 503 | `{ "message": "Forecast data is temporarily unavailable." }` |
| Malformed stored data | 503 | `{ "message": "No complete forecast is available." }` |
| Feature disabled | 503 if owner chooses read-blocking flag behavior | `{ "message": "Weather forecast access is disabled." }` |
| Unexpected exception | 500 | `{ "message": "Forecast request failed." }` |

Do not expose stack traces, SQL statements, connection strings, local file paths, or secret values.

## Database Compatibility

Currently supported:

- `ForecastRunId`
- seven records per run
- `ForecastDate`
- `TargetDate`
- rainfall, temperature, humidity
- `ModelVersion`
- `CreatedAt`
- unique `(ForecastRunId, TargetDate)`

Can be derived safely:

- target start date: min `TargetDate`
- target end date: max `TargetDate`
- complete-run validity from records
- latest valid run by `CreatedAt`

Not currently supported:

- operational weather provider name
- source product type
- source retrieval timestamp
- source observation window
- data quality status
- missing-data indicators
- provider request URL or citation
- station identifier

## Future Schema Change Candidates

Not required for a minimal read-only endpoint, but recommended before claiming full provenance:

- `ForecastSourceProvider`
- `ForecastSourceProduct`
- `ObservationWindowStartDate`
- `ObservationWindowEndDate`
- `SourceRetrievedAt`
- `SourceLocationId`
- `SourceLatitude`
- `SourceLongitude`
- `DataQualityStatus`
- `SourceProvenanceJson`

Any of these requires separate owner approval and migration work.

## Frontend Compatibility

Future Angular dashboard should:

- call `GET /api/forecasts/latest`
- render seven rows
- display forecast run ID and model version
- display freshness/provenance limitations
- handle unavailable forecasts without synthetic weather values
- avoid claiming live weather if the source is persisted AI forecast output
