# Proposed vs Actual Workflow Audit

## Design-source and diagram inventory

The primary approved-design narrative is the final Chapter 3/4 DOCX set in `D:\My\MSc\Assignment\Final Project\Reports\plan\Chapter 3` and `...\Chapter 4`. Those chapters refer to diagrams as “will be inserted”; the actual diagram image set is in `...\plan\Diagrams\V1`, `V2`, and `V3`. A file named `Supervisor_Diagram_Explanation_QA_Final.pdf` exists, but no signed supervisor approval record was located. Therefore formal diagram approval is **NOT VERIFIABLE FROM SOURCE**.

| Diagram requested | Actual source | Actors/processes/data flow identified | Approval evidence |
|---|---|---|---|
| Use Case | `V3/4. Use Case Diagram.png`; `V1/AI Crop Recommendation Use Case Diagram.png` | Public, Registered User, Officer, Administrator; forecast, recommendation, history, validation, administration | Not verifiable |
| System Architecture | `V2/AI Weather and Crop Recommendation Architecture.png` | Angular → API/application → SQL Server; API → Python LSTM | Not verifiable |
| DFD | `V3/2. Level 0 Data Flow Diagram.png`, `V3/3. Level 1 Data Flow Diagram.png`, `V2/Crop Suitability Calculation Data Flow Diagram.png` | request/response, forecast/recommendation configuration stores | Not verifiable |
| Activity | `V1/AI Weather and Crop Activity Diagram.png` | authentication, forecast, suitability decision flow | Not verifiable |
| Sequence | V2 weather, registration/login, recommendation, history, officer, admin sequence PNGs | Angular/API/SQL/Python interactions | Not verifiable |
| ER | `V2/AI Weather Forecasting ER Diagram.png`; `V1/AI Agriculture ER Diagram.png` | UserRole/User/Crop/requirements/soil/config/forecast/recommendation/validation | Not verifiable |
| Class | No final class-diagram source located | Not verifiable from available source | Not verifiable |
| AI Forecasting workflow | `V2/AI Weather Forecasting Workflow.png`; `V3/6. Model Work Flow.png` | historical data → preprocessing/scaler → LSTM → 7 days | Not verifiable |
| Crop recommendation workflow | `V2/Crop Suitability Recommendation Workflow.png` | forecast + requirements + soil + configuration → scores/rank/explanation | Not verifiable |
| Overall system flowchart | `V3/7. System Work Flow.png` | user interactions through system components | Not verifiable |

## Traceability matrix

| Workflow | Baseline source | Proposed actor/steps/output | Actual implementation path | Browser evidence | Status | Gap/severity |
|---|---|---|---|---|---|---|
| WF-01 Forecast | Ch.3 §3.4.3; Ch.4 §§4.6.2,4.7.3 | Public → LSTM 30-day context → 7-day Rainfall/Temperature/Humidity | dashboard → `GET /api/forecasts/latest` → forecast service → persisted operational run | `PUB-01-home-desktop.png`; API 200 | PARTIALLY MATCHES | Retrieval/display proved; current provider/inference generation not exercised. Medium |
| WF-02 Date range → crops | Not present in Ch.3/4; later extension only | Requested extension, not approved baseline | seasonal component → `POST /by-date-range` → ERA5 CSV monthly aggregation | `SP-01`–`SP-08` | NOT VERIFIABLE FROM AVAILABLE DESIGN | Returns climate exploration / insufficient evidence, no suitability assessment. High |
| WF-03 Crop → period | Not present in Ch.3/4; later extension only | Requested extension, not approved baseline | seasonal component → `POST /by-crop` → ERA5 CSV monthly aggregation | `CP-*.png` | NOT VERIFIABLE FROM AVAILABLE DESIGN | No candidate period/calendar. High |
| WF-04 Registered history | Ch.3 §§3.4.2,3.4.5; Ch.4 §§4.6.4,4.7.5 | login → authorized history/detail | routes + auth API + history endpoints/services | none: credentials absent | BLOCKED | Cannot claim implementation from mock E2E/code only. High |
| WF-05 Officer validation | Ch.3 §3.4.6; Ch.4 §§4.6.5,4.7.6 | officer login → review → status/comment/history | officer routes → validation API/service | anonymous redirect only | BLOCKED | No authorized officer account/record. High |
| WF-06 Admin | Ch.3 §3.4.7; Ch.4 §§4.6.6,4.7.7 | admin login → manage configuration/users/records | admin route/API/service | anonymous redirect | BLOCKED | No authorized administrator QA account. High |
| WF-07 Acquisition/inference/storage | Ch.4 §§4.6.2,4.7.3 | weather acquisition → preprocess → LSTM → storage/retrieval | Open-Meteo adapters → Python client → ForecastRun/ForecastRecord | stored run retrieval only | PARTIALLY MATCHES | live generation flag deliberately off; no new inference. Medium |
| WF-08 Suitability/explanation | Ch.3 §3.4.4; Ch.4 §§4.6.3,4.7.4 | retrieve config → score four factors → weight/category/rank/explain | recommendation UI → `POST /evaluate` → orchestration/ranking service → SQL config | `PUB-04-live-recommendation.png`, API 200 | DOES NOT MATCH | Six crops all unscored/Unranked due no configuration. Critical |
| WF-09 Persistence/history | Ch.3 §§3.4.5–3.4.8; Ch.4 §§4.6.4–4.7.7 | authorized storage and retrieval | EF Core entities/services/endpoints | non-mutating audit only | BLOCKED | No authorized test identity/records; no writes made. High |
| WF-10 Error handling | Ch.3 §3.4.8; Ch.4 §4.6.1 | invalid/unavailable input → meaningful feedback | controller validation + Angular alerts/native required fields | `SP-05c`, `SP-06`–`SP-08` | MATCHES PROPOSED DESIGN | Validated examples shown. |

### Coverage

Denominator: 10 workflow groups explicitly requested for the audit. **0/10 fully MATCHES**, 3/10 partially/fully evidenced (WF-01, WF-07, WF-10), 1/10 does not match (WF-08), 4/10 blocked (WF-04–06, WF-09), and 2/10 are requested extensions not verifiable as approved design (WF-02–03). See `QA_WORKFLOW_COVERAGE.md`.
