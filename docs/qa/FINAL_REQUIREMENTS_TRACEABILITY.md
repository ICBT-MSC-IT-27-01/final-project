# Final Requirements Traceability

Status meanings: **PASS** = real technical evidence; **PARTIAL** = working but incomplete acceptance; **BLOCKED** = authorization/environment/evidence unavailable. Scientific acceptance is separate where relevant.

| ID | Acceptance outcome | Implementation / dependency | Evidence | Status / remaining gap |
|---|---|---|---|---|
| PRD-01 | Seven-day forecast displayed | Forecast API, stored `ForecastRun` | `PUB-01-home-desktop.png` | PASS technical; live inference blocked |
| PRD-02 | Explainable crop result returned | Recommendation orchestration/suitability engine | `PUB-04-live-recommendation.png` | PARTIAL; no approved active factors |
| PRD-03 | Date-range climate/evidence cards | Seasonal service/API/UI | `SP-01`–`SP-05b` | PARTIAL; extension approval/candidate rules absent |
| PRD-04 | Crop calendar source result | Seasonal service/API/UI | `CP-*.png` | PARTIAL; no verified cultivation windows |
| PRD-05 | Public and anonymous guards work | Angular guards/API policies | `SEC-*.png` | PARTIAL; authorized roles blocked |
| PRD-06 | History only when enabled | Existing feature-gated history | configuration/code inspection | BLOCKED live credentials/flag |
| PRD-07 | Officer validation only when enabled | Existing feature-gated validation | anonymous guard evidence | BLOCKED live credentials/flag |
| PRD-08 | Provenance and limitations visible | Forecast/recommendation/seasonal DTOs | public screenshots | PASS technical; scientific review pending |
| FR-01 | Public navigation reaches core pages | `app.ts`, routes | headed browser | PASS |
| FR-02 | Seven dated variables with honest source limits | Forecast controller/UI | `PUB-01` | PASS stored display; live path blocked |
| FR-03 | 30-day pipeline retained | provider orchestration/Python artifacts | existing tests | PASS design/tests; operational run blocked |
| FR-04 | Exactly six crops | approved names/API/UI | public screenshots | PASS |
| FR-05 | Factors/weights/evidence limits explained | suitability engine/UI | `PUB-04` | PARTIAL scientific configuration absent |
| FR-06 | Valid future range returns six real backend cards | seasonal service | `SP-*` | PARTIAL; no candidate assertion |
| FR-07 | Crop query returns calendar evidence/limits | seasonal service | `CP-*` | PARTIAL; no verified periods |
| FR-08 | Invalid ranges rejected | seasonal validation/UI | `SP-05c`–`SP-08` | PASS |
| FR-09 | Source type/provenance disclosed | seasonal/forecast responses | screenshots | PASS |
| FR-10 | Public privileged access rejected | policies/guards | `SEC-*` | PARTIAL; authorized-role run blocked |
| FR-11 | Own history feature behaviour | history service/routes | none | BLOCKED |
| FR-12 | Officer validation role isolation | validation service/routes | none | BLOCKED |
| FR-13 | Admin functions role restricted | admin API/UI | anonymous guard | PARTIAL; live admin test blocked |
| FR-14 | Actionable error state | controllers/UI | invalid-input evidence | PASS |
| FR-15 | Three viewport checks | Angular UI | `RSP-*` | PASS public flows |
| FR-16 | Exploration/insufficient evidence labels | seasonal DTO/UI | `SP-*`, `CP-*` | PASS |
| NFR-01 | RBAC/input validation | policies/controllers | tests + `SEC-*` | PARTIAL; advisory open |
| NFR-02 | Graceful failures | error mappings | tests | PARTIAL; no live fault injection |
| NFR-03 | Usable public flow | Angular forms/cards | headed browser | PASS public scope |
| NFR-04 | Labels/keyboard-friendly native forms | Angular templates | visual/manual run | PARTIAL; formal accessibility audit absent |
| NFR-05 | Requirement evidence linked | this matrix/evidence directory | repository docs | PASS |
| NFR-06 | Dataset/model provenance recorded | artifacts/seasonal responses | code/docs | PASS |
| NFR-07 | Measured threshold baseline | no approved threshold | build/test timings only | PARTIAL |
| NFR-08 | No unintended writes | read-only public endpoints | QA run | PASS public run |
| NFR-09 | Horizon and evidence boundaries enforced | suitability/seasonal service | screenshots | PASS technical; agronomic validation pending |
| WF-01 | Public forecast display | forecast API/UI | `PUB-01` | PASS stored display |
| WF-02 | Date range to crop candidates | seasonal API/UI | `SP-*` | PARTIAL exploration only |
| WF-03 | Crop to candidate periods | seasonal API/UI | `CP-*` | PARTIAL source only |
| WF-04 | Registered history | auth/history | none | BLOCKED |
| WF-05 | Officer review | validation API/UI | none | BLOCKED |
| WF-06 | Administrator operations | admin API/UI | anonymous guard | BLOCKED authorized run |
| WF-07 | Acquisition/inference/storage | provider + Python service | existing tests | PARTIAL live flag disabled |
| WF-08 | Suitability/evidence lifecycle | orchestration/seasonal UI | `PUB-04`, `SP-*` | PARTIAL; QA-001 open |

The prior QA coverage used WF-01 through WF-10 despite the source baseline defining WF-01 through WF-08. This final matrix uses the authoritative eight-workflow denominator.
