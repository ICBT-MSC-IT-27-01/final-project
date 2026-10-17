# Codex Project Handover

## 1. Project Identity

Project title: **AI-Driven Weather Forecasting and Crop Recommendation System for Anuradhapura District**

Project type: MSc development/research project

Geographic scope: Anuradhapura District, Sri Lanka

The system combines 7-day weather forecasting, explainable crop suitability and recommendation, recommendation history, optional Agricultural Officer validation, and administration/configuration features.

## 2. Fixed Crop Scope

The approved MSc crop scope is exactly:

1. Paddy
2. Maize
3. Green Gram
4. Cowpea
5. Groundnut
6. Chilli

Do not expand or replace this crop scope without explicit project-owner approval.

## 3. Weather Forecasting Scope

Approved forecasting scope:

- Forecast horizon: 7 days
- Variables: Rainfall, Temperature, Humidity
- Model type: LSTM
- Forecasting component: Python-based

The LSTM does **not** directly predict the recommended crop. The LSTM predicts future weather conditions. The crop suitability engine later consumes those forecast values and evaluates the six supported crops.

Conceptual flow:

```text
Historical Weather Data
    ->
Trained LSTM
    ->
7-Day Forecast
(Rainfall, Temperature, Humidity)
    ->
Crop Suitability Engine
    ->
Crop Environmental Requirements
+ Soil Compatibility
+ Configurable Weights/Thresholds
    ->
Suitability Scores
    ->
Suitability Category
    ->
Explanation / Recommendation
```

## 4. Crop Suitability / Recommendation Design

Crop recommendation is intentionally separate from the LSTM weather model.

Approved core suitability factors only:

- Rainfall suitability
- Temperature suitability
- Humidity suitability
- Soil compatibility

Approved categories:

- Highly Suitable
- Suitable
- Moderately Suitable
- Unsuitable

Approved climate-risk indicators:

- Low rainfall
- Heavy rainfall
- High temperature

Weights and thresholds must remain configurable and must not be hard-coded. Do not invent agricultural numeric thresholds.

## 5. User Roles

Approved roles:

- Public User
- Registered User
- Agricultural Officer
- Administrator

Responsibility boundaries:

- Public User: can access core weather forecast information and request/view crop recommendations without compulsory registration.
- Registered User: has public-user functionality, can register, log in, log out, view recommendation history, and view saved recommendation details associated with the authenticated user.
- Agricultural Officer: must authenticate, can review recommendations selected for validation, view forecast evidence and suitability-factor results, assign Pending Validation / Validated / Needs Review, and add an optional comment. Agricultural Officers are review/validation users and cannot edit crop requirements, soil compatibility, suitability weights, thresholds, or system configuration.
- Administrator: must authenticate, can manage crop profiles, environmental requirements, soil compatibility information, configurable suitability weights, configurable category thresholds, users and roles, recommendation records, validation records, and selected system configuration.

Normal registration creates only a Registered User. Users cannot self-assign Agricultural Officer or Administrator roles.

## 6. Approved Technology Stack

Frontend: Angular

Backend: ASP.NET Core Web API

Database: SQL Server

Forecasting: Python + TensorFlow/Keras LSTM

Approved logical flow:

```text
Angular
    ->
ASP.NET Core Web API
    ->
SQL Server
```

```text
ASP.NET Core Web API
    ->
Python Forecasting Component
    ->
Saved LSTM Model / Scaler
```

No specific cloud provider, deployment provider, IIS setup, Docker setup, or hosting platform has been frozen.

## 7. Backend Architecture

Approved logical three-tier architecture:

- Presentation
- Business/Application
- Data Access

Practical backend project structure:

- `AnuradhapuraAI.Api`
- `AnuradhapuraAI.Application`
- `AnuradhapuraAI.Domain`
- `AnuradhapuraAI.Infrastructure`

`Domain` belongs to the business/core side and does not represent a fourth deployment tier.

Important rules:

- Keep controllers thin.
- Keep business logic outside controllers.
- Use dependency injection.
- Keep EF Core/data-access responsibilities separated from business rules.
- Avoid unnecessary over-engineering for the MSc prototype.

## 8. Database Foundation

Approved ten entities:

1. UserRole
2. User
3. Crop
4. CropEnvironmentalRequirement
5. SoilCompatibility
6. SuitabilityConfiguration
7. ForecastRecord
8. Recommendation
9. RecommendationCrop
10. RecommendationValidation

Frozen database decisions:

- `CropEnvironmentalRequirement` uses variable-based rows.
- Supported variable types are `Rainfall`, `Temperature`, and `Humidity`.
- `ForecastRecord.ModelVersion` is nullable.
- There is no separate `RecommendationHistory` table.
- Recommendation history is derived from `Recommendation` records.
- There is no forced `ForecastRecord` -> `Recommendation` foreign key yet.
- Soil source/selection method remains unresolved and must not be invented.

## 9. Completed Development Phases

- Phase 1 - Solution / Project Structure: APPROVED / FROZEN
- Phase 2 - Database Foundation: APPROVED / FROZEN
- Phase 3 - Authentication / RBAC: APPROVED / FROZEN
- Phase 4 - Admin Configuration Foundation: APPROVED / FROZEN
- Phase 5A - ML Pipeline Foundation: APPROVED / FROZEN
- Phase 5B - Real Dataset Training and Evaluation: APPROVED / FROZEN
- Phase 5 - Weather Forecasting Model: COMPLETE / APPROVED / FROZEN
- Phase 6 - Backend Forecast Integration: COMPLETE / APPROVED / FROZEN
- Phase 7A - Agricultural Evidence and Suitability Scoring Definition: APPROVED / FROZEN
- Phase 7B - Crop Suitability Engine Implementation: IMPLEMENTED WITH RUNTIME CONFIGURATION MAPPING / PENDING PROJECT-OWNER REVIEW

Later phases through Phase 14 have now been implemented in this repository. The historical phase list above remains useful context, but `docs/PHASE13_FINAL_VERIFICATION_REPORT.md` is the current source of truth for operational forecast integration status and `docs/PHASE14_FINAL_VERIFICATION_REPORT.md` is the current source of truth for crop recommendation workflow status.

## 9A. Phase 14 Recommendation Status

Phase 14 completed the technical crop recommendation workflow while preserving scientific caution.

Implemented:

- Public read-only recommendation evaluation endpoint: `POST /api/recommendations/evaluate`.
- Recommendation history endpoint remains separate: `POST /api/recommendations`.
- Recommendation history persistence is fail-closed unless `FeatureFlags:EnableRecommendationHistory` is enabled.
- Angular recommendation page uses the read-only endpoint and displays forecast evidence, seven forecast days, crop rankings where evaluable, missing factor notices, and limitations.
- Agricultural Officer validation remains a review-only workflow and does not grant configuration, forecast, or model privileges.

Scientific status:

- Crop recommendation configuration architecture is ready.
- No new crop requirement thresholds were seeded in Phase 14 because the reviewed sources were not sufficient to create defensible 7-day scoring thresholds for all six crops.
- Scientific/agronomic validation remains LIMITED.
- LSTM predictive accuracy remains NOT VERIFIED.

Phase 14 documentation:

- `docs/PHASE14_FINAL_VERIFICATION_REPORT.md`
- `docs/PHASE14_CROP_REQUIREMENTS.md`
- `docs/PHASE14_RECOMMENDATION_VALIDATION.md`

Phase 14 verification commands:

- `dotnet build .\src\backend\AnuradhapuraAI.Api\AnuradhapuraAI.Api.csproj --no-restore` -> PASS, 0 warnings, 0 errors.
- `dotnet test .\tests\AnuradhapuraAI.Phase3Tests\AnuradhapuraAI.Phase3Tests.csproj --no-restore --logger "console;verbosity=minimal"` -> 272 passed, 0 failed, 0 skipped.
- `npm test -- --watch=false` from `src/frontend` -> 39 passed, 0 failed.
- `npm run build` from `src/frontend` -> PASS.
- `python -m pytest` from `src/forecasting-service` -> 22 passed, 0 failed, 1 upstream deprecation warning.

## 9B. Phase 15 Scientific Evaluation Status

Phase 15 completed dissertation-facing scientific and system evaluation work without fabricating agronomic thresholds.

Implemented/verified:

- Additional suitability reference-calculation tests.
- Annual rainfall context remains unscored for seven-day recommendations.
- Public read-only recommendation endpoint failure handling.
- Deterministic repeated scoring.
- Lightweight prototype scoring performance check.
- Forecast model artifact audit.
- Agricultural Officer expert-validation protocol.

Scientific status:

- Scientific validation: PARTIAL.
- Agronomic live recommendation validation: NOT VERIFIED.
- Agricultural Officer expert validation: NOT PERFORMED.
- Browser E2E: NOT EXECUTED.
- Predictive accuracy for operational recommendations: NOT VERIFIED.

Phase 15 documentation:

- `docs/PHASE15_FINAL_VERIFICATION_REPORT.md`
- `docs/PHASE15_SCIENTIFIC_CROP_REQUIREMENTS.md`
- `docs/PHASE15_TEMPORAL_COMPATIBILITY.md`
- `docs/PHASE15_RECOMMENDATION_EVALUATION.md`
- `docs/PHASE15_FORECAST_MODEL_EVALUATION.md`
- `docs/PHASE15_AGRICULTURAL_OFFICER_PROTOCOL.md`

Phase 15 verification commands:

- `dotnet build .\src\backend\AnuradhapuraAI.Api\AnuradhapuraAI.Api.csproj --no-restore` -> PASS, 0 warnings, 0 errors.
- `dotnet test .\tests\AnuradhapuraAI.Phase3Tests\AnuradhapuraAI.Phase3Tests.csproj --no-restore --logger "console;verbosity=minimal"` -> 281 passed, 0 failed, 0 skipped.
- `python -m pytest` from `src/forecasting-service` -> 22 passed, 0 failed, 1 upstream deprecation warning.
- `npm test -- --watch=false` from `src/frontend` -> 39 passed, 0 failed.
- `npm run build` from `src/frontend` -> PASS.

## 9C. Phase 16 System Validation Status

Phase 16 completed final technical validation and dissertation evidence preparation.

Implemented/verified:

- Minimal Playwright browser E2E framework using installed Chrome.
- Deterministic browser fixtures for public, registered, officer and administrator workflows.
- No live Open-Meteo, LSTM inference, SQL write or operational forecast generation during E2E.
- Requirements traceability matrix.
- UAT test plan and ethics participant pack.
- Deployment readiness checklist.
- Security/accessibility/performance/reliability evidence documents.
- Frontend focus-visible accessibility improvement.
- npm audit remediation for frontend dev dependency advisories.

Phase 16 status:

- Browser E2E: PASS.
- Real participant UAT: NOT PERFORMED.
- Agricultural Officer expert validation: NOT PERFORMED.
- Ethics approval: PENDING.
- Scientific agronomic validation: PARTIAL.
- Production readiness: NOT CLAIMED.

Phase 16 documentation:

- `docs/PHASE16_FINAL_VERIFICATION_REPORT.md`
- `docs/PHASE16_REQUIREMENTS_TRACEABILITY.md`
- `docs/PHASE16_BROWSER_E2E_RESULTS.md`
- `docs/PHASE16_SECURITY_ACCESSIBILITY.md`
- `docs/PHASE16_PERFORMANCE_RELIABILITY.md`
- `docs/PHASE16_UAT_TEST_PLAN.md`
- `docs/PHASE16_ETHICS_PARTICIPANT_PACK.md`
- `docs/PHASE16_DEPLOYMENT_READINESS.md`
- `docs/PHASE16_DISSERTATION_EVIDENCE.md`

Phase 16 verification commands:

- `dotnet build .\src\backend\AnuradhapuraAI.Api\AnuradhapuraAI.Api.csproj --no-restore` -> PASS, 0 warnings, 0 errors.
- `dotnet test .\tests\AnuradhapuraAI.Phase3Tests\AnuradhapuraAI.Phase3Tests.csproj --no-restore --logger "console;verbosity=minimal"` -> 281 passed, 0 failed, 0 skipped.
- `python -m pytest` from `src/forecasting-service` -> 22 passed, 0 failed, 1 upstream deprecation warning.
- `npm test -- --watch=false` from `src/frontend` -> 39 passed, 0 failed.
- `npm run build` from `src/frontend` -> PASS.
- `npm run e2e` from `src/frontend` -> 14 passed, 0 failed, real Chrome browser executed.
- `npm audit --audit-level=high` from `src/frontend` -> 0 vulnerabilities.

## 9D. Phase 17 Dissertation Evidence Status

Phase 17 completed dissertation evidence consolidation and supervisor review preparation.

Implemented:

- Consolidated evidence register.
- Dissertation consistency audit.
- Draft Chapter 5 Development material.
- Draft Chapter 6 Testing material.
- Draft Chapter 7 Evaluation material.
- Draft Chapter 8 Conclusion material.
- Results tables with verified model-improvement calculations.
- Figure and screenshot register.
- References audit.
- Supervisor review brief and likely questions/answers.
- Outstanding research actions register.
- Phase 17 final verification report.

Phase 17 documentation:

- `docs/PHASE17_FINAL_VERIFICATION_REPORT.md`
- `docs/dissertation/phase17/EVIDENCE_REGISTER.md`
- `docs/dissertation/phase17/CONSISTENCY_AUDIT.md`
- `docs/dissertation/phase17/CHAPTER5_DEVELOPMENT_DRAFT.md`
- `docs/dissertation/phase17/CHAPTER6_TESTING_DRAFT.md`
- `docs/dissertation/phase17/CHAPTER7_EVALUATION_DRAFT.md`
- `docs/dissertation/phase17/CHAPTER8_CONCLUSION_DRAFT.md`
- `docs/dissertation/phase17/RESULTS_TABLES.md`
- `docs/dissertation/phase17/FIGURE_REGISTER.md`
- `docs/dissertation/phase17/REFERENCES_AUDIT.md`
- `docs/dissertation/phase17/SUPERVISOR_REVIEW_BRIEF.md`
- `docs/dissertation/phase17/SUPERVISOR_QUESTIONS_AND_ANSWERS.md`
- `docs/dissertation/phase17/OUTSTANDING_RESEARCH_ACTIONS.md`

Phase 17 verification commands:

- `dotnet build .\src\backend\AnuradhapuraAI.Api\AnuradhapuraAI.Api.csproj --no-restore` -> PASS, 0 warnings, 0 errors.
- `dotnet test .\tests\AnuradhapuraAI.Phase3Tests\AnuradhapuraAI.Phase3Tests.csproj --no-restore --logger "console;verbosity=minimal"` -> PASS, 281 passed, 0 failed, 0 skipped.
- `python -m pytest` from `src/forecasting-service` -> PASS, 22 passed, 0 failed, 1 upstream Starlette/httpx deprecation warning.
- `npm test -- --watch=false` from `src/frontend` -> PASS, 9 files, 39 tests passed.
- `npm run build` from `src/frontend` -> PASS.
- `npm run e2e` from `src/frontend` -> PASS, 14 passed, 0 failed, real Chrome browser executed.

Phase 17 status:

- Dissertation approval: PENDING.
- Real participant UAT: NOT PERFORMED.
- Agricultural Officer expert validation: NOT PERFORMED.
- Ethics approval: PENDING.
- Scientific agronomic validation: PARTIAL.
- Production readiness: NOT CLAIMED.

## 9E. Phase 18 Dissertation Integration Status

Phase 18 completed authorized dissertation integration, academic quality audit and supervisor submission preparation using available repository sources.

Implemented:

- Source inventory.
- Missing-source report for unavailable approved Chapters 1-4, template, final diagrams and supervisor feedback.
- Integration manifest.
- Dissertation structure audit.
- Research alignment matrix.
- Refined Chapter 5-8 drafts.
- Abstract and front matter draft with placeholders.
- References audit.
- Figures and tables audit.
- Appendices register.
- Academic quality audit.
- Supervisor submission package and email draft.
- Submission readiness checklist.
- Phase 18 final verification report.

Phase 18 documentation:

- `docs/PHASE18_FINAL_VERIFICATION_REPORT.md`
- `docs/dissertation/phase18/SOURCE_INVENTORY.md`
- `docs/dissertation/phase18/MISSING_SOURCE_REPORT.md`
- `docs/dissertation/phase18/INTEGRATION_MANIFEST.md`
- `docs/dissertation/phase18/DISSERTATION_STRUCTURE_AUDIT.md`
- `docs/dissertation/phase18/RESEARCH_ALIGNMENT_MATRIX.md`
- `docs/dissertation/phase18/CHAPTER5_REFINED.md`
- `docs/dissertation/phase18/CHAPTER6_REFINED.md`
- `docs/dissertation/phase18/CHAPTER7_REFINED.md`
- `docs/dissertation/phase18/CHAPTER8_REFINED.md`
- `docs/dissertation/phase18/ABSTRACT_AND_FRONT_MATTER.md`
- `docs/dissertation/phase18/REFERENCES_AUDIT.md`
- `docs/dissertation/phase18/FIGURES_AND_TABLES_AUDIT.md`
- `docs/dissertation/phase18/APPENDICES_REGISTER.md`
- `docs/dissertation/phase18/ACADEMIC_QUALITY_AUDIT.md`
- `docs/dissertation/phase18/SUPERVISOR_SUBMISSION_PACKAGE.md`
- `docs/dissertation/phase18/SUPERVISOR_EMAIL_DRAFT.md`
- `docs/dissertation/phase18/SUBMISSION_READINESS_CHECKLIST.md`

Phase 18 verification commands:

- `dotnet build .\src\backend\AnuradhapuraAI.Api\AnuradhapuraAI.Api.csproj --no-restore` -> PASS, 0 warnings, 0 errors.
- `dotnet test .\tests\AnuradhapuraAI.Phase3Tests\AnuradhapuraAI.Phase3Tests.csproj --no-restore --logger "console;verbosity=minimal"` -> PASS, 281 passed, 0 failed, 0 skipped.
- `python -m pytest` from `src/forecasting-service` -> PASS, 22 passed, 0 failed, 1 upstream Starlette/httpx deprecation warning.
- `npm test -- --watch=false` from `src/frontend` -> PASS, 9 files, 39 tests passed.
- `npm run build` from `src/frontend` -> PASS.
- `npm run e2e` from `src/frontend` -> PASS, 14 passed, 0 failed, real Chrome browser executed.

Phase 18 status:

- Full dissertation assembly: INCOMPLETE.
- Approved Chapter 1-4 sources: MISSING.
- Supervisor approval: PENDING.
- University submission readiness: NOT READY.
- Real participant UAT: NOT PERFORMED.
- Agricultural Officer expert validation: NOT PERFORMED.
- Ethics approval: PENDING.
- Scientific agronomic validation: PARTIAL.
- Production readiness: NOT CLAIMED.

## 9F. Phase 19 Integrated Dissertation Review Draft Status

Phase 19 completed full dissertation integration and review-package preparation after the owner supplied the original Chapters 1-4 Word document.

Primary source:

- `docs/dissertation/phase19/input/Chapters_1_to_4_Piyumi_Paramee_Darshika.docx`
- SHA256 before and after processing: `FD463F65C9FF713D9A24974F16E795C49ECC8B094BD90265CBC82603BB76B6F2`
- Original preservation: PASS.

Local-only DOCX outputs:

- `docs/dissertation/phase19/FULL_DISSERTATION_INTEGRATED_DRAFT.docx`
- `docs/dissertation/phase19/CHAPTERS_1_TO_4_CORRECTED_DRAFT.docx`
- `docs/dissertation/phase19/CHAPTERS_5_TO_8_INTEGRATED_DRAFT.docx`

These DOCX files are intentionally ignored and not pushed because they contain authored dissertation content and may include personal academic details.

Tracked Phase 19 documentation:

- `docs/PHASE19_FINAL_VERIFICATION_REPORT.md`
- `docs/dissertation/phase19/SOURCE_DOCUMENT_INVENTORY.md`
- `docs/dissertation/phase19/CHAPTERS_1_TO_4_CHANGE_LOG.md`
- `docs/dissertation/phase19/CROSS_CHAPTER_CONSISTENCY_AUDIT.md`
- `docs/dissertation/phase19/RESEARCH_TRACEABILITY_MATRIX.md`
- `docs/dissertation/phase19/EVIDENCE_REGISTER.md`
- `docs/dissertation/phase19/REFERENCES_AUDIT.md`
- `docs/dissertation/phase19/FIGURE_AND_TABLE_REGISTER.md`
- `docs/dissertation/phase19/FORMATTING_AND_DOCX_QA.md`
- `docs/dissertation/phase19/SCIENTIFIC_AND_ETHICS_LIMITATIONS.md`
- `docs/dissertation/phase19/SUPERVISOR_REVIEW_SUMMARY.md`
- `docs/dissertation/phase19/SUPERVISOR_QUESTIONS_AND_ANSWERS.md`
- `docs/dissertation/phase19/OUTSTANDING_ACTIONS_AND_SUBMISSION_CHECKLIST.md`
- `docs/dissertation/phase19/README.md`

Phase 19 status:

- Integrated Chapters 1-8 DOCX review draft: GENERATED LOCALLY.
- Source Chapters 1-4 preservation: PASS.
- Generated DOCX parser validation: PASS.
- Review PDF: NOT GENERATED; no reliable renderer confirmed.
- Substantive consistency issues found: 8.
- Substantive consistency issues resolved in review package: 7.
- Open supervisor item: final diagram/asset handling and whether inline tracked changes are required.
- Real participant UAT: NOT PERFORMED.
- Agricultural Officer expert validation: NOT PERFORMED.
- Ethics approval: PENDING.
- Scientific agronomic validation: PARTIAL.
- Supervisor approval: PENDING.
- University submission readiness: NOT READY until supervisor/template/reference/ethics decisions are completed.

## 10. Authentication / RBAC Implementation

Implemented authentication foundation:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/auth/me`
- JWT Bearer authentication

Authorization policies:

- `AuthenticatedUser`
- `AgriculturalOfficerOnly`
- `AdministratorOnly`

Logout is currently stateless and handled by client-side token removal. Refresh-token and server-side token revocation functionality are not part of the current approved MSc prototype scope.

## 11. Admin Foundation

Existing admin route areas:

- `/api/admin/crops`
- `/api/admin/crop-requirements`
- `/api/admin/soil-compatibility`
- `/api/admin/suitability-config`
- `/api/admin/users`
- `/api/admin/recommendations`
- `/api/admin/validations`

Recommendation and validation access in the admin foundation is read-only where designed.

## 12. Historical Weather Dataset

Actual training dataset:

- `docs/anuradhapura_weather_2010_2025_era5.csv`

Source:

- ERA5 historical/reanalysis weather data accessed through Open-Meteo

Period:

- 2010-01-01 to 2025-12-31

Total daily records:

- 5,844

Variables:

- Temperature
- Rainfall
- Humidity

Quality results:

- Missing values: 0
- Duplicate dates: 0
- Complete daily continuity

Important limitation: ERA5 is reanalysis/modelled historical weather data and is not direct Sri Lanka Department of Meteorology station observation data. The selected dataset represents a representative Anuradhapura location and must not be described as exact microclimate coverage of every location in the entire district.

## 13. ML Training Configuration

- Lookback: 30 days
- Forecast horizon: 7 days
- Input shape: `(samples, 30, 3)`
- Output shape: `(samples, 7, 3)`

Chronological periods:

- Training: 2010-2021
- Validation: 2022-2023
- Test: 2024-2025

Scaler:

- `MinMaxScaler`
- Scaler fitted only on training data.

Model architecture:

```text
Input(30,3)
->
LSTM(64)
->
Dropout(0.2)
->
Dense(21)
->
Reshape(7,3)
```

Hyperparameters:

- Batch size: 32
- Maximum epochs: 100
- Learning rate: 0.001
- Early stopping patience: 10
- Seed: 42
- Loss: MSE
- Optimizer: Adam

Actual training:

- 76 epochs
- Best validation loss epoch: 66
- Best weights restored

## 14. Model Evaluation Results

Persistence baseline overall:

- MAE = 3.584
- RMSE = 8.738

LSTM overall:

- MAE = 3.163
- RMSE = 6.729

Per-variable LSTM:

- Rainfall: MAE = 4.904, RMSE = 10.547
- Temperature: MAE = 0.707, RMSE = 0.891
- Humidity: MAE = 3.879, RMSE = 4.881

Notes:

- LSTM outperformed the persistence baseline.
- Rainfall remains the most difficult target.
- Forecast error generally increases across the seven-day horizon.
- Overall MAE/RMSE combines variables with different units, so per-variable metrics should receive greater emphasis in dissertation evaluation.

## 15. Model Artifacts

Current artifact location:

- `src/forecasting-service/artifacts/v1_era5_2010_2025/`

Expected important files include:

- `v1.keras`
- `v1_scaler.joblib`
- `v1_metadata.json`
- `v1_evaluation.json`
- `v1_training_history.json`
- `v1_sample_forecast.json`

Do not move or regenerate these files during handover. These generated model and preprocessing artifacts are ignored by Git through `.gitignore`.

## 16. Current Testing Status

Verified testing state:

- Backend build: `dotnet build .\src\backend\AnuradhapuraAI.Api\AnuradhapuraAI.Api.csproj --no-restore` passed on 2026-10-13 with 0 warnings and 0 errors.
- Backend tests: `dotnet test .\tests\AnuradhapuraAI.Phase3Tests\AnuradhapuraAI.Phase3Tests.csproj --no-restore --logger "console;verbosity=minimal"` passed 268 tests on 2026-10-13 with 0 failed and 0 skipped.
- Frontend tests: `npm test -- --watch=false` from `src/frontend` passed 9 test files / 38 tests on 2026-10-13.
- Frontend production build: `npm run build` from `src/frontend` succeeded on 2026-10-13.
- Python tests: `python -m pytest` from `src/forecasting-service` passed 22 tests on 2026-10-13 with one Starlette/httpx deprecation warning from installed test dependencies.

`npm test -- --run` is not a supported Angular CLI argument in this project and fails before test execution; use `npm test`.

## 16A. Phase 13 ForecastRun Operational Integration Status

Phase 13 technical development and controlled live integration are complete for the MSc prototype.

Final evidence:

- `docs/PHASE13_FINAL_VERIFICATION_REPORT.md`
- `C:\Users\User\AppData\Local\Temp\anuradhapura_phase13_DE_verification.json`

Implemented backend behavior:

- `POST /api/forecasts` remains the manual forecast endpoint and persists generated runs as `LegacyUnknown`.
- `GET /api/forecasts/latest` now selects only eligible `Operational` `ForecastRun` parents and never combines child rows across runs.
- Operational eligibility requires one parent run, exactly seven child `ForecastRecord` rows, seven distinct consecutive target dates, consistent run id, forecast origin date, model version, target range, provider identity, the configured fresh-source product, Asia/Colombo timezone, observation window, source retrieval timestamp, accepted data quality status, and finite plausible weather values.
- `HistoricalEvaluation` and `LegacyUnknown` runs are excluded from operational forecast display and recommendation evaluation.
- Recommendation orchestration now consumes only eligible operational forecast runs.
- `IProviderForecastOrchestrationService` supports the existing historical preparation path and a new operational generation path.
- Operational generation resolves the latest 30-day local observation window ending yesterday in Asia/Colombo, calls the configured `IWeatherObservationSource`, validates exactly 30 daily observations, calls the existing Python forecasting client, validates exactly seven forecast outputs, and persists one `Operational` `ForecastRun` plus seven child `ForecastRecord` rows atomically through EF Core save semantics.
- `OpenMeteoWeatherSource` remains the approved Open-Meteo Historical Weather API adapter for ERA5 using the representative Anuradhapura coordinate `8.312229, 80.413055`, timezone `Asia/Colombo`, and daily variables `precipitation_sum`, `temperature_2m_mean`, and `relative_humidity_2m_mean`.
- Option B adds `OpenMeteoForecastPastDaysWeatherSource` as the candidate recent operational input adapter. It uses Open-Meteo Forecast API `/v1/forecast` with `past_days=30`, `forecast_days=0`, timezone `Asia/Colombo`, and hourly variables `temperature_2m`, `relative_humidity_2m`, and `precipitation`.
- The operational Open-Meteo typed `HttpClient` disables automatic redirects. HTTP 3xx responses fail closed.
- The fresh-source adapter aggregates hourly values into local daily observations only when every day has exactly 24 hourly records. Daily rainfall is summed; temperature and relative humidity are averaged. Missing, duplicate, out-of-window, current/future, non-finite, negative-rainfall, or invalid-humidity records fail closed.
- The operational source is explicitly selected with `FeatureFlags:OperationalWeatherSource = "OpenMeteoForecastPastDays"` and separately accepted with `FeatureFlags:AcceptOperationalWeatherSource = true`. Both remain unset/false in default appsettings, and `EnableWeatherModelIntegration` remains false by default.
- The existing LSTM artifacts remain ERA5-trained (`v1_era5_2010_2025`). Forecast API past-days data is not labelled ERA5, and operational use remains pending real paired-data compatibility evaluation and owner approval because distribution shift has not been scientifically validated.
- Provider response metadata now carries requested coordinates and optional returned grid coordinates in the application DTO. The currently applied database schema stores requested coordinates but has no returned-grid columns, so returned grid coordinates are surfaced in the orchestration response only.
- `PythonForecastingClient` validates malformed JSON, forecast count, duplicate/nonconsecutive target dates, model-version consistency, and invalid weather values.
- `POST /api/admin/forecasts/operational` is Administrator-only and feature-gated by `EnableWeatherModelIntegration`.
- When `EnableWeatherModelIntegration` is false, the admin trigger returns disabled/unavailable without provider calls, Python inference, persistence, or run creation.

Implemented Angular behavior:

- The public dashboard continues to use `GET /api/forecasts/latest`; because backend selection is now operational-only, it cannot display historical or manual LegacyUnknown runs as current operational weather.
- The Administrator screen includes a manual operational forecast trigger with loading state, success run summary, failure messages, and duplicate-click prevention while a request is pending.
- Angular only calls backend APIs; it does not expose secrets or perform provider/Python calls directly.

Applied database baseline:

- Migration `20261012060858_AddForecastRunProvenance` was applied before this handover update.
- Database evidence confirmed `dbo.ForecastRun`, primary key, check constraints, `ForecastRecord -> ForecastRun` foreign key, and migration-history entry.
- The final migration-runner JSON had a reporting assertion inconsistency, but the database evidence confirmed the migration execution and verification. Do not rerun this migration without a new owner request.

Final D/E live verification:

- Development database: `AnuradhapuraAI`
- Backup/recovery: existing full backup verified with `RESTORE VERIFYONLY`
- Administrator: one active Administrator existed; no duplicate Administrator was created during final D/E
- Admin trigger: one authenticated `POST /api/admin/forecasts/operational`
- ForecastRunId: `b2d45e38-72fd-4b3d-87b5-418891a485c2`
- Open-Meteo GET count: 1
- Python inference count: 1
- SQL delta: `ForecastRun +1`, `ForecastRecord +7`, `Recommendation +0`
- `GET /api/forecasts/latest`: PASS and returned the new Operational run
- Recommendation integration: PASS by existing non-persisting orchestration tests and live read-only checks; the live DB has no configured crop requirements/soil/threshold rows, so live recommendations would be insufficient-evidence until administrators configure agronomic data
- Angular verification: PASS at component/service test level; browser E2E was not executed
- Temporary process-local runtime overrides were removed after execution

No User Secrets, production configuration, deployment settings, or database migrations were changed during final D/E.

Remaining scientific / policy items:

- Open-Meteo API data attribution is required and commercial/high-volume use terms must be reviewed before production use; do not claim production clearance without owner/legal review.
- ERA5 is gridded historical/reanalysis data for a representative point and must not be described as measured district-average observations.
- Forecast API past-days gridded model data must not be described as measured district-average observations.
- The project still lacks full paired-data statistical compatibility evidence between the ERA5-trained LSTM inputs and the Forecast API past-days source.
- Predictive accuracy of the operational forecasts is not verified.
- Persisting returned grid coordinates would require a future explicit schema decision and migration; no such migration was created in Phase 13.

Git closure note:

- `src/backend/AnuradhapuraAI.Api/AnuradhapuraAI.Api.csproj` is protected by owner instruction and must not be staged, committed, reverted, reset, normalized, or pushed as part of Phase 13 closure.

## 17. Features Explicitly Outside Current Scope

The following are outside the approved MSc scope unless explicitly approved later:

- nationwide coverage
- IoT soil sensors
- automated irrigation
- pest/disease prediction
- market-price forecasting
- native mobile application
- full multilingual UI
- nationwide alerts
- yield prediction
- autonomous farming decisions
- NPK-based recommendation
- organic-matter-based recommendation
- land-size recommendation
- planting-time recommendation
- season-specific weighting
- high-humidity climate-risk indicator

## 18. Feature Flags

Approved lightweight feature flags:

- `EnableOfficerValidation`
- `EnableRecommendationHistory`
- `EnableClimateRiskIndicators`
- `EnableWeatherModelIntegration`

These are lightweight/config-driven placeholders and not an enterprise feature-flag platform.

## 19. Important Unresolved / Future Decisions

The following must not be invented by a future developer or Codex session:

- final agricultural numeric thresholds/ranges where not yet approved
- final soil data/source/selection method
- specific production hosting provider
- any additional features outside approved scope
- any architecture/schema change affecting frozen phases

These require explicit project-owner approval.

## 20. Rules for Future Codex Sessions

1. Read this handover document before modifying the project.
2. Read `AGENTS.md` if present.
3. Read `.agents/skills/anuradhapura-ai-development/`.
4. Treat APPROVED/FROZEN decisions as immutable unless the project owner explicitly approves a change.
5. Do not silently expand project scope.
6. Do not invent agricultural values.
7. Do not fabricate model performance.
8. Do not retrain or replace the approved model without approval.
9. Do not modify completed database/auth/admin foundations unnecessarily.
10. Work phase-by-phase.
11. Stop after each phase and provide a completion report.
12. Wait for project-owner approval before starting the next phase.

## 21. Next Development Task

An explicitly requested seasonal-planning extension added a public `/seasonal-planning` workflow and read-only seasonal climate endpoints. It computes calendar-month statistics from the local ERA5 2010–2025 CSV and does not treat historical averages as forecasts. Crop-specific seasonal suitability remains insufficient-evidence until verified compatible requirements or locally applicable calendars are configured; this extension requires supervisor review.

## 22. Current Project State — 18 October 2026

The project owner subsequently accepted Seasonal Planning as approved project scope. It must remain clearly described as historical climate and crop-calendar guidance, not as a long-range LSTM forecast or a full agronomic recommendation.

### Crop-suitability scientific status

- The six-crop evidence catalogue is at `docs/agriculture/crop-evidence.json`.
- The supporting QA review is at `docs/qa/QA001_SCIENTIFIC_VALIDATION_REPORT.md` and `docs/qa/PHASE2_EVIDENCE_FEASIBILITY_REVIEW.md`.
- The evidence supports only a seven-day mean temperature compatibility indicator. Annual or seasonal rainfall figures must not be divided or converted into weekly thresholds.
- There are no validated crop-specific seven-day rainfall, relative-humidity, soil, weighting, or category-threshold values for all six crops.
- `QA-001` remains **PARTIAL**. It must not become PASS until an authorized agricultural expert approves a traceable six-crop matrix for the current forecast horizon and factors.
- Partial assessments preserve available factor-level evidence, but must not show an overall suitability score, suitability category, or crop rank. This safeguard is implemented in `RecommendationOrchestrationService` and enforced before recommendation persistence.

### Current verification

- Focused suitability/recommendation regression tests: **PASS — 38/38**.
- Full backend test suite: **288/289 passed**.
- Known test issue: `OperationalForecastTrigger_AdministratorReceivesFeatureDisabledResponse_WhenIntegrationFlagOff` expects HTTP 503 but currently receives HTTP 400. No feature flags were changed while recording this result.
- Previously verified ML and frontend results remain: Python tests 22/22; Angular tests 39/39. Do not repeat LSTM training or evaluation without a genuine defect.

### Git and safety status

- `src/backend/AnuradhapuraAI.Api/AnuradhapuraAI.Api.csproj` remains protected and must not be staged, committed, reset, or modified.
- `qa-evidence/` contains retained QA evidence. `qa-runtime/` is generated runtime material and is intentionally not committed unless the owner explicitly requests it.
- Do not change production settings, database records, or working feature flags without explicit owner approval.

The next substantive completion item is the agricultural expert-approved suitability matrix. Independent technical work may continue on remaining role QA, accessibility/performance evidence, and final acceptance documentation.

## Startup Prompt for a New Codex Session

Read the following before making any changes:

1. `AGENTS.md`, if present
2. `.agents/skills/anuradhapura-ai-development/`
3. `docs/CODEX_HANDOVER.md`

This is an MSc project with approved/frozen development decisions.

Confirm that you understand:

- the project scope
- the six supported crops
- the architecture
- the completed phases
- the trained LSTM model
- the distinction between weather forecasting and crop recommendation
- the current project status
- the next planned phase

Do not modify anything yet. First provide a short project-state summary and wait for approval.
