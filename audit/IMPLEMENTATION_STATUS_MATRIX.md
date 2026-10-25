# Implementation status matrix

Checked 25 October 2026 against the working tree, local development services and the evidence in `evidence/`. `TESTED_PASS` describes a technical behavior observed in this run. It does not mean agronomic validation or supervisor approval. `CODE_PRESENT` means implementation exists without a fresh end-to-end check. Historical results remain dated in `docs/qa/QA_TEST_CASES.md`.

| Function | Status | Current evidence and limit |
| --- | --- | --- |
| Public seven-day stored forecast | TESTED_PASS | `src/frontend/src/app/features/public-dashboard/public-dashboard.component.ts`, `src/backend/AnuradhapuraAI.Api/Controllers/ForecastsController.cs:10`, `evidence/live-api-summary.json`, live browser screenshots: seven days. |
| Forecast provenance/freshness display | TESTED_PASS | Dashboard component and live browser light/dark images in `evidence/`. |
| Historical ingestion, cleaning, chronology and train-only scaler | TESTED_PASS | `src/forecasting-service/forecasting_service/training/train.py:31-55`; Python 25/25 tests; dataset 5,844 continuous daily rows. Current run did not retrain. |
| Saved `v1` LSTM model and scaler | TESTED_PASS | `src/forecasting-service/artifacts/v1_era5_2010_2025/`; running `/health` reported Healthy, `/forecast` returned 7 dated values from 30 real historical rows. |
| Fresh external observation → Python → SQL run | TESTED_PASS | `evidence/live-staff-and-forecast-summary.json`: local Administrator endpoint fetched 30 days (2026-09-25–2026-10-24), generated seven values through Python, persisted a new operational run in LocalDB, and latest API returned seven days. Temporary process overrides enabled integration; this does not establish ERA5/Open-Meteo equivalence or predictive accuracy. |
| Public six-crop assessment | TESTED_PASS | `src/backend/AnuradhapuraAI.Api/Controllers/RecommendationsController.cs:19`; live API returned 6 `Complete` crops and 6 `Unsuitable` categories; desktop/mobile browser rendered all six and the warning. Scientific qualification is in `SCIENTIFIC_EVIDENCE_GAPS.md`. |
| Evidence-gated partial assessment | TESTED_PASS | `src/backend/AnuradhapuraAI.Infrastructure/Recommendations/RecommendationOrchestrationService.cs`, backend tests; browser fixture covers display. Not triggered by the current live configuration. |
| Four factor details and optional soil | TESTED_PASS | `src/frontend/src/app/features/recommendation/recommendation-page.component.ts`; browser fixture and backend tests. No real soil selection was entered in this run. |
| Approved climate-risk indicators | CODE_PRESENT | `src/backend/AnuradhapuraAI.Infrastructure/Recommendations/RecommendationOrchestrationService.cs`; controlled by `EnableClimateRiskIndicators`. No fresh risk-triggering live case was recorded. |
| User registration and login | TESTED_PASS | `src/backend/AnuradhapuraAI.Api/Controllers/AuthenticationController.cs`; disposable local QA account in `evidence/live-registered-summary.json`. |
| Registered recommendation save/history/detail | TESTED_PASS | `RecommendationsController.cs:61,99,123`; local QA save/list/detail passed after null-factor parser fix. |
| Own-history isolation and staff-route denial | TESTED_PASS | Backend tests and local Registered User 403 responses for admin/officer endpoints; history ownership tests in `RecommendationHistoryEndpointTests.cs`. |
| Officer review/status/comment/history | TESTED_PASS | Fresh local Agricultural Officer login listed pending items, retrieved all six crop evidence rows, submitted `Needs Review` for a disposable recommendation, and received 403 from admin crops. See `evidence/live-staff-and-forecast-summary.json`. |
| Administrator crop and configuration management | TESTED_PASS | Fresh local Administrator login passed `/api/auth/admin-check`, listed six crops, received 400 for an unsupported crop, and generated the operational forecast. Backend 301-pass suite also passed. |
| Admin crop-name scope enforcement | TESTED_PASS | `src/backend/AnuradhapuraAI.Infrastructure/Admin/AdminManagementService.cs:35-58`; `Phase4AdminServiceTests.cs` rejects unapproved create/rename. |
| Date-range calendar/climate output | TESTED_PASS | `SeasonalRecommendationsController.cs:8`, `SeasonalClimateService.cs:23`; live 6-crop `CALENDAR_SUPPORTED_CANDIDATES`, browser display, invalid-range 400. |
| Crop-based approximate months | TESTED_PASS | `SeasonalRecommendationsController.cs:9`, `SeasonalClimateService.cs:33`; six crop calls returned 200 with conditional status and zero or one November candidate windows. |
| Anonymous staff API protection | TESTED_PASS | Local `/api/admin/users` and `/api/validations/pending` each returned 401. |
| Unexpected API error response | TESTED_PASS | `Program.cs` returns fixed JSON for unhandled exceptions; `UnhandledExceptionEndpointTests.cs` verifies no authorization marker or stack name in the body. |
| Unexpected API error response | TESTED_PASS | `Program.cs` returns fixed JSON for unhandled exceptions; `UnhandledExceptionEndpointTests.cs` verifies no authorization marker or stack name in the body. |
| Evidence source display | TESTED_PASS | Recommendation UI renders external HTTP(S) sources as links and local repository evidence paths as text; Angular unit test passed. |
| Mobile navigation, registration visibility, light/dark results | TESTED_PASS | Desktop/mobile Playwright and `evidence/` screenshots. |
| Independent agronomist validation of crop bands and calendar windows | BLOCKED | Owner-approved project configuration exists, but repository calendar fields say `agronomistValidated=false` and `productionApproved=false`; no independent sign-off supplied. The successful technical forecast run does not resolve this. |
| Supervisor approval of corrected diagrams and dissertation captions | BLOCKED | Corrected editable diagrams and PNGs are provided under `CORRECTED_DIAGRAMS/`; no signed approval record was found. |

No schema migration, model retraining or production configuration change was made in this run.
