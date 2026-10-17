# MSc Project — Single Source of Truth

**Project:** AI-Driven Weather Forecasting and Crop Recommendation System for Anuradhapura District  
**Document ID:** SOT-001 | **Version:** 1.0 (working baseline) | **Date:** 17 October 2026  
**Status:** DRAFT FOR RECONCILIATION — not an independently verified approved specification  
**Intended use:** One reference for requirements, workflow implementation, QA, supervisor review and final MSc evaluation.

> **Evidence boundary:** This baseline consolidates information stated in the project discussions and Codex reports. The original signed-off proposal, exact approved diagrams, current repository contents and live application were not accessible for independent inspection when this document was created. 'Reported implemented' is not 'QA verified'; 'requested' is not 'supervisor approved'.

## 1. Product Requirements Document (PRD)

### 1.1 Problem and purpose
Agricultural planning in Anuradhapura District benefits from accessible weather information and explainable crop decision support. The proposed system combines a short-horizon AI weather forecast with crop recommendation logic and, as a separately tracked requested extension, historical seasonal-climate exploration and crop-period guidance. It is **decision support**, not a guarantee of agronomic success or a substitute for agricultural expertise.

### 1.2 Project objective
Design, develop and evaluate an integrated web application for Anuradhapura District that provides weather forecasts and explainable crop-related guidance using appropriately labelled weather, climate and agronomic evidence.

### 1.3 Geographic, crop and user scope
- **Geographic coverage:** Whole Anuradhapura District, Sri Lanka; no verified subdistrict or field-level precision.
- **Supported crops:** Paddy, Maize, Green Gram, Cowpea, Groundnut and Chilli.
- **User roles:** Public, Registered User, Agricultural Officer, Administrator.
- **Public access:** Weather and relevant crop-planning functions without compulsory registration.
- **Agricultural Officer:** Review/validation only when enabled and authorized; not unrestricted administration.
- **Languages:** Do not assume multilingual support is delivered; confirm against approved scope.

### 1.4 Product capabilities
| ID | Capability | Expected user outcome | Baseline classification |
|---|---|---|---|
| PRD-01 | Seven-day AI weather forecast | View labelled rainfall, temperature and humidity forecast where enabled and operational | Original project capability; operational status requires verification |
| PRD-02 | Explainable crop recommendation | View crops and explanations based on valid inputs and defensible crop requirements | Original project capability; scientific completeness unverified |
| PRD-03 | Date range to crop candidates | Select a period up to 366 days and receive evidence-backed crop candidates, not invented suitability | User-requested extension; supervisor approval unverified |
| PRD-04 | Crop to cultivation periods | Select one of six crops and see evidence-backed candidate periods with assumptions | User-requested extension; supervisor approval unverified |
| PRD-05 | Role-based access | Appropriate public, registered, officer and administrator permissions | Project requirement; live behavior unverified |
| PRD-06 | Recommendation history | Authorized access to stored history when feature enabled | Conditional feature, default disabled |
| PRD-07 | Officer validation | Officer can review/validate recommendations when enabled | Conditional feature, default disabled; expert validation not yet evidenced |
| PRD-08 | Scientific transparency | Show provenance, period, uncertainty and evidence status | Required for defensible decisions |

### 1.5 Explicit exclusions and change control
The initial agreed scope excludes NPK/organic-matter estimation, irrigation optimization, IoT, pests, prices, yield prediction, planting-time prediction, multilingual/native mobile apps, autonomous decisions and expansion beyond Anuradhapura. **Crop-to-period planning and longer-term date-range recommendations must therefore be recorded as scope changes requiring supervisor confirmation**, even though the user has asked for their implementation. They must not be described as previously approved without documentary evidence.

## 2. Software Requirements Specification (SRS)

### 2.1 Functional requirements
| ID | Requirement | Acceptance criteria (observable) | Priority |
|---|---|---|---|
| FR-01 | Public landing and navigation | Unauthenticated user can reach public forecast and planning pages without privileged access | Must |
| FR-02 | Seven-day weather forecast | When integration is enabled and healthy, real request returns 7 dated predictions with correct variables and source/limitations; otherwise honest disabled/unavailable state | Must |
| FR-03 | Forecast data preparation | Operational pipeline uses 30 preceding days of rainfall, mean temperature and mean relative humidity; documents gaps and transformation | Must |
| FR-04 | Crop catalogue | UI and API support exactly the six in-scope crops, with stable identifiers | Must |
| FR-05 | Crop suitability engine | Explains factors, weights, optional soil handling, missing-factor renormalization and evidence limits; does not invent thresholds | Must |
| FR-06 | Date-range crop workflow | User chooses valid future dates (up to 366 days), submits, and receives real backend results for all six crops with explicit evidence tier, climate basis and caveats | Requested extension |
| FR-07 | Crop-period workflow | User selects crop and search interval, receives candidate windows only where official calendar/requirements support them, otherwise clear evidence gap | Requested extension |
| FR-08 | Date/input validation | Missing, reversed, past or >366-day ranges are handled consistently in UI and API; invalid crop rejected | Must |
| FR-09 | Provenance | Result identifies whether LSTM forecast, historical climatology, crop calendar, or other evidence was used | Must |
| FR-10 | Authentication and authorization | Roles enforced by backend; public cannot perform privileged actions; no officer/admin self-registration | Must |
| FR-11 | Recommendation history | When enabled, authorized records can be viewed and retained under defined policy; when disabled, no misleading UI or unauthorized writes | Conditional |
| FR-12 | Officer validation | When enabled, only authorized officer may review/validate; never present technical test as real expert validation | Conditional |
| FR-13 | Administration | Existing approved admin functions accessible only to administrators; exact actions to be verified from approved design | Must / verify |
| FR-14 | Error handling | API/service/data failures produce actionable, non-fabricated errors without secrets | Must |
| FR-15 | Responsive UI | Core flows usable on desktop, tablet and mobile viewports | Must |
| FR-16 | Recommendation evidence status | Results distinguish calendar-supported candidate, climate-compatible candidate, assessed suitability, exploration-only, insufficient evidence and data unavailable | Must for extensions |

### 2.2 Non-functional requirements
| ID | Requirement | Verification method |
|---|---|---|
| NFR-01 | Security | Backend authorization, input validation, secret redaction, no privilege escalation |
| NFR-02 | Reliability | Graceful response to weather/API/AI/database unavailability |
| NFR-03 | Usability | Public user can complete primary flows without assistance; meaningful labels and errors |
| NFR-04 | Accessibility | Keyboard navigation, accessible labels and readable contrast; record actual findings |
| NFR-05 | Traceability | Requirement ID links to workflow, diagram, implementation, test and evidence |
| NFR-06 | Reproducibility | Dataset coverage, model version, feature preprocessing and source provenance recorded |
| NFR-07 | Performance | Establish measured response-time baseline and agreed thresholds before declaring pass |
| NFR-08 | Data integrity | Avoid unauthorized writes and protect stored user/recommendation records |
| NFR-09 | Scientific integrity | Do not extrapolate a 7-day LSTM into 3–12 month forecasts; do not claim unsupported suitability |

### 2.3 Existing implementation and evidence — reported, not independently verified
- **Frontend:** Angular. **Backend:** ASP.NET Core (Api/Application/Domain/Infrastructure). **Database:** SQL Server. **AI service:** Python FastAPI/TensorFlow LSTM.
- **Historical weather:** `docs/anuradhapura_weather_2010_2025_era5.csv`, reported coverage 2010-01-01 to 2025-12-31.
- **Operational weather:** Open-Meteo, reportedly last 30 days ending yesterday; operational compatibility and accuracy unverified.
- **Forecast:** LSTM 30-day input → 7-day output. Historical aggregate MAE 3.1633, RMSE 6.7294, baseline MAE 3.5835, RMSE 8.7379 were reported mixed-unit metrics; **not** live accuracy or recommendation accuracy.
- **Crop scoring:** Weighted rainfall/temperature/RH and optional soil, missing-factor renormalization; nominal bands ≥80 Highly Suitable, ≥60 Suitable, ≥40 Moderately Suitable, below 40 Unsuitable. **These bands alone do not establish scientifically validated thresholds for a given time horizon.**
- **Reported seasonal API:** `POST /api/seasonal-recommendations/by-date-range`, `POST /api/seasonal-recommendations/by-crop`, `GET /api/seasonal-recommendations/supported-crops`.
- **Reported seasonal route:** `/seasonal-planning`. Historical calendar-month climatology, not long-range weather forecast.
- **Latest available Codex evidence:** UI and real API integration reportedly worked, but all six crops were still returning `INSUFFICIENT_AGRONOMIC_EVIDENCE`; official crop-calendar index was located, individual calendars not confirmed extracted. **Do not label both primary seasonal recommendation flows complete until new evidence demonstrates it.**
- **Feature flags reported default false:** `EnableOfficerValidation`, `EnableRecommendationHistory`, `EnableClimateRiskIndicators`, `EnableWeatherModelIntegration`.

## 3. Proposed workflows and acceptance criteria

### WF-01 — Public weather forecast
**Actor:** Public user. **Flow:** Open forecast page → request/view forecast → backend obtains approved operational input → 30-day preprocessing → LSTM inference when enabled → seven dated outputs → UI shows values, source, model limits.  
**Acceptance:** Visible Chromium browser + real API evidence of seven dated values when service is operational; otherwise accurate disabled/unavailable status. **No claim of 3–12 month LSTM prediction.**

### WF-02 — Date range → crop candidates (requested extension)
**Actor:** Public or other authorized user. **Flow:** Open seasonal planning → choose start/end dates → validate (future, ordered, max 366 days) → backend obtains matching historical seasonal climatology and crop evidence → assess each of six crops → render evidence-tiered candidate results and explanations.  
**Acceptance:** A real response and meaningful candidate guidance only for supported crops; unsupported crops must show evidence gaps. A 200 response containing only climate statistics is **partial**, not complete recommendation functionality.

### WF-03 — Crop → cultivation periods (requested extension)
**Actor:** Public or other authorized user. **Flow:** Select crop → specify period → retrieve official crop calendar and applicable requirements → consider historical seasonal climate and growing duration → show source-backed candidate windows/season assumptions → display limitations.  
**Acceptance:** Evidence-backed periods for applicable crops, not fabricated exact dates. If only historical statistics appear, mark **partial**.

### WF-04 — Registered account and history
**Actor:** Registered user. **Flow:** Login → access allowed features → view own history only if enabled → logout.  
**Acceptance:** Valid authorization; disabled history is clearly handled; no cross-user data access.

### WF-05 — Agricultural Officer review
**Actor:** Agricultural Officer. **Flow:** Authorized login → view recommendation → review/validate only when enabled → preserve traceable decision.  
**Acceptance:** Officer has validation-only permissions and no admin escalation. Real agricultural expert validation requires separate evidence.

### WF-06 — Administrator operations
**Actor:** Administrator. **Flow:** Authorized login → access documented admin functions → perform permitted actions → logout.  
**Acceptance:** Role isolation; actual functions reconciled with approved use-case diagram rather than assumed.

### WF-07 — Weather/model data lifecycle
**Flow:** ERA5 training/evaluation data → preprocessing → model training and historical evaluation; separately Open-Meteo operational observations → 30-day features → inference → API result.  
**Acceptance:** Historical/operational data provenance kept distinct, preprocessing and model artifact traceable, limitations documented.

### WF-08 — Crop recommendation evidence lifecycle
**Flow:** User request → validate period and crop → load scientifically appropriate climate and agronomic evidence → assess suitability/candidate status → explain factors and missing evidence → UI.  
**Acceptance:** No inappropriate annual/growing-season thresholds applied to weekly/monthly data; no unsupported categorical 'suitable' claims.

## 4. Architecture and design reference

**Expected logical architecture:** Angular UI → ASP.NET Core API → application/domain services → SQL Server and/or historical ERA5 dataset; backend communicates with Python FastAPI LSTM service where enabled; external operational weather source supplies observations. The exact deployed topology, contracts and diagrams must be reconciled against repository evidence.

**Design artefacts to trace:** Use Case, System Architecture, DFD, Activity, Sequence, ERD, Class, AI Workflow, Crop Recommendation Workflow, Overall Flowchart. Their *existence and approved status have not been independently verified here*. Formal UI Design System (colors, typography, components, responsive and accessibility rules) also requires verification or creation.

## 5. Scientific data and decision rules

1. **Short horizon:** LSTM is a 7-day weather forecasting model, not a 3-, 6- or 12-month forecast model.
2. **Long horizon:** Use historical calendar-month climatology and official crop-calendar evidence, explicitly labelled `HISTORICAL_SEASONAL_CLIMATE`.
3. **Agronomy:** Official Sri Lanka Department of Agriculture crop calendars are candidate evidence, but region, irrigation, rainfed/irrigated and crop duration applicability must be checked. Calendar index: https://doa.gov.lk/naicc-publications-crop-calender/ . Anuradhapura context: https://doa.gov.lk/etc-inter-provincial-anuradhapura/ . The individual calendars were not confirmed ingested in the latest report.
4. **Evidence tiers:** `CALENDAR_SUPPORTED_CANDIDATE`, `CLIMATE_COMPATIBLE_CANDIDATE`, `SUITABILITY_ASSESSED`, `CLIMATE_EXPLORATION_ONLY`, `INSUFFICIENT_AGRONOMIC_EVIDENCE`, `DATA_UNAVAILABLE`. These are proposed/previously requested labels; inspect actual code for exact enums.
5. **Validation:** Scientific suitability requires appropriate source-backed crop thresholds, temporal units and expert review; source presence alone does not prove validity.
6. **Uncertainty:** Each result must state source, relevant period, assumptions, gaps and interpretation limits.

## 6. Role / permission baseline

| Capability | Public | Registered | Officer | Administrator |
|---|---|---|---|---|
| View public weather information | Yes | Yes | Yes | Yes |
| Use public crop-planning flows | Yes | Yes | Yes | Yes |
| View own history | No | If enabled | Only as authorized | As explicitly authorized |
| Validate recommendations | No | No | If enabled | Not by default; verify policy |
| Administer system | No | No | No | Authorized admin functions only |
| Self-register as privileged role | No | No | No | No |

**Important:** Detailed admin actions and whether officer can see all recommendation records must be derived from approved diagrams/policy, not guessed.

## 7. QA acceptance and traceability

### 7.1 Acceptance levels
- **PASS — technical:** Actual browser → API → real service/data → UI response matches defined behavior.
- **PASS — scientific:** Technical pass **plus** crop-specific evidence, correct temporal/geographic interpretation and documented validation where claimed.
- **PARTIAL:** UI/API works but core decision output is missing or only exploratory.
- **FAIL:** Implemented workflow violates requirement or produces incorrect output.
- **BLOCKED:** Environment/credentials/source unavailable; never count as pass.
- **NOT VERIFIED:** No independently inspected evidence.

### 7.2 Mandatory traceability matrix
| Requirement | Workflow | Source design evidence | Implementation evidence | Browser/API QA | Current baseline |
|---|---|---|---|---|---|
| FR-02, FR-03 | WF-01, WF-07 | Approved AI workflow needed | LSTM and integration reported | Visible end-to-end not verified | NOT VERIFIED |
| FR-05, FR-09 | WF-08 | Crop recommendation design needed | Weighted scoring reported | Scientific validity not verified | PARTIAL / NOT VERIFIED |
| FR-06 | WF-02 | Requested scope extension; approval pending | Seasonal API/UI reported | Reported live proxy/API; no final candidate evidence | PARTIAL (reported) |
| FR-07 | WF-03 | Requested scope extension; approval pending | Crop search UI/API reported | Six crops reportedly insufficient evidence | PARTIAL (reported) |
| FR-10 | WF-04–06 | Approved use cases needed | Role framework reported | Role E2E not verified | NOT VERIFIED |
| FR-11, FR-12 | WF-04–05 | Feature-flag policy | Default disabled reported | Enabled-path QA not verified | CONDITIONAL / NOT VERIFIED |
| FR-15 | WF-01–06 | UI design needed | Angular app reported | Headed Chromium QA not verified | NOT VERIFIED |

### 7.3 Required visible Playwright QA
Run Angular, ASP.NET Core, Python service where required and authorized, and relevant SQL Server connections; execute real Playwright Chromium with `headless: false` on the user's interactive desktop. Cover desktop/tablet/mobile, public and authorized roles, both crop workflows for all six crops, seven-day forecast, invalid inputs, provenance and error handling. Save screenshots/traces and a defects register. If a visible local browser cannot be launched, report `VISIBLE_BROWSER_BLOCKED` and provide an executable local command; do not fabricate visibility.

### 7.4 Release / demonstration gates
- **G1 — design:** Original approved diagrams and requirements located and mapped; scope extensions explicitly reviewed.
- **G2 — core functions:** Public forecast and crop recommendation behavior demonstrated in real application, with honest evidence statuses.
- **G3 — crop-period extensions:** If included in demo scope, both workflows produce meaningful, sourced results rather than only climate exploration; otherwise clearly identified as incomplete.
- **G4 — role/security:** Correct authorization and feature-flag handling demonstrated.
- **G5 — scientific:** Model evaluation, source provenance, agronomic limits and expert validation status stated accurately.
- **G6 — QA:** Visible headed-browser test evidence, defects, reproducible results and acceptance decision.
- **G7 — academic:** Supervisor review, ethics requirements (where applicable), dissertation and evaluation evidence complete.

**Current release readiness:** **NOT DETERMINED by independent QA**. Based solely on the latest shared Codex report, the two requested seasonal workflows are **partial**; therefore do not claim fully complete for demonstration.

## 8. Open decisions and evidence gaps

| ID | Decision or missing evidence | Owner / next action |
|---|---|---|
| GAP-01 | Actual approved proposal and diagram files | Locate files and record exact path/version and approval evidence |
| GAP-02 | Supervisor approval for long-range date/crop and crop/period workflows | Request explicit scope-change confirmation; do not backdate approval |
| GAP-03 | Official crop calendars for all six crops, extracted and applicability checked | Ingest and trace individual source documents; agricultural review |
| GAP-04 | Scientifically valid thresholds per crop and temporal scale | Research, source, document units and obtain validation |
| GAP-05 | Real 7-day operational forecast integration and accuracy | Execute service E2E; evaluate separately from historical metrics |
| GAP-06 | Authorized role accounts and history/validation feature policy | Confirm actual role permissions and safe QA credentials |
| GAP-07 | Full UI design system and exact diagrams | Reconcile against design artefacts and Angular implementation |
| GAP-08 | Visible Chromium E2E QA and actual user acceptance | Execute, collect evidence and report honestly |
| GAP-09 | SQL Server schema and retention/privacy decisions | Inspect approved DB design and data policy |
| GAP-10 | Supervisor, ethics and academic completion evidence | Confirm documented approvals and evaluation requirements |

## 9. Repository handover and update policy

**Suggested repository location:** `docs/requirements/PROJECT_SOURCE_OF_TRUTH.md` (this file is provided as a draft for review; it has not been added to the user's repository).

**Repository for reconciliation:** https://github.com/ICBT-MSC-IT-27-01/final-project.git  
**Known local route:** `/seasonal-planning`  
**Known backend health path:** `/health`  
**Known safety restriction:** Never modify, stage, revert, reset, stash, normalize, commit or push `src/backend/AnuradhapuraAI.Api/AnuradhapuraAI.Api.csproj`; protect credentials and unrelated local work.

**Change-control rules:**
1. Every new requirement gets an ID, owner, priority, rationale, scope status and acceptance criteria.
2. Each approved change updates the affected PRD, SRS, workflow, diagram, API and QA mappings together.
3. Keep three distinct states: **Proposed/Requested**, **Approved**, and **Implemented + QA Verified**.
4. Source hierarchy: signed-off supervisor-approved artefacts → versioned design/requirements → inspected implementation → reproducible QA evidence → informal Codex reports. Disagreements remain open until resolved.
5. Do not overwrite evidence gaps with assumptions. Record date, reviewer and evidence path for every status change.

## 10. Baseline sign-off register

| Role | Name | Decision | Date | Evidence |
|---|---|---|---|---|
| Project owner | Pending | Draft prepared; not signed off | — | — |
| MSc supervisor | Pending | Not independently confirmed | — | — |
| Agricultural expert | Pending | Scientific validation not evidenced | — | — |
| QA reviewer | Pending | Full E2E acceptance not evidenced | — | — |

**Document conclusion:** This is a usable **single working baseline** for implementation and QA alignment. It is **not yet the final approved source of truth** until the original approved documents are reconciled and the open decisions are resolved.
