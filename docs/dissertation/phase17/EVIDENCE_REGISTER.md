# Phase 17 Evidence Register

Generated: 2026-10-13

Purpose: consolidate dissertation evidence for supervisor review. This register records what the repository supports and where claims remain limited.

## Evidence Summary

| Claim ID | Claim | Implementation / source | Test or evidence | Evidence quality | Status | Limitation |
| --- | --- | --- | --- | --- | --- | --- |
| EV-01 | Scope is Anuradhapura District only | `.agents/.../project-specification.md`, `docs/CODEX_HANDOVER.md` | Documentation audit | Approved scope | PASS | Representative-point weather data does not prove every district microclimate |
| EV-02 | Exactly six crops are supported | `ApprovedCropNames.cs`, recommendation tests, E2E fixtures | Backend tests; Phase 16 E2E | Automated | PASS | No new crops approved |
| EV-03 | LSTM predicts rainfall, temperature and humidity | `forecasting_service/`, artifacts metadata | `v1_metadata.json`, Python tests | Artifact-backed | PASS | It does not predict crop suitability or yield |
| EV-04 | Historical dataset is ERA5 2010-2025 | `docs/anuradhapura_weather_2010_2025_era5.csv` | Metadata row count 5,844 | Artifact-backed | PASS | Reanalysis/gridded data, not station observations |
| EV-05 | Chronological ML split is used | `v1_metadata.json`, preprocessing tests | Python tests | Automated and artifact-backed | PASS | Split dates are fixed in artifact metadata |
| EV-06 | LSTM improves over persistence baseline on ERA5 test set | `v1_evaluation.json` | MAE/RMSE comparison | Artifact-backed | PASS | Historical ERA5 only, not live operational accuracy |
| EV-07 | Backend integrates Python inference through a service boundary | `PythonForecastingClient.cs`, Phase 6/13 tests | Backend tests | Automated | PASS | Live inference not rerun in Phase 17 |
| EV-08 | Operational input source uses Open-Meteo Forecast API past-days data | `OpenMeteoForecastPastDaysWeatherSource.cs` | Phase 13 report and tests | Automated plus controlled live evidence | PASS technical | Source distribution shift remains scientifically unverified |
| EV-09 | Operational ForecastRun persistence works | `ForecastRun`, `ForecastRecord` entities and migrations | Phase 13 live verification | Controlled live evidence | PASS technical | No new Phase 17 database write |
| EV-10 | Latest forecast API exposes eligible operational seven-day forecasts | `ForecastService.cs`, `ForecastsController.cs` | Forecast endpoint tests | Automated | PASS | Requires existing eligible operational run |
| EV-11 | Recommendation evaluation is read-only | `RecommendationsController.cs` | Recommendation endpoint tests; Phase 14/16 reports | Automated | PASS | History endpoint is separate and feature-gated |
| EV-12 | Suitability scoring is explainable and weighted | `CropSuitabilityEngine.cs`, `RangeSuitabilityScorer.cs` | Suitability tests | Automated | PASS technical | Live agronomic thresholds not fully validated |
| EV-13 | Missing optional soil uses valid-factor weight renormalization | suitability engine and tests | Backend suitability tests | Automated | PASS | Soil source/selection remains unresolved |
| EV-14 | Recommendation ranking returns all six crops when evidence allows | orchestration and ranking services | Recommendation tests | Automated | PASS technical | Insufficient-evidence crops may be unranked |
| EV-15 | Recommendation history is fail-closed unless enabled | persistence/history services | Recommendation history tests | Automated | PASS | Requires approved runtime flag to write history |
| EV-16 | Role-based authorization is implemented | JWT auth, policies, role guard | Backend auth tests; E2E route checks | Automated | PASS | Refresh-token revocation is outside approved prototype scope |
| EV-17 | Agricultural Officer validation is review-only | validation service and officer UI | Phase 9 tests; Phase 16 E2E | Automated | PASS technical | Real expert validation not performed |
| EV-18 | Administrator configuration screens exist | admin service and Angular admin component | Admin tests and E2E | Automated | PASS technical | Production configuration values still need owner approval |
| EV-19 | Browser E2E covers core workflows | `src/frontend/e2e/phase16.spec.ts` | `npm run e2e`: 14 passed | Real browser with mocked APIs | PASS | Mocked browser tests, not live production E2E |
| EV-20 | Security boundaries are tested | auth, admin, validation, history tests | Backend and E2E tests | Automated | PASS technical | No penetration test performed |
| EV-21 | Accessibility was checked at prototype level | focus-visible CSS, role-based E2E selectors | Phase 16 report | Partial | PARTIAL | No WCAG certification or assistive-technology study |
| EV-22 | Deployment readiness was assessed | Phase 16 deployment checklist | Documentation audit | Checklist | PARTIAL | No public deployment or production hardening claim |

## Evidence Quality Legend

- Approved scope: direct project specification or owner-approved handover.
- Artifact-backed: generated model or dataset artifact exists in the repository workspace.
- Automated: verified by repeatable tests.
- Controlled live evidence: one documented controlled development execution.
- Partial: useful evidence exists but does not support a complete scientific or production claim.
