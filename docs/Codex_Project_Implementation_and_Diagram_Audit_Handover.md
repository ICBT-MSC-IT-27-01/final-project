# MSc Project — Source-Code and Diagram Consistency Audit
## Codex Handover Report (Read-Only Audit First)

**Project:** AI-Driven Weather Forecasting and Crop Recommendation System for Anuradhapura District, Sri Lanka
**Report date:** 25 October 2026
**Purpose:** Provide Codex with a source-grounded gap register and a reproducible audit plan for comparing the current Angular, ASP.NET Core and Python codebase with the research methodology, project requirements, and historical diagrams.
**Overall preliminary verdict:** **CORE ARCHITECTURE PRESENT; END-TO-END FUNCTIONAL COMPLETENESS NOT YET VERIFIED.** Do **not** interpret source-code presence as proof of operational, scientific, or supervisor-approved functionality.

## 1. Materials and evidence precedence

### Supplied implementation archives
- `frontend.zip` — Angular UI, routes, components, services and tests.
- `backend.zip` — ASP.NET Core API, application/domain/infrastructure layers, EF Core migrations and configuration.
- `forecasting-service.zip` — Python FastAPI/LSTM source, tests, trained `v1` model/scaler, metadata and evaluation artifacts.

### Research and QA references
- Approved Chapter 3, 4 and 5 DOCX files (methodology, results and limitations).
- `COMPLETE_SYSTEM_QA_REPORT.md`, `PROPOSED_VS_ACTUAL_WORKFLOW_AUDIT.md`, `QA_WORKFLOW_COVERAGE.md`, `QA_DEFECT_REGISTER.md`, `SIX_CROP_SCIENTIFIC_EVIDENCE_MATRIX.md`.
- Historical diagrams: `1. System Context Diagram.png`, `2. Level 0 Data Flow Diagram.png`, `3. Level 1 Data Flow Diagram.png`, `4. Use Case Diagram.png`, `5. User Regiatration .png`, `6. Model Work Flow.png`, `7. System Work Flow.png`, `8. Deployment Diagram.png`, `AI Weather System Diagram Set.png`.
- Recent figures: `Figure_3_2_Data_Preprocessing_and_LSTM_Training_Workflow.png`, `Figure_3_3_Corrected_LSTM_and_Seasonal_Planning.png`, `Figure_3_4_Overall_System_Architecture_Three_Outputs_Corrected.png`.

**Evidence priority:** (1) actual source and controlled execution; (2) reproducible runtime/test logs; (3) dated QA reports; (4) approved research/design text; (5) diagram illustrations. Distinguish a *later source-code change* from an *older QA observation*. Do not silently resolve contradictory evidence.

**Audit scope completed for this report:** Static inspection of uploaded archives and the supplied historical QA Markdown reports, plus visual review of diagram images. This report does **not** claim to have completed a new live Angular–ASP.NET–Python–SQL Server integration test, agricultural expert validation, or a fresh TensorFlow inference run. Previous QA reports contain their own historical runtime observations. An earlier conversation reported Python tests passing; this handover does not treat that statement as a freshly reproduced test result.

## 2. Agreed functional scope: three distinct user outputs

| ID | Output | Technical and scientific boundary |
|---|---|---|
| OUT-1 | **AI weather forecast** | Independent multivariate LSTM: **30 historical daily observations × 3 variables** (rainfall, mean temperature, relative humidity) → **7 forecast days × 3 variables**. Seven days is the LSTM horizon only. |
| OUT-2 | **Date-range crop planning** | User chooses start and end dates (maximum 366 days in the documented design). Uses historical calendar-month climate and agricultural calendar evidence to identify **conditional crop-calendar candidates**, not long-range LSTM forecasts or scientifically validated rankings. |
| OUT-3 | **Crop-based cultivation-period planning** | User chooses one of six crops; service provides **approximate candidate establishment months/windows**, conditional on verified crop-calendar evidence. Do not assert exact sowing dates or full agronomic suitability. |

**Six approved crops:** Paddy, Maize, Green Gram, Cowpea, Groundnut, Chilli. **Geographic scope:** Anuradhapura District; do not imply validated farm-by-farm spatial predictions.

**Additional crop-suitability assessment:** An intended component separate from the three user-output categories. Overall four-factor scores/categories/rankings must only be shown if compatible, documented agronomic requirements, soil evidence and configuration support them. Insufficient evidence is an acceptable and required explicit status, not a validated recommendation.

## 3. Source-code findings (static evidence)

| Area | Relevant code/evidence | Preliminary assessment | What remains unverified |
|---|---|---|---|
| Angular UI | `frontend/src/app/app.routes.ts`; public dashboard, recommendation and seasonal-planning components | Public pages and route structure **present** | Real browser behaviour against latest backend, accessibility, all three output displays |
| Authentication and history | Angular auth components, role guard; backend authentication and history services | Code paths **present** | Authorized login/history E2E; `registered-history-blocked.component.ts` indicates UI readiness needs particular scrutiny |
| Officer/admin | Angular officer pages, `admin-readiness.component.ts`; API `ValidationsController.cs`, `AdminController.cs` | Role-gated code **present** | Actual authorized workflows, feature flags, permissions, and whether admin is a full management UI or a readiness screen |
| Backend and SQL | API controllers, services, `AnuradhapuraAiDbContext.cs`, migrations | Layered backend/persistence **present** | Real SQL connectivity, migration application, data seeding, record writes/reads |
| LSTM | `forecasting_service/models/lstm_model.py`, `training/train.py`, `artifacts/v1_era5_2010_2025/v1_metadata.json` | Model code/artifacts **present**; metadata specifies 30-day lookback, 7-day horizon, 3 variables, 64-unit LSTM | Live inference, reproducibility against source data, accuracy under new operational inputs |
| Data leakage prevention | `training/train.py`: chronological `date_based_split`, then scaler `.fit(train[features])`, then transform | Training-code ordering **matches** intended train-only scaler fitting | Whether deployed artifacts were produced with exactly this code/config and data |
| Seasonal endpoints | `SeasonalRecommendationsController.cs`: anonymous `POST /api/seasonal-recommendations/by-date-range` and `/by-crop` | Both API routes **present** | End-to-end outputs with the required datasets and local calendar JSON |
| New seasonal logic | `SeasonalClimateService.cs`, `DoaCalendarMonthCatalog.cs` | Source supports `CALENDAR_SUPPORTED_CANDIDATE` and `CLIMATE_CONTEXT_ONLY`; month-level windows, limitations and evidence labels | Actual candidate results, calendar correctness and expert validation |
| Crop suitability | `CropSuitabilityEngine.cs`, recommendation orchestration/ranking | Scoring logic **present**, with insufficient-evidence safeguards | Full six-crop scores/categories/rankings with scientifically defensible configuration |
| Officer validation | `ValidationsController.cs`: `[Authorize(Policy = "AgriculturalOfficerOnly")]`, GET review and POST validation | Review/comment/status submission **present in code** | Role-specific end-to-end behaviour; precise definition of “read-only” (officer must not edit configuration, but may submit controlled validation when enabled) |

### Key version-sensitive finding
The dated `COMPLETE_SYSTEM_QA_REPORT.md` says both seasonal endpoints returned `INSUFFICIENT_AGRONOMIC_EVIDENCE` with no candidate periods during its run. The **uploaded latest** `SeasonalClimateService.cs` contains a newer calendar-backed branch that can return `CALENDAR_SUPPORTED_CANDIDATE`. These are **not necessarily contradictory**: they may represent different versions or missing datasets in the earlier environment. **Codex must verify runtime outputs with the supplied source and datasets before declaring the earlier defect fixed.**

### Required external evidence files absent from all three ZIPs
- `docs/anuradhapura_weather_2010_2025_era5.csv` (or the configured equivalent).
- `docs/English calendar images/evidance json/DOA_SIX_CROP_CALENDAR_MONTH_EXTRACTION_REVIEW.json` (exact path as referenced by `DoaCalendarMonthCatalog.cs`).

The code explicitly reads these files. The absence from the uploaded archives is a **packaging/reproducibility blocker**, not proof that they are absent from the user's working repository. Verify their existence, provenance and content in the actual repository. Do not invent, fabricate or silently replace them.

## 4. Historic QA findings: retain until re-tested

From `COMPLETE_SYSTEM_QA_REPORT.md` and `QA_DEFECT_REGISTER.md`:

| ID | Severity | Historic finding | Current disposition |
|---|---|---|---|
| QA-001 | Critical | Public crop recommendations returned six crops, all unscored/unranked due to missing usable requirements/soil/configuration | **Open pending new live evidence**; code for scoring alone does not close it |
| QA-002 | High | Crop-to-period workflow returned no candidate periods | **Re-test against latest calendar-backed code and datasets** |
| QA-003 | High | Date-range planning was climate exploration, not validated crop recommendation | **Retain scientific disclosure** even if candidate calendar windows now appear |
| QA-004 | High | No authorized registered/officer/admin E2E credentials and records | **Blocked** until safe authorized test data available |
| QA-005 | Medium | Dependency advisory for `System.Security.Cryptography.Pkcs` 9.0.11 | **Re-scan** installed/restored dependency graph; remediate under change control |
| QA-006 | Medium | New Open-Meteo → Python → SQL operational forecast not exercised | **Blocked/pending controlled live run** |

**Historical browser evidence:** stored `GET /api/forecasts/latest` succeeded; public `POST /api/recommendations/evaluate` returned HTTP 200 with insufficient evidence; earlier seasonal requests returned HTTP 200 with no candidates; invalid ranges were rejected. A 200 response alone is **not** proof of scientifically valid recommendations.

## 5. Diagram-by-diagram reconciliation register

| Diagram | Decision | Specific change required |
|---|---|---|
| Historical System Context | **Correct** | Include three distinct outputs; show historical meteorological and agricultural calendar sources; distinguish public access and role-specific capabilities. |
| Historical Level 0 DFD | **Correct** | Keep one system process, but distinguish forecast request/result, date-range crop candidate request/result, crop-period request/result. Clarify data stores and officer conditional validation. |
| Historical Level 1 DFD | **Major correction** | Add date-range planning and crop-period planning processes; separate these from seven-day LSTM inference and suitability scoring. Keep process numbering internally consistent. |
| Historical Use Case | **Correct** | Add `Explore Crops by Date Range` and `View Candidate Cultivation Periods by Crop` for public users; avoid presenting validation as universally enabled. Check registered-history and admin UI status. |
| Registration/Login Sequence | **Verify/correct** | Match actual Angular auth → ASP.NET authentication → SQL flow. Confirm hash/token/session details from code; do not infer token storage mechanism or untested success cases. |
| AI Weather Model Workflow | **Correct** | Show data cleaning → chronological partition → train-only scaler fit → 30×3 sequences → model training → 7×3 evaluation/output → FastAPI integration. Avoid showing model inference as necessarily verified live. |
| Crop Suitability/Recommendation Workflow | **Major correction** | Add `Evidence sufficient?` decision and explicit insufficient-evidence result. Do not always flow to weighted score, category, ranking or climate-risk indicators. Show scientific time-basis compatibility checks. |
| Deployment Diagram | **Correct** | Retain Angular / ASP.NET Core / SQL Server / Python FastAPI; show external historical/operational weather inputs and backend seasonal-calendar logic separately from LSTM. Do not invent physical hosting arrangements. |
| Historical `AI Weather System Diagram Set` poster | **Do not cite as a final validated diagram** | Historical “Ready” labels are superseded by this source/QA audit; keep as a working design artifact only. |
| New Figure 3.2 (Preprocessing) | **Keep subject to exact-code check** | Its train-only scaling and chronological split agree with inspected training code. Verify diagram sequence and labels against deployed artifact metadata. |
| New Figure 3.3 (LSTM + Planning Context) | **Keep with caption discipline** | Clearly label LSTM 30-day input / 7-day output; crops and calendar planning are **outside** the neural network. Consider separating model-only architecture if needed for dissertation clarity. |
| New Figure 3.4 (Three Outputs Architecture) | **Minor correction** | Explicit API request/response arrows, three distinct outputs, separate seasonal logic, and external evidence sources. |

**Recommended DFD Level 1 process names** (numbering proposal, not a claim about existing code): `1.0 User Management`, `2.0 AI Weather Forecasting`, `3.0 Crop Suitability Assessment`, `4.0 Date-Range Crop Planning`, `5.0 Crop-Based Cultivation Period Planning`, `6.0 Recommendation Review/Validation (Conditional)`, `7.0 System Administration`.

## 6. Codex execution plan (do not modify code in the initial audit)

### Phase A — Repository and environment inventory
1. Confirm repository root, Git status/branch, archives' relation to the working tree, .NET/Node/Python versions and safe test commands.
2. Inventory Angular routes, backend controllers/endpoints, EF entities/migrations, feature flags, Python model and artifacts.
3. Verify presence of required ERA5 CSV and calendar JSON, their checksums, schema and provenance. **Do not generate substitute scientific data.**
4. Identify sensitive configuration files. **Do not echo credentials, tokens or connection strings** into reports or commits; use redacted summaries. Do not commit archive build outputs, virtual environments or `node_modules`.

### Phase B — Three-output traceability
5. For OUT-1/2/3, trace UI → HTTP route → service → data source → response DTO → UI rendering. Record exact file paths and line numbers.
6. Record output states (`forecast generated/stored/displayed`; `CALENDAR_SUPPORTED_CANDIDATE`; `CLIMATE_CONTEXT_ONLY`; `INSUFFICIENT_AGRONOMIC_EVIDENCE`; errors), their preconditions and user-facing labels.
7. Confirm 366-day range handling and six-crop restrictions. Confirm historical climatology is **not** passed off as long-range LSTM output.

### Phase C — Model and scientific integrity
8. Verify 30×3 input, 7×3 output, LSTM 64, Dropout 0.2, Dense 21, Reshape (7,3) in model source and saved metadata; verify training/validation/test chronology and scaler fit only on train data.
9. Read stored evaluation JSON and identify whether results are test-set or validation-set metrics. Do not invent/upgrade provisional metrics into final measurements.
10. Trace each crop requirement's source, temporal basis, geographic applicability, evidence status and soil compatibility. Ensure annual/seasonal rainfall is never compared directly with seven-day totals. Flag unsupported humidity/soil scoring.
11. Confirm crop-calendar windows are approximate month-level evidence and whether agronomist validation exists. Do not infer exact planting dates or guaranteed outcomes.

### Phase D — Controlled QA (only with authorization and prerequisites)
12. Run existing unit tests and builds without changing source; capture commands, versions, exit codes and summaries.
13. In a safe disposable environment, verify public dashboard, recommendation and both seasonal workflows using actual data. Record HTTP status **and** semantic output. Test valid/invalid date ranges, all six crops, missing datasets, empty results and error messages.
14. With explicitly authorized disposable accounts, verify Registered User, Agricultural Officer and Administrator workflows. Check authorization on the backend, not just Angular guards.
15. With owner approval, execute a controlled Open-Meteo → Python inference → SQL persistence → Angular display test; do not alter production records or feature flags without permission.
16. Re-run dependency/security checks, redact secrets, and distinguish confirmed vulnerabilities from historic warnings.

### Phase E — Diagrams and dissertation alignment
17. Create a **diagram-to-code traceability matrix** with each arrow, actor, store, API and output linked to actual source paths and runtime proof where available.
18. Produce corrected diagrams as **editable Mermaid or draw.io source** plus PNG exports. Clearly distinguish *designed*, *implemented*, *runtime verified*, and *scientifically validated* claims in notes, not misleadingly in the same flow.
19. Provide a chapter impact list (especially Chapter 3 design and Chapter 4 results). **Do not rewrite approved dissertation chapters** without explicit permission; propose exact insertion/replacement text only.
20. Return an evidence-backed issue register prioritized Critical/High/Medium/Low, with reproduction steps, proposed fixes, acceptance criteria and regression tests.

## 7. Acceptance criteria and deliverables

**Required Codex outputs:**
1. `audit/IMPLEMENTATION_STATUS_MATRIX.md` — every function: `CODE_PRESENT`, `TESTED_PASS`, `TESTED_FAIL`, `BLOCKED`, or `NOT_IMPLEMENTED`; add evidence links.
2. `audit/THREE_OUTPUT_TRACEABILITY.md` — complete UI/API/service/dataset/model flow for OUT-1, OUT-2, OUT-3.
3. `audit/DIAGRAM_MISMATCH_REGISTER.md` — per diagram: Keep/Correct/Remove, exact mismatches, source references, severity.
4. `audit/QA_EXECUTION_LOG.md` — test commands, environment, results, missing prerequisites, no invented pass counts.
5. `audit/SCIENTIFIC_EVIDENCE_GAPS.md` — crop-by-crop evidence and prohibited claims.
6. `audit/CORRECTED_DIAGRAMS/` — editable diagrams + PNGs **only after** review of findings, or a proposed set if edit approval has not been given.
7. `audit/PRIORITIZED_FIX_PLAN.md` — safe, staged changes and tests; no changes to protected files without explicit owner approval.

**Minimum success:** No diagram implies that seasonal planning is a 7-day LSTM prediction; no crop score/rank is described as validated without evidence; three outputs and six crops are represented consistently; authorization boundaries are correct; all implementation claims link to code or reproducible runtime proof; unresolved QA blockers remain visible.

## 8. Safety / change-control requirements

- **First pass is read-only.** Do not edit source code, migrations, configurations, models, training artifacts, or dissertation DOCX files until the user explicitly approves a change plan.
- Do not create accounts, seed crop thresholds, alter weights, toggle production features, call paid/external services or write to real SQL data without permission.
- Preserve protected files and existing Git state; do not reset, force-push, normalize or auto-format unrelated files.
- Do not manufacture weather records, agricultural thresholds, approval signatures, officer feedback, test evidence or screenshots.
- Do not publish secrets. If secrets are present in development settings, report the risk without reproducing their values.

## 9. Copy/paste instruction for Codex

> You are auditing my MSc project, **AI-Driven Weather Forecasting and Crop Recommendation System for Anuradhapura District**. Read `Codex_Project_Implementation_and_Diagram_Audit_Handover.md` fully and treat it as a **read-only audit specification**, not permission to modify code. Inspect the Angular frontend, ASP.NET Core backend, Python LSTM forecasting service, historical diagrams, approved methodology and QA Markdown files. Reproduce and update the findings using actual source paths/line numbers and safe tests. Verify the three distinct outputs (7-day LSTM weather forecast; date-range crop-calendar candidates; crop-based cultivation-period candidates), the six crops, role permissions, data dependencies and scientific-evidence limitations. Separate code presence from tested operation and agricultural validation. Reconcile older QA findings with the latest code rather than assuming either is automatically correct. Produce the seven audit deliverables listed in Section 7 and a prioritized fix plan. **Do not edit application code, protected files, model artifacts, configurations, databases or dissertation chapters unless I explicitly approve a separate implementation phase.** If required datasets, credentials, tooling or approvals are absent, mark tests BLOCKED and list precisely what is missing. Never invent results.

---

**Final handover conclusion:** The uploaded source supports the existence of the intended multi-component architecture and both seasonal-planning API routes. It does **not**, by itself, establish complete end-to-end operation or scientifically validated six-crop recommendations. Correct the diagrams to reflect the three-output design, the latest code's conditional calendar evidence and the explicit insufficient-evidence path; close historical QA findings only after controlled re-testing.
