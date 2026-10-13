# Phase 16 Requirements Traceability

Generated: 2026-10-13

## Matrix

| Requirement | Implementation | API/component | Test evidence | Status |
| --- | --- | --- | --- | --- |
| Anuradhapura-only scope | District validation and Anuradhapura UI copy | `RecommendationsController`, Angular dashboard | Backend endpoint tests; browser E2E public workflow | PASS |
| Seven-day weather forecast | Latest eligible operational selector and forecast UI | `GET /api/forecasts/latest`, `PublicDashboardComponent` | Backend forecast tests; browser E2E dashboard | PASS |
| Six supported crops | Fixed approved crop list | `ApprovedCropNames`, recommendation UI | Suitability tests; browser E2E crop display | PASS |
| Explainable suitability | Factor breakdown and explanation fields | Suitability engine, recommendation DTOs | Backend suitability tests; recommendation E2E | PASS |
| Optional soil | Missing soil message and weight renormalization | Suitability engine, recommendation page | Backend suitability tests; browser E2E missing soil | PASS |
| Public access | Public dashboard and read-only recommendation | Angular routes, `/api/recommendations/evaluate` | Browser E2E public workflow; API tests | PASS |
| Registered access | Login, logout, history route | `AuthService`, history components | Browser E2E registered workflow | PASS |
| Agricultural Officer validation | Officer-only validation workflow | `/api/validations/*`, officer components | Backend validation tests; browser E2E officer workflow | PASS technical |
| Administrator permissions | Admin-only UI and backend contracts | `/api/admin/*`, admin component | Backend authorization tests; browser E2E admin workflow | PASS technical |
| Recommendation history flag | Fail-closed history persistence | `EnableRecommendationHistory` | Backend tests | PASS |
| Operational forecast provenance | ForecastRun provenance persisted and displayed | Forecast DTOs, dashboard/recommendation UI | Phase 13 evidence; browser E2E | PASS |
| Missing scientific evidence | Insufficient-evidence response | Suitability engine, recommendation API | Phase 15 tests; browser E2E | PASS |
| Authentication and authorization | JWT backend, local frontend session, route guards | Auth services and policies | Backend auth tests; browser E2E route guard checks | PASS |
| Input validation | District, soil length, JSON errors | API controllers | Backend endpoint tests | PASS |
| Error handling | Safe frontend/backend error messages | API error mapping, Angular notices | Browser E2E failure tests; backend tests | PASS |
| Reliability | Controlled failure handling | Mocked browser/API tests | Browser E2E fail-closed forecast generation | PASS technical |
| Accessibility | Semantic labels/headings and visible focus | Angular templates, `styles.scss` | Browser tests by role/label; focus style added | PARTIAL |
| Deployment readiness | Checklist only, no deployment | Documentation | `PHASE16_DEPLOYMENT_READINESS.md` | PARTIAL |
| Real participant UAT | Prepared instruments only | UAT pack | `PHASE16_UAT_TEST_PLAN.md` | NOT PERFORMED |
| Expert officer validation | Prepared protocol only | Officer protocol | Phase 15/16 docs | NOT PERFORMED |

## Notes

Automated test evidence is technical evidence. It does not replace participant UAT, expert Agricultural Officer review, or agronomic field validation.
