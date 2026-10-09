# Phase 8A Recommendation Workflow Specification

## 1. Purpose And Scope

Status: **PHASE 8A APPROVED / FROZEN**

This document defines the proposed Phase 8 crop recommendation workflow for the **AI-Driven Weather Forecasting and Crop Recommendation System for Anuradhapura District**.

Phase 8A is the approved recommendation workflow design. It does not by itself implement recommendation generation, ranking services, persistence services, API controllers, officer validation, or Angular UI.

### Already Approved

- Geographic scope: Anuradhapura District, Sri Lanka.
- Supported crops exactly:
  1. Paddy
  2. Maize
  3. Green Gram
  4. Cowpea
  5. Groundnut
  6. Chilli
- Forecast variables: Rainfall, Temperature, Humidity.
- Forecast horizon: 7 days.
- Weather model: approved Python LSTM model from Phase 5.
- Suitability factors exactly: Rainfall, Temperature, Humidity, Soil compatibility.
- Suitability categories exactly: Highly Suitable, Suitable, Moderately Suitable, Unsuitable.
- Climate-risk indicators exactly: Low rainfall, Heavy rainfall, High temperature.
- Recommendation history is derived from `Recommendation` records. There is no separate `RecommendationHistory` table.
- Agricultural Officer validation belongs to Phase 9.
- Angular UI belongs to Phase 10.

### Approved / Frozen Phase 8A Workflow Decisions

1. Use the latest valid complete seven-day persisted forecast run, never mixing runs.
2. `SoilType` is optional. Unavailable or unmatched soil is not evaluable, and existing Phase 7B missing-factor normalization applies.
3. Always represent all six approved crops.
4. Rank evaluable crops by `OverallScore` descending, then evidence coverage, then fixed approved crop order. Clearly label partial evidence. Insufficient-evidence crops have no rank.
5. Persist both public and registered recommendations. Public `UserId` is `NULL`; public history is unavailable.
6. Registered users access only their own recommendation history/details.
7. Unavailable factor scores are `NULL`. For insufficient evidence, `OverallScore`, `SuitabilityCategory`, and `Rank` are `NULL`.

### Approved / Frozen Phase 8A Schema Decisions

1. Add `ForecastRunId` to `ForecastRecord` and `Recommendation`; do not add a separate `ForecastRun` table.
2. Make factor scores, `OverallScore`, `SuitabilityCategory`, and `Rank` nullable on `RecommendationCrop`.
3. Add `EvaluationStatus` with exactly `Complete`, `Partial`, and `InsufficientEvidence`.
4. Add versioned JSON evidence snapshots on `Recommendation` and `RecommendationCrop`.
5. Use a filtered unique rank index; retain uniqueness of `RecommendationId + CropId`.
6. Support public recommendation persistence; do not add a public historical retrieval/token system in this phase.

### Out Of Scope For Phase 8

- New crops, roles, suitability factors, suitability categories, or climate risks.
- Crop suitability formula changes.
- LSTM retraining or weather-model changes.
- External live weather-provider selection.
- Automatic soil acquisition, soil sensors, soil APIs, NPK, organic matter, irrigation, yield prediction, pest/disease, market price, mobile app, or Angular UI.
- Agricultural Officer validation actions.

## 2. Existing Architecture And Reusable Components

Status: **ALREADY APPROVED / INSPECTED**

The backend follows the approved practical structure:

- `AnuradhapuraAI.Api`: presentation/controllers.
- `AnuradhapuraAI.Application`: application contracts and business workflow.
- `AnuradhapuraAI.Domain`: domain entities and approved constants.
- `AnuradhapuraAI.Infrastructure`: EF Core, SQL Server, external service integration.

Phase 8B should keep controllers thin and place recommendation orchestration in the Application/Infrastructure boundary. Recommendation scoring must reuse the Phase 7B suitability engine instead of duplicating suitability logic.

Reusable inspected components:

- `CropSuitabilityEngine`
- `ICropSuitabilityEngine`
- `SuitabilityConfigurationProvider`
- `ISuitabilityConfigurationProvider`
- `SuitabilityEvaluationRequest`
- `SuitabilityEvaluationResponse`
- `ForecastRecord`
- `Recommendation`
- `RecommendationCrop`
- `RecommendationValidation`
- Admin read-only recommendation listing/detail endpoints
- `POST /api/forecasts` from Phase 6
- JWT/RBAC policies: `AuthenticatedUser`, `AgriculturalOfficerOnly`, `AdministratorOnly`

## 3. Proposed Recommendation Workflow

Status: **APPROVED / FROZEN**

The Phase 8B workflow should be:

```text
Public or Registered User
    ->
POST /api/recommendations
    ->
Recommendation Application Service
    ->
Resolve approved seven-day forecast
    ->
Load six approved crops and active suitability configuration
    ->
Call CropSuitabilityEngine
    ->
Build six crop result records with evidence coverage
    ->
Apply approved deterministic ranking rule
    ->
Return recommendation response
    ->
Persist recommendation where approved/required
```

Detailed proposed steps:

1. Accept a recommendation request from an anonymous public user or an authenticated registered user.
2. Resolve the seven-day forecast using an approved forecast-source strategy.
3. Accept an optional selected soil type.
4. Load active crop requirements, soil compatibility entries, weights, and thresholds through `SuitabilityConfigurationProvider`.
5. Evaluate exactly the six approved crops through `CropSuitabilityEngine`.
6. Preserve factor availability and evidence coverage for every crop.
7. Rank only crop results that have an evaluable overall score, using the approved Phase 8 ranking rule.
8. Return all six crop results with clear statuses.
9. Persist recommendation data according to the approved public/registered persistence decision.
10. Allow registered users to retrieve their own saved recommendation history/details when history is enabled.

## 4. Forecast Source Alternatives

Status: **APPROVED / FROZEN**

Phase 6 currently exposes `POST /api/forecasts`, which requires exactly 30 chronological observed-weather days. It persists seven individual `ForecastRecord` rows. The final runtime provider for recent observed-weather input remains unresolved.

### Option A: Consume Existing Persisted Forecast

Phase 8 uses an already persisted seven-day forecast from `ForecastRecord`.

Pros:

- Fits the current separation between forecast generation and recommendation generation.
- Avoids inventing a live recent-weather provider.
- Does not require recommendation callers to supply 30 historical observations.
- Keeps Phase 8 focused on recommendation orchestration.

Limitations:

- `ForecastRecord` currently stores individual forecast days and has no batch identifier.
- The selection rule for "approved/current forecast" is not yet frozen.
- Forecast provenance can be inferred by `ForecastDate`, `ModelVersion`, and seven target dates, but there is no direct `Recommendation` foreign key.

### Option B: Receive Forecast Reference Or Identifier

Phase 8 receives a forecast reference from the client.

Pros:

- Makes provenance explicit at request time.
- Could support a future forecast selection UI.

Limitations:

- There is no current forecast batch/reference entity.
- Passing individual `ForecastRecord` IDs for seven rows is awkward and fragile.
- A robust identifier may require a schema change or a forecast batch concept, which is not currently approved.

### Option C: Invoke Phase 6 Forecast Service With 30 Historical Observations

Phase 8 accepts or obtains 30 observations and invokes the existing Phase 6 forecast path.

Pros:

- Uses the approved LSTM inference path.
- Useful for controlled tests using historical fixtures.

Limitations:

- Runtime recent-weather acquisition is unresolved.
- Public users should not be forced to provide 30 daily observations unless that UX is explicitly approved.
- Phase 8 must not invent or silently select an external weather provider.

### Recommended Phase 8B Direction

Status: **PROPOSED**

Use **Option A** as the default Phase 8B design: recommendation generation consumes an existing approved persisted seven-day forecast set.

Approved forecast selection rule:

- use the latest valid complete seven-day persisted forecast run, grouped by a single non-empty `ForecastRunId`;
- require exactly seven records;
- require seven consecutive `TargetDate` values with no duplicates;
- require complete and valid rainfall, temperature, and humidity values;
- require consistent run metadata;
- order valid runs by reliable run creation timestamp, with deterministic tie-breaking.

This avoids new external data-provider decisions. If no complete persisted forecast exists, return a controlled `ForecastUnavailable` result.

## 5. Soil Input Boundary

Status: **APPROVED / FROZEN**

Phase 7B supports a supplied/selected soil type. Automatic soil acquisition remains unresolved.

The recommendation request should allow:

```text
SoilType: string? optional
```

Behavior:

- If `SoilType` is supplied, Phase 8 passes it to `CropSuitabilityEngine`.
- If a crop has a matching active `SoilCompatibility` entry, soil is evaluable.
- If no matching compatibility exists, soil is not evaluable for that crop.
- If `SoilType` is not supplied, soil is not evaluable for all crops.
- Missing soil must not be converted to `0` or `100`.
- Missing soil must be reported in evidence coverage and explanations.

No automatic soil detection, external soil API, sensor input, NPK, organic matter, or irrigation data is introduced by Phase 8.

## 6. Six-Crop Evaluation

Status: **APPROVED / FROZEN**

Phase 8 must evaluate the fixed MSc crop scope exactly:

- Paddy
- Maize
- Green Gram
- Cowpea
- Groundnut
- Chilli

Recommended response semantics:

- Return one crop result for each of the six approved crops.
- Do not silently omit crops with incomplete evidence.
- Each crop result should include an evaluation status.
- Crops with at least one evaluable factor may have `OverallScore` and `SuitabilityCategory`.
- Crops with no evaluable factors should have `OverallScore = null`, `SuitabilityCategory = null`, `Rank = null`, and status `InsufficientEvidence`.
- Crops with invalid configuration should be reported through a controlled failure rather than mixed into a normal recommendation response.

Suggested result statuses:

- `Evaluated`: all expected currently configured factors were evaluable.
- `PartiallyEvaluated`: at least one factor was evaluable and at least one factor was unavailable.
- `InsufficientEvidence`: no factor was evaluable.

Status labels are proposed API/application concepts, not new suitability categories.

## 7. Ranking Design Alternatives

Status: **APPROVED / FROZEN**

The ranking rule must be deterministic, explainable, and based on the Phase 7B suitability result. It must not introduce new agronomic factors or weights.

### Alternative 1: Score Only

Rank crops by:

1. `OverallScore` descending.
2. Fixed approved crop order for ties.

This is simple, but it may place a partially evaluated crop with one factor above a fully evaluated crop without visibly accounting for evidence coverage in the ordering.

### Alternative 2: Score Plus Evidence Coverage Tie-Break

Rank crops by:

1. `OverallScore` descending.
2. Evaluated factor count descending.
3. Total configured weight coverage descending.
4. Fixed approved crop order.

This keeps `OverallScore` primary while making equal-score ties more transparent.

### Alternative 3: Full-Evidence Priority

Rank crops by:

1. Evidence completeness class.
2. `OverallScore` descending.
3. Fixed approved crop order.

This avoids treating partial evidence as equally reliable, but it may demote a strong partial result below a weaker full-evidence result.

### Recommended Phase 8B Direction

Use **Alternative 2**:

```text
Eligible for ranking:
    OverallScore is not null

Sort:
    OverallScore descending
    EvaluatedFactorCount descending
    EvaluatedConfiguredWeightTotal descending
    Approved crop order ascending

Rank:
    assign 1..N after sorting
```

Crops with no evaluable factors are not ranked. They remain in the response with `Rank = null` and `InsufficientEvidence`.

This ranking design is approved for the future Phase 8B recommendation workflow implementation.

## 8. Evidence Coverage And Insufficient Evidence

Status: **APPROVED / FROZEN**

Every response should make clear which evidence was used and which evidence was unavailable.

Suggested evidence coverage fields:

```text
EvaluatedFactorCount
TotalFactorCount
ConfiguredWeightTotalEvaluated
UnavailableFactors[]
FactorResults[]
```

Each factor result should include:

- factor name
- whether it was evaluable
- score if evaluable
- aggregated forecast value if relevant
- configured weight
- effective weight if evaluable
- explanation

Response classifications:

- **Valid recommendation**: at least one crop has an overall score and ranking can be produced.
- **Partially evaluated suitability**: some crops/factors were evaluated, but evidence gaps remain.
- **Insufficient evidence**: no crop can produce an overall score.
- **Invalid configuration**: active configuration exists but violates validation rules.
- **Unavailable forecast**: no complete approved seven-day forecast is available.

Do not fabricate crop scores, categories, or rankings to fill gaps.

## 9. Persistence Model

Status: **APPROVED DESIGN / IMPLEMENTATION PENDING**

Existing entities:

- `Recommendation`
  - `Id`
  - `UserId` nullable
  - `CreatedAt`
  - navigation to `User`
  - navigation to `RecommendationCrops`
  - navigation to `RecommendationValidations`
- `RecommendationCrop`
  - `Id`
  - `RecommendationId`
  - `CropId`
  - `RainfallScore` required decimal
  - `TemperatureScore` required decimal
  - `HumidityScore` required decimal
  - `SoilScore` required decimal
  - `OverallScore` required decimal
  - `SuitabilityCategory` required string
  - `Explanation` required string
  - `Rank` required int
- `ForecastRecord`
  - stores individual persisted forecast days with `ForecastDate`, `TargetDate`, weather variables, nullable `ModelVersion`, and `CreatedAt`.
- `RecommendationValidation`
  - exists for future Phase 9 validation and points to `Recommendation`.

### Public Recommendation Persistence

Status: **APPROVED / FROZEN**

Approved behavior:

- Public recommendations are persisted with `UserId = null`.
- Public users receive the immediate generated result.
- Public users do not receive historical retrieval or history endpoints in this phase.
- No public retrieval token system is added in this phase.

### Registered Recommendation Persistence

Status: **PROPOSED**

For authenticated registered users:

- Persist `Recommendation.UserId` as the current user ID.
- Persist `Recommendation.CreatedAt`.
- Persist one `RecommendationCrop` row per approved crop if schema supports nullable/unranked results.
- Registered recommendation history is derived from `Recommendation` rows filtered by current `UserId`.
- Users must not read another user's recommendation details/history.

### Forecast Provenance

Status: **APPROVED / FROZEN**

Approved schema design:

- Add `ForecastRunId` to `ForecastRecord` and `Recommendation`.
- Do not add a separate `ForecastRun` table in this phase.
- Do not add a `ForecastRecord -> Recommendation` foreign key.
- Store recommendation-level evidence in a versioned JSON snapshot.

## 10. Required Schema Compatibility Review

Status: **APPROVED SCHEMA DESIGN / IMPLEMENTATION VERIFICATION REQUIRED**

Phase 7B permits unevaluable factor scores and nullable overall/category results. Current `RecommendationCrop` persistence does not.

### Conflict 1: Nullable Factor Scores

Current fields are non-nullable:

- `RainfallScore`
- `TemperatureScore`
- `HumidityScore`
- `SoilScore`

Phase 8 must not store `0` or `100` when a factor is not evaluable.

Minimum proposed schema change:

- Make the four factor score columns nullable.

### Conflict 2: Overall Score And Category

Current fields are non-nullable:

- `OverallScore`
- `SuitabilityCategory`

If a crop has no evaluable factors, Phase 7B returns no overall score and no category.

Minimum proposed schema change:

- Make `OverallScore` nullable.
- Make `SuitabilityCategory` nullable or add an explicit evaluation status and only require category when overall score exists.

### Conflict 3: Rank

Current field:

- `Rank` required int
- unique index on `RecommendationId + Rank`

If all six crops are represented and insufficient-evidence crops are not ranked, `Rank` must be nullable or unranked crops cannot be persisted.

Minimum proposed schema change:

- Make `Rank` nullable.
- Rework the unique rank index so multiple null ranks are allowed safely, or only enforce uniqueness for non-null rank values.

### Conflict 4: Evidence Coverage

Current `RecommendationCrop` has only `Explanation` for evidence coverage.

Minimum no-schema option:

- Store a concise human-readable evidence summary in `Explanation`.

More explicit option requiring approval:

- Add an `EvaluationStatus` field.
- Add a structured `EvidenceSummary` or JSON field only if the owner approves the extra persistent metadata.

### Conflict 5: Forecast Provenance

Current `Recommendation` cannot store a durable link or summary of the forecast used.

Minimum proposed schema change, if durable provenance is required:

- Add nullable forecast provenance columns to `Recommendation`.

No schema change should be made during Phase 8A.

## 11. Public And Registered Access Control

Status: **PROPOSED**

### Public User

- Can call recommendation generation without authentication.
- Receives immediate forecast/suitability/recommendation response.
- Should not access saved recommendation history.
- Public persistence requires owner decision.

### Registered User

- Can call recommendation generation with authentication.
- Can retrieve own recommendation history when `EnableRecommendationHistory` is enabled.
- Can retrieve own recommendation details.
- Cannot retrieve another user's recommendation records.

### Administrator

- Existing admin read-only recommendation routes remain Administrator-only.
- Administrator can view recommendation records if persisted.

### Agricultural Officer

- No Phase 8 action endpoint.
- Existing `RecommendationValidation` is reserved for Phase 9.

## 12. Proposed API Contracts

Status: **PROPOSED / NOT IMPLEMENTED**

Use existing lowercase plural route style.

### Generate Recommendation

```text
POST /api/recommendations
Authorization: optional
```

Request DTO:

```json
{
  "soilType": "string or null",
  "forecastDate": "YYYY-MM-DD or null",
  "modelVersion": "string or null"
}
```

Notes:

- `soilType` is optional.
- `forecastDate` and `modelVersion` are optional if the approved rule is "latest complete forecast".
- If a forecast reference strategy is approved later, the request DTO should be adjusted before implementation.

Response DTO:

```json
{
  "id": 123,
  "persisted": true,
  "createdAt": "2026-10-08T00:00:00Z",
  "forecast": {
    "forecastDate": "2026-10-08",
    "modelVersion": "v1",
    "targetStartDate": "2026-10-09",
    "targetEndDate": "2026-10-15"
  },
  "soilType": "string or null",
  "overallStatus": "ValidRecommendation",
  "crops": [
    {
      "cropName": "Paddy",
      "rank": 1,
      "overallScore": 81.5,
      "suitabilityCategory": "Highly Suitable",
      "evaluationStatus": "PartiallyEvaluated",
      "evaluatedFactorCount": 2,
      "totalFactorCount": 4,
      "climateRisks": [],
      "factors": [],
      "explanation": "Human-readable explanation"
    }
  ]
}
```

The JSON above is an example contract shape only. It does not contain real agricultural recommendation values.

### Get Recommendation Detail

```text
GET /api/recommendations/{id}
Authorization: AuthenticatedUser
```

Behavior:

- Registered user can read only their own recommendation.
- Administrator access remains through existing `/api/admin/recommendations/{id}`.
- Public anonymous retrieval is not recommended unless a separate anonymous access-token design is approved.

### Get Registered User Recommendation History

```text
GET /api/recommendations/history
Authorization: AuthenticatedUser
```

Behavior:

- Returns only the current user's recommendation records.
- Does not expose another user's recommendations.
- Disabled cleanly if `EnableRecommendationHistory` is false.

## 13. Feature Flags

Status: **PROPOSED**

Existing approved flags:

- `EnableOfficerValidation`
- `EnableRecommendationHistory`
- `EnableClimateRiskIndicators`
- `EnableWeatherModelIntegration`

Phase 8 behavior:

- `EnableRecommendationHistory`
  - When enabled: registered-user recommendations may be persisted and history endpoints are available.
  - When disabled: recommendation generation may still return immediate results, but history endpoints return a controlled disabled response.
- `EnableClimateRiskIndicators`
  - Passed to `CropSuitabilityEngine`.
  - When disabled: no climate-risk indicators are returned.
- `EnableWeatherModelIntegration`
  - If Phase 8 invokes Phase 6 forecast generation, disabled means forecast generation is unavailable.
  - If Phase 8 consumes already persisted `ForecastRecord` data, owner should confirm whether the flag should also block persisted forecast use. Recommended: do not block read-only use of existing persisted forecasts, but report the forecast source clearly.
- `EnableOfficerValidation`
  - No Phase 8 validation actions are implemented.
  - May only affect whether later Phase 9 validation status is exposed.

Do not add new feature flags in Phase 8B unless explicitly approved.

## 14. Error Handling

Status: **PROPOSED**

Use controlled API responses consistent with current controller behavior. Do not expose stack traces, secrets, connection strings, local paths, or internal exception details.

Recommended errors:

- `ForecastUnavailable`: no complete approved seven-day forecast is available.
- `InvalidForecastReference`: requested forecast date/version does not identify a complete forecast set.
- `InvalidSoilInput`: supplied soil type fails validation, if validation rules are approved.
- `NoActiveCropConfiguration`: the six approved crop profiles cannot be loaded.
- `MissingAgriculturalRanges`: crop requirements are missing or not compatible with seven-day scoring.
- `NoEvaluableFactors`: no crop can produce an overall score.
- `InvalidSuitabilityConfiguration`: weights, thresholds, ranges, or compatibility scores are invalid.
- `PersistenceFailed`: recommendation records could not be saved.
- `Unauthorized`: authenticated user attempted to read another user's record.
- `RecommendationNotFound`: requested recommendation does not exist or is not visible to the current user.
- `FeatureDisabled`: requested history or integration behavior is disabled by feature flag.

Suggested HTTP mapping:

- 400 Bad Request: invalid request shape or invalid soil/reference input.
- 401 Unauthorized: authentication required but missing.
- 403 Forbidden: authenticated user lacks access.
- 404 Not Found: recommendation or forecast reference not found.
- 409 Conflict: missing/incomplete required configuration or no evaluable factors.
- 500 Internal Server Error: persistence failure with safe message only.
- 503 Service Unavailable: forecast integration unavailable where applicable.

## 15. Phase 8B Test Plan

Status: **PROPOSED**

### Unit Tests

- Recommendation service calls `CropSuitabilityEngine` rather than recalculating suitability.
- Exactly six approved crops are evaluated.
- No accidental crop-scope expansion.
- Ranking by descending score.
- Equal-score tie behavior.
- Partial-evidence ranking behavior.
- No-evaluable-factor crops remain unranked.
- Missing soil type marks soil unavailable.
- Missing crop requirements produce evidence gaps.
- Invalid suitability configuration returns controlled failure.
- `EnableClimateRiskIndicators` disables climate risks.
- `EnableRecommendationHistory` controls history behavior.

### Integration Tests

- Public recommendation generation succeeds when forecast/configuration are sufficient.
- Public recommendation generation does not require authentication.
- Registered recommendation generation associates records with the authenticated user.
- Registered history returns only the current user's records.
- Unauthorized cross-user detail access is rejected.
- Forecast unavailable returns controlled error.
- Persisted forecast set is resolved deterministically.
- Phase 7B engine output maps into recommendation DTOs without losing nullable factor-score semantics.

### Database-Backed Tests

- Recommendation persistence creates one parent `Recommendation`.
- Recommendation persistence records six crop results when schema supports nullable/unranked outputs.
- Nullable factor scores are stored as null when not evaluable.
- Rank uniqueness is preserved for ranked crops.
- Unranked insufficient-evidence crops do not violate unique rank constraints.
- No separate `RecommendationHistory` table is introduced.
- Existing admin recommendation read-only views still work after any approved schema adjustment.

### Regression Tests

- Existing Phase 6 forecast tests continue to pass.
- Existing Phase 7B suitability scoring tests continue to pass.
- Existing authentication/RBAC tests continue to pass.
- Python forecasting tests continue to pass.
- No LSTM retraining occurs during tests.

## 16. Risks And Limitations

Status: **UNRESOLVED**

- Runtime recent observed-weather acquisition remains unresolved.
- Current `ForecastRecord` has no forecast batch identifier.
- Current `Recommendation` has no durable forecast provenance fields.
- Current `RecommendationCrop` cannot represent nullable factor scores, nullable category/overall score, or nullable rank.
- Current real database has approved roles and six crops, but no approved real agricultural numeric requirement ranges, no approved real soil compatibility values, and no approved real suitability configuration records.
- Rainfall scoring remains limited to requirements explicitly compatible with seven-day forecast scoring.
- Humidity scoring remains limited to approved numeric humidity ranges compatible with seven-day mean aggregation.
- Soil input is manual/supplied only until a runtime soil acquisition method is approved.

## 17. Approved Decisions And Remaining Limits

Status: **APPROVED / FROZEN**

The seven workflow decisions and six schema decisions listed in Section 1 are frozen for the controlled Phase 8B schema foundation.

Remaining limitations:

1. The full Phase 8B recommendation workflow is not implemented by this specification.
2. No recommendation endpoints, ranking service, history service, or Angular UI are approved by this document alone.
3. Agricultural numeric configuration still requires separate approved values before real agronomic recommendations are presented.
4. Runtime recent-weather acquisition remains unresolved outside the persisted forecast-run boundary.

## 18. Explicit Phase 8A Completion Status

Status: **PHASE 8A APPROVED / FROZEN; PHASE 8B WORKFLOW NOT IMPLEMENTED**

This document:

- Defines the proposed recommendation workflow.
- Identifies forecast-source alternatives.
- Defines the soil input boundary.
- Defines six-crop evaluation expectations.
- Proposes deterministic ranking semantics.
- Identifies evidence coverage requirements.
- Maps existing persistence capabilities and conflicts.
- Proposes API contracts for Phase 8B.
- Defines feature-flag behavior and error handling.
- Provides a Phase 8B test plan.

This document does not:

- implement recommendation generation
- add API endpoints
- create migrations
- modify SQL Server data
- seed agricultural numeric values
- change the LSTM model or dataset
- implement officer validation
- implement Angular UI
- start Phase 8B, Phase 9, or Phase 10
