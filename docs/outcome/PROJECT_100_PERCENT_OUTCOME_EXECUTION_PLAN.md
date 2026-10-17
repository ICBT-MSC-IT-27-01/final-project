# MSc Project — Complete Outcome Delivery Plan

**Project:** AI-Driven Weather Forecasting and Crop Recommendation System for Anuradhapura District  
**Version:** 1.0 | **Date:** 17 October 2026  
**Status:** Implementation plan; not supervisor/agronomist approval or a claim of completed work.

## A. Definition of 100% completion

Completion means **100% of applicable, approved Source of Truth acceptance criteria have real, reproducible evidence**, no unresolved Critical/High defects affecting those criteria, and clear recorded disposition for requested scope extensions. Technical completion, scientific validation, academic approval and operational accuracy are separate gates. Never force fabricated crop scores, thresholds, planting dates or test results to achieve a numerical 100%.

**Requirements baseline:** `PROJECT_SOURCE_OF_TRUTH_v1.md` — PRD-01–08, FR-01–16, NFR-01–09, WF-01–08. This is a *working* baseline and must be reconciled with original Chapter 3/4 design. **QA baseline:** `COMPLETE_SYSTEM_QA_REPORT.md`, `PROPOSED_VS_ACTUAL_WORKFLOW_AUDIT.md`, `QA_TEST_CASES.md`, `QA_WORKFLOW_COVERAGE.md`, `QA_DEFECT_REGISTER.md`, plus later Codex implementation report. Do not assume Codex claims equal independently reproduced results.

## B. Delivery sequence and gates

### Gate 0 — Baseline and environment (do not block independent implementation)
- Read original Chapter 3/4 and diagrams; reconcile with Source of Truth, label each item `ORIGINAL`, `REQUESTED EXTENSION`, `CONDITIONAL`, or `OUT OF SCOPE`.
- Record approval status with evidence; specifically WF-02/WF-03 crop-period/date-range functions are requested extensions, not proven original approvals.
- Trace each PRD/FR/NFR/WF item to source, code, API, data, acceptance test, owner and blocker.
- Reconcile QA workflow-coverage contradiction: one document says 1/10 fully matched, another says 0/10 although WF-10 is marked matched. Retain historical reports and issue corrected calculation.
- Record actual feature-flag defaults and environment safety. No secrets in logs.

**Exit:** Requirements traceability baseline exists; no missing requirement silently marked PASS. Scope approval may remain an explicitly owned external dependency.

### Gate 1 — Evidence acquisition and scientific feasibility (highest-priority dependency)
- Retrieve **individual** Department of Agriculture calendar documents for Paddy, Maize, Green Gram, Cowpea, Groundnut and Chilli, not merely the index URL: https://doa.gov.lk/naicc-publications-crop-calender/ . Record original file/URL, page/table, publication/version and extraction method.
- Cross-check Anuradhapura/Dry Zone applicability: https://doa.gov.lk/etc-inter-provincial-anuradhapura/ ; distinguish Maha/Yala, rainfed/irrigated, varieties and crop duration.
- Research crop-specific temperature, rainfall and humidity requirements, soil compatibility, and their **temporal units**. A season-total rainfall range cannot be applied directly to 7-day rainfall. Do not transfer location-specific thresholds without justification.
- Produce a machine-readable `CropEvidence` catalogue with fields: crop, variable, value/range, unit, temporal basis, location/zone, growth stage, water regime, season, source URL/document/page, extraction confidence, expert-review status, limitation.
- Establish an **agronomist review package** listing disputed or missing thresholds, suitability weighting, soil method and regional assumptions. Expert sign-off is an external task; do not fabricate it.
- Choose the simplest defensible soil method: use available, geographically appropriate soil data only if reliably mapped; otherwise soil is optional/unknown, disclose uncertainty and renormalize eligible evidence-backed factors. Do not invent field-level soil.

**Exit:** Each crop has traceable calendar/season evidence where available, explicit gap records, and a documented determination of which factors can legitimately be used for 7-day scores versus long-range candidate planning. **Block unsupported score claims**, not independent development.

### Gate 2 — Core approved crop suitability (QA-001 Critical)
- Trace Angular `POST /api/recommendations/evaluate` → ASP.NET services → SQL crop/requirement/configuration → factor calculation → result cards.
- Fix missing/invalid configuration and data access; configure only evidence-backed, **7-day-compatible** requirements and score semantics. Validate unit conversions, missing-data weight renormalization and cross-crop score comparability.
- Return each of six crops with factor-by-factor evidence, missing inputs, score/category/rank **only where scientifically defensible**, source/period/uncertainty, and meaningful explanation. Do not treat `InsufficientEvidence` for all crops as functional success; also do not fabricate scores to eliminate it.
- If evidence cannot justify seven-day crop suitability, obtain an explicit scientific-design decision to use supported climate compatibility or season-scale suitability with accurately renamed outputs, and reconcile this with the supervisor before claiming the original scored objective is fulfilled.
- Cover normal, partial-data, unsupported-factor, empty-dataset, invalid crop, and inconsistent-unit cases.

**Exit:** Real public browser → backend → real configuration/evidence → interpretable crop result, with scientific interpretation independently reviewed where claimed; QA-001 closed only on proof, otherwise explicitly BLOCKED pending evidence/review.

### Gate 3 — Seasonal extension: both user workflows (QA-002 / QA-003)
- Preserve current date validation (future, ordered, maximum 366 days) and ERA5 partial-month historical climatology, 2010–2025.
- Implement a versioned, source-traceable crop calendar knowledge base with season/month windows, crop duration, rainfed/irrigated conditions and geographic applicability. Do not infer exact sowing dates from a broad seasonal calendar.
- **WF-02:** User chooses date range → evaluate all six crops against supported calendar windows, duration and matching historical climatology → return evidence-tiered crop candidates with reasons, season, water-regime assumptions and source references. Unsupported crops remain explicitly insufficient, not silently ranked.
- **WF-03:** User chooses crop + period → return overlapping, applicable candidate cultivation windows/months and reasons for each, or a precise evidence gap. Test all six crops.
- Keep `HISTORICAL_SEASONAL_CLIMATE` distinct from 7-day LSTM prediction. A calendar-supported candidate is **not** a validated suitable planting recommendation.
- Get explicit supervisor scope-change decision before presenting these features as approved MSc objectives; implement user-requested functions in parallel where feasible.

**Exit:** Both forms yield actual source-backed candidate guidance wherever supported; no page that only shows historical averages is counted as full feature completion. QA-002/003 status supported by real screenshots/API responses and source provenance.

### Gate 4 — Live AI weather lifecycle (QA-006)
- In an authorized isolated development configuration, verify Open-Meteo observations for preceding 30 days → rainfall, mean temperature and mean RH preprocessing → original scaler/feature ordering → Python FastAPI/TensorFlow LSTM → seven dated predictions → ASP.NET API → permitted SQL persistence → Angular rendering.
- Respect `EnableWeatherModelIntegration` default false; test disabled behavior separately from safely enabled behavior. Do not alter production settings or retrain/replace the model silently.
- Verify dates, units, null handling, timezones, artifact version, provenance, failures and retrievability. Evaluate weather variables separately on held-out data where possible; do not represent historical mixed-unit MAE/RMSE as live accuracy.

**Exit:** A new, traceable end-to-end run observed (not just display of stored v1 forecast), or a precisely documented external operational blocker.

### Gate 5 — Roles, persistence, security and usability (QA-004 / QA-005)
- Test public pages without login; authorized registered login/logout and own-history behavior when enabled; Officer review/validation-only permissions when enabled; Administrator approved actions; backend denial of cross-role and cross-user requests.
- Obtain owner-provisioned, non-production test identities and safe existing/disposable test records; do not invent privileged credentials or bypass authentication. Feature flags `EnableRecommendationHistory`, `EnableOfficerValidation`, `EnableClimateRiskIndicators`, `EnableWeatherModelIntegration` remain disabled by default unless safely tested in isolated config.
- Confirm no privileged self-registration, data leakage, unauthorized writes or destructive SQL operations; define actual retention and privacy policy.
- Inspect NU1902/NU1903 `System.Security.Cryptography.Pkcs` 9.0.11 issue; remediate only through authorized change control. **Never modify protected `src/backend/AnuradhapuraAI.Api/AnuradhapuraAI.Api.csproj`**; if that file is required, record owner action and keep QA-005 open.
- Verify accessibility, desktop/tablet/mobile, meaningful error/loading states and measured performance against an agreed threshold.

**Exit:** All applicable role tests pass in a real browser and backend authorization tests; unresolved credentials or protected-file remediation are explicitly BLOCKED, never PASS.

### Gate 6 — Final acceptance and MSc evidence
- Start actual Angular, ASP.NET Core, Python service where authorized and SQL connectivity; run Playwright **Chromium headed (`headless: false`) on the user's interactive desktop**. If not possible, mark `VISIBLE_BROWSER_BLOCKED` and supply exact runnable local command; do not claim visibility without evidence.
- Test both core and seasonal workflows, all six crops, valid/invalid periods, all authorized roles, disabled/enabled feature-flag states, service failures, provenance, responsive layout and permissions.
- Preserve reproducible traces, screenshots, browser console/API evidence, model/data versions, seed/test configuration and sanitized logs.
- Review scientific claims with qualified agronomist; obtain supervisor scope decision and academic/ethics requirements as applicable. A technical QA pass is not expert validation or supervisor approval.
- Recompute traceability coverage from requirements, not from raw counts of trivial browser checks. No Critical/High release-blocking defect, and no unexplained missing accepted requirement.

**Exit:** Every applicable requirement is `PASS` with evidence; requested extensions are either explicitly approved and accepted, or documented as not approved and excluded from the *approved-scope* completion denominator. For the user's requested **full-feature** goal, extension features must also pass technically; approval is separately reported.

## C. Minimum acceptance matrix

| Outcome | Concrete acceptance evidence | Current reported state |
|---|---|---|
| PRD-01 / FR-02/03 / WF-01/07 — Weather | Fresh authorized 30-day operational input → Python inference → seven dated results → UI; disabled mode correct | Stored forecast only; fresh inference unverified |
| PRD-02 / FR-05 / WF-08 — Suitability | Six-crop real evaluation; valid applicable factor evidence; justified score/rank/category or explicit scientific-design resolution | QA-001 OPEN, all unscored |
| PRD-03 / FR-06 / WF-02 — Date → crops | All six considered; source-backed candidate crop output and correct evidence tier across 7/30/3/6/12-month inputs | Historical climate cards only; extension approval pending |
| PRD-04 / FR-07 / WF-03 — Crop → periods | Source-backed windows/season assumptions and duration for each crop where applicable; gap clearly identified | Calendar links only; no windows; extension approval pending |
| PRD-05–07 / FR-10–13 / WF-04–06 — Roles | Actual authorized user sessions, enabled/disabled behavior, role isolation, history/validation records as applicable | BLOCKED pending accounts/configuration |
| PRD-08 / FR-09/16 / NFR-09 — Provenance | Crop evidence, date/time scale, model vs climatology and uncertainty visible and correct | Partially implemented |
| FR-01/04/08/14/15; NFR-01–08 — Quality | Public UI, six-crop catalogue, validations, auth, responsiveness, performance baseline, failure recovery and reproducible QA | Partial; targeted re-verification required |

## D. Decision ownership and external dependencies

| Decision/dependency | Responsible party | Required evidence |
|---|---|---|
| Approve scope extensions WF-02/WF-03 | MSc supervisor | Written decision / meeting record |
| Approve agronomic thresholds, applicability, weights and interpretation | Qualified agronomist / agricultural officer in expert capacity | Review record, comments, evidence matrix version |
| Authorize isolated model integration test and credentials | Project owner / environment administrator | Safe configuration and test-account access |
| Authorize remediation requiring protected project file | Project owner | Explicit change-control approval (Codex must not touch protected file) |
| Confirm MSc ethics/UAT/evaluation requirements | MSc supervisor / institution | Actual approval or documented exemption as appropriate |

## E. Recommended execution cadence

**Execution order:** Gate 0 (brief) → Gate 1 evidence acquisition and feasibility → Gate 2 critical scoring → Gate 3 calendar planning → Gate 4 fresh AI inference → Gate 5 role/security → Gate 6 final acceptance. Work on independent tasks in parallel, but do not report final scientific acceptance before external reviews. **No reliable completion date can be promised** until the individual calendar documents, seven-day-compatible thresholds, authorized accounts and approval decisions are confirmed.

**Codex implementation instruction:** Prioritize working code and real data over new reports; run through all feasible gates continuously; if a scientific or authorization blocker is real, document it and proceed with independent work. Maintain protected-file restriction and do not commit/push unless explicitly authorized.

## F. Final evidence package

1. Reconciled `PROJECT_SOURCE_OF_TRUTH` requirement/change register with approval status.
2. Six-crop scientific evidence matrix with individual calendar source documents, applicability and review status.
3. Actual code/config changes for suitability, seasonal windows, fresh forecast integration and roles.
4. Requirements → code → API/data → Playwright → scientific-review traceability matrix.
5. Real headed-browser evidence and updated defect register with QA-001–006 closure status.
6. Separate **technical completeness**, **scientific validity**, **supervisor approval**, and **demonstration readiness** decisions.

**Non-negotiable principle:** 100% must describe proven satisfaction of agreed criteria, not an artificially forced percentage or a promise that uncertain science is settled.
