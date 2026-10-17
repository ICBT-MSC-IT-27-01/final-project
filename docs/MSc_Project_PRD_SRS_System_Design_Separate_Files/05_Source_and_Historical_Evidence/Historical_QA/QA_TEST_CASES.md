# QA Test Cases — Real Visible Browser Run

All browser cases used visible headed Google Chrome, 500 ms slow motion, and real local services. “PASS” means the stated test expectation occurred; it does not by itself override the proposed-vs-actual workflow assessment.

| Test ID | Workflow | Role | Scenario / observed result | Status | Evidence |
|---|---|---|---|---|---|
| QA-001 | startup | Public | API `/health` returned 200 | PASS | runner output |
| QA-002 | startup | Public | Angular root returned 200 | PASS | runner output |
| QA-003 | WF-01 | Public | Dashboard requested and rendered latest forecast | PASS | `PUB-01-home-desktop.png`; observations JSON |
| QA-004 | WF-08 | Public | Live evaluate returned 200, but six crops had no scores/rank/category | FAIL | `PUB-04-live-recommendation.png` |
| QA-005–010 | WF-02 extension | Public | 7-day, 30-day, 3-month, 6-month, 12-month, custom future ranges each returned 200 and six climate-exploration cards | PASS | `SP-01`–`SP-05b` PNGs |
| QA-011 | WF-10 | Public | Past date rejected with meaningful feedback | PASS | `SP-05c-past.png` |
| QA-012 | WF-10 | Public | Reversed date rejected | PASS | `SP-06-reversed.png` |
| QA-013 | WF-10 | Public | >366-day range rejected | PASS | `SP-07-over-366.png` |
| QA-014 | WF-10 | Public | Empty required dates blocked by browser validation | PASS | `SP-08-empty.png` |
| QA-015–020 | WF-03 extension | Public | Paddy, Maize, Green Gram, Cowpea, Groundnut, Chilli all returned `INSUFFICIENT_AGRONOMIC_EVIDENCE`, no candidate months | NOT IMPLEMENTED | `CP-*.png` |
| QA-021 | public | Public | Seasonal planning accessible without login | PASS | `PUB-03-seasonal-desktop.png` |
| QA-022 | RBAC | Anonymous | `/admin` redirected to login | PASS | `SEC-01-admin-anonymous.png` |
| QA-023 | RBAC | Anonymous | `/officer` redirected to login | PASS | `SEC-02-officer-anonymous.png` |
| QA-024 | responsive | Public | Desktop 1440×900 rendered usable forms/results | PASS | `PUB-03-seasonal-desktop.png` |
| QA-025 | responsive | Public | Tablet 768×1024 rendered | PASS | `RSP-01-tablet.png` |
| QA-026 | responsive | Public | Mobile 390×844 rendered | PASS | `RSP-02-mobile.png` |
| QA-027 | WF-04 | Registered User | login/logout/history not run: no authorized account | BLOCKED | no credentials supplied |
| QA-028 | WF-05 | Agricultural Officer | review/validation not run: no authorized account/record | BLOCKED | no credentials supplied |
| QA-029 | WF-06 | Administrator | administration not run: no authorized account | BLOCKED | no credentials supplied |
| QA-030 | WF-07 | Public | Persisted operational forecast carried model `v1`, seven daily values and was reused by evaluation | PASS | `PUB-04-live-recommendation.png` |

**Totals: 30 test cases — PASS 20, FAIL 1, BLOCKED 3, NOT IMPLEMENTED 6.**

Notes: expected HTTP 400 validation responses appeared as Chrome console resource errors; they are expected negative-test outcomes, not unhandled application failures. No backend-unavailability or Python-service-unavailability fault was induced because that would require disrupting the active configuration.
