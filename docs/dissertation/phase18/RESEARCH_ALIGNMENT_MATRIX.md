# Phase 18 Research Alignment Matrix

Generated: 2026-10-13

Approved research-objective wording was not available in repository chapter files. The matrix below uses repository-approved project requirements and marks the exact dissertation objective wording as unresolved.

| Alignment ID | Project objective inferred from approved scope | Implementation evidence | Test/evaluation evidence | Achievement status | Limitation |
| --- | --- | --- | --- | --- | --- |
| RA-01 | Forecast seven days of weather for Anuradhapura District | Python LSTM, backend forecast integration | Python tests, backend forecast tests, ERA5 metrics | Achieved technically | Operational accuracy not independently verified |
| RA-02 | Use rainfall, temperature and humidity as forecast variables | Model artifacts, source code | Metadata and tests | Achieved | Overall metrics are mixed-unit |
| RA-03 | Recommend among six approved crops | Approved constants, recommendation UI/API | Suitability and E2E tests | Achieved technically | Scientific agronomic validation partial |
| RA-04 | Keep recommendation explainable | Factor breakdowns, suitability engine | Backend tests and Phase 16 evidence | Achieved technically | Explanations depend on configured evidence |
| RA-05 | Support public and authenticated user workflows | Angular routes, auth, API controllers | Backend and browser tests | Achieved technically | Real participant UAT not performed |
| RA-06 | Support Agricultural Officer validation | Validation service and officer UI | Validation tests and E2E | Achieved technically | Real expert review not performed |
| RA-07 | Support Administrator configuration | Admin services and UI | Admin tests and E2E | Achieved technically | Final agronomic values require approval |
| RA-08 | Preserve secure role boundaries | JWT auth, policies, route guards | Backend and E2E tests | Achieved technically | Production security review not performed |
| RA-09 | Provide dissertation-ready evaluation evidence | Phase 15-18 docs | Regression and artifact audits | Achieved for available sources | Approved Chapters 1-4 missing |
