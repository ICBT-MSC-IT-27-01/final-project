# Phase 15 Recommendation Evaluation

Generated: 2026-10-13

## Evaluation Scope

Phase 15 evaluated the recommendation workflow using controlled automated tests and the Phase 13 operational forecast foundation.

No new Open-Meteo request, LSTM inference, or administrator forecast trigger was executed during Phase 15.

## Read-Only Recommendation API

Endpoint:

- `POST /api/recommendations/evaluate`

Verified behavior:

- Public read-only access works.
- Agricultural Officer and Administrator identities may read evaluation output without creating history.
- Invalid district values return `400`.
- Forecast unavailable and database-read failures return safe service errors.
- Internal connection/configuration details are not exposed.
- Persistence service is not called by the read-only evaluation endpoint.

## History Endpoint

Endpoint:

- `POST /api/recommendations`

Verified behavior:

- Recommendation history creation remains separate from public evaluation.
- `EnableRecommendationHistory=false` returns a controlled service-unavailable response.
- Registered-user identity is trusted from authentication context, not from request-body spoofing.
- Agricultural Officers and Administrators are forbidden from using the public history-creation path.

## Real Operational Recommendation Status

Phase 13 verified Operational ForecastRun:

- `b2d45e38-72fd-4b3d-87b5-418891a485c2`

Phase 15 did not re-query the live development database because secure owner-local SQL credentials were not available to the Codex process without exposing secrets in chat. The application path remains tested through integration tests, and Phase 13 evidence remains the latest live operational forecast evidence.

Live agronomic recommendation validation is therefore `NOT VERIFIED`.

Expected behavior with current scientific configuration:

- If no active seven-day-compatible crop requirements exist, the API returns insufficient-evidence or partial-evidence crop results.
- It must not invent rankings from annual or seasonal values.

## Performance and Repeatability

Automated test coverage includes:

- 100 repeated suitability evaluations under a prototype budget.
- deterministic repeated outputs for the same input.
- independent weighted reference calculation for the approved scoring algorithm.

Observed backend test result:

- 281 passed, 0 failed, 0 skipped.
