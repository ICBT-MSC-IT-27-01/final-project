# Chapter 6 System Testing Refined Draft

Generated: 2026-10-13

Status: refined draft for supervisor review. Current Phase 18 regression results are recorded in `docs/PHASE18_FINAL_VERIFICATION_REPORT.md`.

## 6.1 Testing Strategy

Testing combined automated backend tests, Python forecasting-service tests, Angular component/service tests, Angular production build verification and Playwright browser E2E tests. Controlled live operational integration evidence is taken from Phase 13; Phase 18 did not repeat live Open-Meteo calls, LSTM inference or SQL writes.

## 6.2 Backend Testing

Backend tests verify authentication, registration, role authorization, admin boundaries, forecast selection, provider-source validation, Python client validation, suitability scoring, recommendation orchestration, recommendation persistence behavior and Agricultural Officer validation.

## 6.3 Forecasting-Service Testing

Python tests verify data ingestion, preprocessing, chronological splitting, sequence preparation and inference API behavior. These tests support reproducibility of the weather forecasting pipeline.

## 6.4 Angular Testing

Angular tests verify routing, API services, authentication services, role guards and user-facing components for the main role workflows.

## 6.5 Browser E2E Testing

Playwright tests run in real Chrome browser projects for desktop and mobile viewport profiles. API responses are deterministic mocked fixtures. This validates browser behavior and role navigation without causing live Open-Meteo calls, Python inference or database writes.

## 6.6 Security And Failure Testing

Security coverage includes invalid login, protected routes, role boundaries, privileged self-registration prevention and feature-flag fail-closed behavior. Failure testing covers forecast unavailable states, disabled operational forecast generation and insufficient crop evidence.

## 6.7 Accessibility And Usability Checks

Browser tests use accessible roles and labels for interaction. The frontend includes visible focus styling. This is useful accessibility evidence but not a formal WCAG audit.

## 6.8 Test Results Summary

The final Phase 18 report records current counts after rerun. Results should be copied from `docs/PHASE18_FINAL_VERIFICATION_REPORT.md` into the formatted dissertation.

## 6.9 Testing Limitations

Real participant UAT, Agricultural Officer expert validation, production load testing, penetration testing and public deployment testing are not performed.
