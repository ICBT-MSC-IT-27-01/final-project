# QA execution log

Run date: 25 October 2026. Workspace: Windows PowerShell, .NET SDK 10.0.101, Node 24.13.0, npm 11.6.2, Python virtual environment 3.12.13. Tests used local development services: Angular on `localhost:4200`, ASP.NET API on HTTP 5099 and HTTPS 7055, Python FastAPI on 8000, and local SQL Server LocalDB. No production environment was accessed. The exact data and calendar file checksums are in `THREE_OUTPUT_TRACEABILITY.md`.

| Check | Command or method | Result and evidence |
| --- | --- | --- |
| Backend unit/integration | `dotnet test tests/AnuradhapuraAI.Phase3Tests/AnuradhapuraAI.Phase3Tests.csproj --no-restore -c Release` | **301 passed, 0 failed** after fixes. Includes six-crop name restriction, recommendation history null factor, authorization, and safe unexpected-error response. |
| Angular unit | `npm test -- --watch=false` from `src/frontend` | **53 passed, 0 failed**. Includes local evidence path rendering as plain text. |
| Angular production build | `npm run build` from `src/frontend` | Passed. |
| Angular Playwright | `npm run e2e` from `src/frontend` | **15 passed, 1 skipped** (desktop variant of a mobile-only menu test). Uses mocked API responses for all roles; it does not prove fresh privileged live login. |
| Python forecasting | `.venv/Scripts/python.exe -m pytest -q` from `src/forecasting-service` | **25 passed**; one upstream Starlette warning. The local virtual environment needed pytest, httpx and pandas installed before this run. |
| Python live inference | `/health`; `/forecast` with the last 30 rows of the real historical CSV | Healthy and **seven dated forecasts** returned for 1–7 January 2026 with `v1` artifacts. This was an inference check, not a model retraining or an operational API persistence run. |
| Live public API | `audit/run-live-api-check.ps1` | Seven stored operational forecast days; six complete suitability assessments, all six `Unsuitable`; six date-range crop entries; six by-crop calls; past, >366-day and unsupported-crop requests returned 400; anonymous admin/officer requests returned 401. See `evidence/live-api-summary.json`. |
| Live registered API | `audit/run-live-registered-check.ps1` | Disposable local user registered and logged in, saved a six-crop recommendation, retrieved it through list and detail, and received 403 on admin/officer endpoints. See `evidence/live-registered-summary.json`. This created disposable local database records. |
| Live staff API | `audit/run-live-staff-and-forecast-check.ps1` | Disposable local Officer and Administrator accounts were provisioned in LocalDB. Officer listed pending items, reviewed six crop rows, submitted `Needs Review`, and received 403 on admin API. Administrator passed role check, listed six crops, received 400 for unsupported crop creation, and ran operational forecast generation. Credentials remained in process memory; evidence contains no credentials. |
| Fresh operational forecast | Same script; temporary process-only feature overrides | Open-Meteo supplied 30 days (2026-09-25 through 2026-10-24); Python `v1` produced seven days; ASP.NET persisted a new operational run to LocalDB; latest API returned seven days. This is technical integration evidence only; it does not establish ERA5/Open-Meteo equivalence or predictive accuracy. Committed feature flags were not changed for the run. See `evidence/live-staff-and-forecast-summary.json`. |
| Live browser | `node audit/run-live-public-check.cjs` | Desktop/mobile light/dark public pages and recommendation results rendered, including visible mobile registration and all-Unsuitable warning. Screenshots are in `evidence/`. |
| Backend dependency scan | `dotnet list tests/AnuradhapuraAI.Phase3Tests/AnuradhapuraAI.Phase3Tests.csproj package --vulnerable --include-transitive` | No vulnerable packages reported by current configured NuGet sources. Historical QA-005 is not reproduced by this scan. |
| Source hygiene | `git diff --check` | Exit code 0; Git emitted only line-ending conversion notices. |

The first registered-user live check found a real 500 on saved-recommendation detail: an optional Soil factor was JSON `null` and the parser called `TryGetDecimal` on it. `RecommendationHistoryService.ReadDecimal` and `ReadNestedDecimal` now check JSON kind before conversion; a regression fixture and a second live run passed. The development exception response also exposed request-header details during that failure. A global JSON exception handler now returns a fixed message; its integration test verifies that a request credential and stack name are absent from the body. No real bearer token is stored in this audit.

The earlier public-browser run failed only because the Angular proxy targeted HTTPS 7055 while the API was listening on HTTP 5099. The API was restarted on both configured local ports and the browser check passed. The final API process was rebuilt in Debug after the exception-handler change and is listening on 5099 and 7055.

## Still open

- Agronomist review of the six crop bands, soil applicability, calendar-month windows and local irrigation conditions; calendar evidence records are explicitly unapproved.
- Supervisor acceptance of the corrected diagrams or dissertation text. No dissertation DOCX was changed.
- Privileged browser login was not repeated; the live role and authorization workflows were exercised through API calls using disposable local accounts.

The current checks demonstrate technical operation only. `IMPLEMENTATION_STATUS_MATRIX.md` records the status and limits for each function.

## Configuration security finding

The tracked `src/backend/AnuradhapuraAI.Api/appsettings.Development.json` contained a remote database connection and signing key. Its working copy now uses LocalDB and a clearly local-only key. Since the original values were in a tracked file, the database credential and signing key should be rotated by their owner. Rotation and Git history rewriting were not attempted. Do not restore the previous values. The live QA run connected only to LocalDB.
