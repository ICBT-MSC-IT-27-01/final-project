# Approved MSc Scope Traceability Audit

Updated: 2026-10-17. Sources reviewed: original Chapters 1–4 draft (`docs/dissertation/phase19/input/Chapters_1_to_4_Piyumi_Paramee_Darshika.docx`), project specification, the working PRD/SRS baseline, implementation, tests, and `QA_TEST_CASES.md`.

| Requirement | Implemented | Tested | Missing work / status |
|---|---|---|---|
| Public seven-day rainfall, temperature and humidity forecast | Yes — Angular dashboard, ASP.NET Core forecast API, SQL `ForecastRun`/records, Python LSTM boundary | Yes — QA-003, QA-030–033 and backend/frontend/Python regression suites | No software gap; operational workflow already verified. |
| Six-crop explainable recommendation | Yes — approved crop scope, factor explanations, optional soil and evidence-state handling | Yes — recommendation tests and QA-004 | **PARTIAL** scientifically. QA-001 remains PARTIAL; do not emit full suitability scores, categories or ranks without valid seven-day-compatible evidence. |
| Basic climate-risk indicators: low rainfall, heavy rainfall, high temperature | Yes — approved labels and guarded evaluator exist | Yes — unit tests cover enabled/disabled and threshold-present behavior | **BLOCKED** for live use: flag remains disabled and no approved threshold configuration exists. No values were invented. |
| Public access and normal registration | Yes — public routes, registration, login, JWT/RBAC | Yes — QA-002, QA-021–027 and authorization tests | No known software gap. |
| Registered-user recommendation history | Yes — persistence, ownership-scoped list/detail API and Angular pages | Yes — backend/frontend tests; disabled-path live QA-027 | **BLOCKED** for enabled-path live acceptance because `EnableRecommendationHistory` remains disabled by approved configuration. |
| Agricultural Officer validation | Yes — Officer-only pending/review/submit endpoints, status/comment/history, Angular Officer pages | Yes — backend authorization/integration tests | **BLOCKED** for live acceptance: authorized Officer credential and a safe pending recommendation are not available. |
| Administrator crop, requirement, soil, suitability-configuration, user/role, recommendation and validation management | Yes — Administrator-only API and Angular administration page | Yes — backend admin tests and QA-029 authorized Administrator workflow | No known software gap; no additional destructive live data-change test was authorized. |
| Configurable weights and categories | Yes — database-backed admin configuration and suitability provider | Yes — admin/suitability tests | Scientifically usable values remain unavailable for a complete seven-day crop assessment; keep QA-001 PARTIAL. |
| SQL Server persistence and role data | Yes — EF Core SQL Server entities for forecasts, recommendations, validations and users | Yes — SQL/integration tests and verified backend connection | No known software gap. |
| LSTM model, baseline and operational integration | Yes — existing evaluated LSTM v1 and verified Open-Meteo → LSTM → SQL → Angular workflow | Yes — completed QA-031–033 and model evaluation artifacts | Already complete; not re-evaluated in this audit. |
| Multilingual user interface | No | No | Not implemented because Chapters 1–4 mention future language expansion only; the approved project specification explicitly excludes full multilingual UI. It is not an approved missing feature. |

## Acceptance boundary

- Feature flags and production configuration were not changed by this audit.
- `EnableOfficerValidation` remains enabled only in local development from the prior authorized Officer-QA preparation.
- Soil remains optional.
- No recommendation, crop, user, role, threshold or other database record was created or modified for this audit.
