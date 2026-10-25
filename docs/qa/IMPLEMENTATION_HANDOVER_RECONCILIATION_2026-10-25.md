# Implementation handover reconciliation — 25 October 2026

This note records checks made while implementing the diagram-aligned UI and crop-scope safeguards. It is not the complete read-only audit requested by `docs/Codex_Project_Implementation_and_Diagram_Audit_Handover.md` and does not establish agronomic validation.

## Three user outputs

| Output | Current path | Evidence in this run | Boundary |
| --- | --- | --- | --- |
| Seven-day LSTM weather forecast | `PublicDashboardComponent` → `GET /api/forecasts/latest` → persisted forecast | Local API returned seven daily forecasts with `Operational` freshness. The UI now exposes run ID, generation time, status and model version. | No fresh Python inference or external ingestion was executed in this run. |
| Date-range crop planning | `SeasonalPlanningComponent` → `POST /api/seasonal-recommendations/by-date-range` → `SeasonalClimateService` → historical CSV and crop calendar | For 1–30 November 2026, local API returned `CALENDAR_SUPPORTED_CANDIDATES` and six crop entries. | Conditional calendar candidates, separate from seven-day LSTM output and full suitability. |
| Crop-based cultivation-period planning | `SeasonalPlanningComponent` → `POST /api/seasonal-recommendations/by-crop` → `SeasonalClimateService` → historical CSV and crop calendar | For Paddy, 1–30 November 2026, local API returned `CALENDAR_SUPPORTED_CANDIDATE` with one approximate window. | No exact sowing date, irrigation assessment, or agronomist validation is claimed. |

The local evidence files are present: `docs/anuradhapura_weather_2010_2025_era5.csv` has `Date,Temperature,Rainfall,Humidity` columns (SHA-256 `DB88D3072A4336F2ECC5D177ADC9CC940BFACFE870221733F62A8DC36D9A3E97`); `docs/English calendar images/evidance json/DOA_SIX_CROP_CALENDAR_MONTH_EXTRACTION_REVIEW.json` has a six-crop calendar structure (SHA-256 `495C9B10AB30A092C8F6F5096470BF472BE6E72B87880A6A0C6DF4207AB148F6`). Presence and successful parsing do not prove source provenance or expert approval.

## Suitability and scope

`POST /api/recommendations/evaluate` returned six `Complete` crop evaluations and six ranks against the local persisted forecast. All six categories were `Unsuitable`; the UI now states that rank does not imply suitability. Factor details show configured weights and explanations. The administrator API now rejects crop creation or renaming outside the six approved names. No agricultural thresholds, weights, evidence records, database schema, or model artifacts were changed in this implementation.

## Checks and remaining evidence

- Frontend production build passed; 52 frontend tests passed.
- ASP.NET Core Release test suite passed: 300 tests, including nine admin-service tests. Debug API build passed with zero warnings and errors.
- At 390 px width, browser check confirmed the public menu and registration link are visible and the seasonal route opens.
- The local API was restarted against the existing local development database. No account creation, forecast generation, seed change, or external weather request was performed.
- Registered User, Agricultural Officer, and Administrator browser workflows were not re-exercised in this run. Earlier QA records should be read with their own dates and test environments.
- Calendar and crop requirement provenance, geographic applicability, and agronomist validation remain separate scientific review tasks. The historical QA notes must not be closed solely from HTTP success or code presence.
