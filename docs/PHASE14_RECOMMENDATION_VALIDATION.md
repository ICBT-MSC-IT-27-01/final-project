# Phase 14 Recommendation Validation

Generated: 2026-10-13

## Technical Validation

Phase 14 completed the technical recommendation workflow on top of the Phase 13 operational forecast foundation.

Implemented and verified:

- Latest eligible `Operational` forecast selection is reused through the existing orchestration service.
- `HistoricalEvaluation`, `LegacyUnknown`, malformed, and incomplete runs remain excluded by forecast selection tests.
- The public recommendation page uses a read-only evaluation endpoint.
- The persistence endpoint remains available for approved history behavior, but is fail-closed unless `EnableRecommendationHistory` is enabled.
- Ranking remains deterministic and only ranks crops with evaluable scores.
- Missing soil information is handled as unavailable evidence, not fabricated data.
- Agricultural Officer validation remains separate from crop configuration and forecast generation privileges.

## API Behavior

Read-only evaluation:

- Endpoint: `POST /api/recommendations/evaluate`
- Access: public read-only, plus existing authenticated roles.
- Side effects: no recommendation history record is created.
- Output: forecast reference, six crop evaluations where configuration permits, factor breakdown, missing-data indicators, limitations.

History persistence:

- Endpoint: `POST /api/recommendations`
- Access: existing public/registered path.
- Side effects: creates recommendation history only when `EnableRecommendationHistory` is enabled.
- Default behavior: fail-closed with service-unavailable response when recommendation history is disabled.

## Frontend Behavior

The Angular recommendation page now calls the read-only evaluation endpoint. It displays:

- Forecast run identifier.
- Forecast period and model version.
- Seven forecast days.
- Crop scores and classifications where available.
- Unranked insufficient-evidence states.
- Missing factor indicators.
- Soil-information warning when no soil type is supplied.
- Backend limitations returned in the response.

## Database Behavior

No Phase 14 migration was added.

No new live recommendation rows were required for Phase 14 technical validation because the public user workflow now has a read-only evaluation path. The history-creating endpoint remains gated by configuration and is covered by automated tests.

## Agronomic Validation

Agronomic/scientific validation remains LIMITED.

Reasons:

- No repository-approved crop requirement thresholds were available for all six crops.
- Reviewed public sources do not provide a complete, directly mappable 7-day scoring configuration.
- The Phase 13 LSTM predictive accuracy remains NOT VERIFIED.
- One operational forecast run is not evidence of long-term crop recommendation accuracy.

The system is technically ready to consume verified crop requirements, but it must not present unverified thresholds as scientific truth.

## Regression Evidence

Executed Phase 14 checks:

- Backend build: PASS.
- Backend tests: 272 passed, 0 failed, 0 skipped.
- Angular tests: 39 passed, 0 failed.
- Angular production build: PASS.
- Python tests: 22 passed, 0 failed, 1 upstream deprecation warning.

Browser E2E was not executed during Phase 14.
