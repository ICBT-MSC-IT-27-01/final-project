# Chapter 6 Testing Draft

Generated: 2026-10-13

Status: proposed draft material. Current Phase 17 regression results are recorded in `docs/PHASE17_FINAL_VERIFICATION_REPORT.md`.

## 6.1 Testing Strategy

The testing strategy combines backend unit and API tests, Python forecasting-service tests, Angular component and service tests, production build verification, browser E2E tests and documentation-based evidence checks. Live operational weather generation and SQL writes are not repeated during Phase 17 because they are not required for dissertation drafting and could create unnecessary side effects.

## 6.2 Backend Testing

Backend tests cover authentication, role authorization, forecast selection, Python client validation, provider-source validation, recommendation orchestration, recommendation persistence, suitability scoring, admin boundaries and officer validation.

## 6.3 Python Forecasting-Service Testing

Python tests validate ingestion, preprocessing, chronological splitting, sequence preparation, baseline behavior, model API shape and inference response validation.

## 6.4 Angular Testing

Angular tests cover route configuration, API service behavior, authentication service behavior, role guards and core components for public, recommendation, registered, officer and admin workflows.

## 6.5 Browser E2E Testing

Playwright browser E2E tests exercise public, registered, Agricultural Officer and Administrator flows using deterministic mocked API responses. This avoids live Open-Meteo calls, LSTM inference and SQL writes. Browser E2E should be presented as browser workflow validation, not live production validation.

## 6.6 Security And Authorization Testing

Security tests check anonymous restrictions, role boundaries, invalid login behavior, protected route redirects, privileged self-registration prevention and feature-flag fail-closed behavior.

## 6.7 Accessibility And Usability Checks

The frontend uses semantic headings, labels, alerts and status regions in tested workflows. Phase 16 added visible focus styling. This is partial accessibility evidence and should not be described as WCAG certification.

## 6.8 Failure-State Testing

Controlled failure tests cover forecast unavailable responses, disabled operational generation, invalid weather values, malformed forecast responses, insufficient crop evidence and invalid authentication.

## 6.9 Test Case Summary

| Test ID | Requirement | Objective | Expected result | Evidence |
| --- | --- | --- | --- | --- |
| T-01 | Authentication | Registered user can register/login | Token and user returned | Backend and E2E tests |
| T-02 | Privilege control | No privileged self-registration | Role selection absent/ignored | Auth tests and E2E |
| T-03 | Forecast API | Latest eligible run selected | Seven consecutive days returned | Forecast endpoint tests |
| T-04 | Python integration | Client validates forecast responses | Invalid payloads rejected | Python client tests |
| T-05 | Recommendation read-only | Evaluate does not write history | Response only | Recommendation endpoint tests |
| T-06 | Suitability scoring | Score boundaries and categories | Deterministic categories | Suitability tests |
| T-07 | Missing soil | Renormalize valid factors | No fabricated soil score | Suitability tests |
| T-08 | Officer validation | Officer can submit review status | Validation persisted in test host | Validation tests |
| T-09 | Admin boundary | Admin-only endpoints protected | Non-admin denied | Authorization tests |
| T-10 | Browser workflows | Public/registered/officer/admin pages work | 14 E2E tests pass | Playwright results |

## 6.10 Known Testing Limitations

Real participant UAT, Agricultural Officer expert validation, production deployment testing and long-running operational accuracy monitoring are not performed.
