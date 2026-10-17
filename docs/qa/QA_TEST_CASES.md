# QA Test Cases — Actual Local Verification

Updated: 2026-10-17. Browser cases used visible headed Google Chrome against the local Angular and ASP.NET Core services. `PASS (technical)` means the observed application behavior met the stated technical expectation; it does not claim scientific agronomic validation.

| Test ID | Workflow | Role | Scenario / observed result | Status | Evidence |
|---|---|---|---|---|---|
| QA-001 | WF-08 / crop suitability | Public | The live recommendation view exposed only source-linked seven-day temperature compatibility indicators. Rainfall, humidity and optional soil factors remained unavailable; no overall suitability score, category or comparable crop rank was shown. | PARTIAL | `qa-evidence/PUB-04-live-recommendation.png`; `visible-browser-observations.json` |
| QA-002 | startup | Public | Angular root returned 200. | PASS | visible audit output |
| QA-003 | WF-01 | Public | Dashboard rendered the latest eligible persisted operational forecast. | PASS (technical) | `qa-evidence/PUB-01-home-desktop.png` |
| QA-004 | WF-08 display behaviour | Public | Read-only evaluation correctly presented source-linked temperature indicators, unavailable rainfall/humidity/soil factors, and no fabricated category or comparable rank. | PASS (technical) | `qa-evidence/PUB-04-live-recommendation.png`; `visible-browser-observations.json` |
| QA-005–010 | WF-02 extension | Public | 7-day, 30-day, 3-month, 6-month, 12-month and custom future ranges each returned six source-backed, conditional calendar/climate results. | PASS (technical) | `qa-evidence/SP-01`–`SP-05b` PNGs |
| QA-011 | WF-10 | Public | Past date rejected with meaningful feedback. | PASS | `qa-evidence/SP-05c-past.png` |
| QA-012 | WF-10 | Public | Reversed range rejected. | PASS | `qa-evidence/SP-06-reversed.png` |
| QA-013 | WF-10 | Public | Range longer than 366 days rejected. | PASS | `qa-evidence/SP-07-over-366.png` |
| QA-014 | WF-10 | Public | Empty required dates blocked by browser validation. | PASS | `qa-evidence/SP-08-empty.png` |
| QA-015–020 | WF-03 extension | Public | Paddy, Maize, Green Gram, Cowpea, Groundnut and Chilli each returned conditional Maha/Yala calendar-supported periods with source/condition limits. | PASS (technical) | `qa-evidence/CP-*.png` |
| QA-021 | public | Public | Seasonal planning was accessible without login. | PASS | `qa-evidence/PUB-03-seasonal-desktop.png` |
| QA-022 | RBAC | Anonymous | `/admin` redirected to login. | PASS | `qa-evidence/SEC-01-admin-anonymous.png` |
| QA-023 | RBAC | Anonymous | `/officer` redirected to login. | PASS | `qa-evidence/SEC-02-officer-anonymous.png` |
| QA-024 | responsive | Public | Desktop 1440×900 rendered usable forms and results. | PASS | `qa-evidence/PUB-03-seasonal-desktop.png` |
| QA-025 | responsive | Public | Tablet 768×1024 rendered. | PASS | `qa-evidence/RSP-01-tablet.png` |
| QA-026 | responsive | Public | Mobile 390×844 rendered. | PASS | `qa-evidence/RSP-02-mobile.png` |
| QA-027 | WF-04 | Registered User | A normal QA user registered and logged in through the live API. `/api/auth/me` returned `Registered User`; administrator and officer routes returned `403`; history returned the expected `503` while its approved feature flag remains disabled. | PASS (technical) | 2026-10-17 live registration/login/API audit |
| QA-028 | WF-05 | Agricultural Officer | One active Agricultural Officer account exists, but no authorized credential was supplied for a live validation-session test. | BLOCKED | current database role audit |
| QA-029 | WF-06 | Administrator | Existing authorized Administrator logged in, reached the administration screen, and loaded the authorized user-management contract. | PASS (technical) | `qa-evidence/AUTH-01-admin-operational-forecast.png`; `authenticated-operational-workflow.json` |
| QA-030 | WF-07 / WF-08 | Public | Existing persisted operational forecast carried model `v1`, seven daily values, and was consumed by the visible recommendation workflow. | PASS (technical) | `qa-evidence/visible-browser-observations.json` |
| QA-031 | WF-07 | System | Existing Python LSTM service loaded model `v1` and returned seven dated forecasts from an existing 30-day ERA5 input; no retraining or SQL write occurred. | PASS (technical) | 2026-10-17 local inference command output |
| QA-032 | WF-07 | Administrator | Fresh Open-Meteo past-days observations → LSTM `v1` → one Operational SQL ForecastRun → Angular dashboard → public recommendation evaluation completed. The run covers 2026-10-17 to 2026-10-23. | PASS (technical) | `qa-evidence/AUTH-01`–`AUTH-03` PNGs; `authenticated-operational-workflow.json` |
| QA-033 | WF-07 | System | Saved LSTM `v1` was re-evaluated without retraining against the approved 2024-01-01 to 2025-12-31 chronological test set (725 sequences). | PASS (technical) | 2026-10-17 local evaluation command output; `artifacts/v1_era5_2010_2025/v1_evaluation.json` |

Totals: 33 test cases — PASS 31, PARTIAL 1, BLOCKED 1, FAIL 0.

Notes:

- QA-001 remains `PARTIAL`: temperature-only indicators are useful and traceable but not complete scientifically validated suitability recommendations.
- Seasonal outputs are conditional calendar candidates, not exact planting prescriptions or long-range LSTM forecasts.
- Existing active accounts now include Administrator, Agricultural Officer and Registered User roles. The Registered User flow was tested through approved public registration; Agricultural Officer credentials are still required for its live workflow test.
- Final regression on 2026-10-17: backend 289/289 tests passed; forecasting service 22/22 tests passed (one upstream FastAPI/Starlette deprecation warning); Angular 39/39 tests passed.
- Expected HTTP 400 responses in negative seasonal tests appear as Chrome console resource errors; they are validated negative outcomes, not unhandled application failures.
- The local development weather-integration configuration was preserved during this verification. No production configuration, model artifact, database schema or protected API project file was changed.
