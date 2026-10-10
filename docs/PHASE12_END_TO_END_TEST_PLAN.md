# Phase 12 End-to-End Test Plan

Status: PLANNING ONLY - NOT EXECUTED AS LIVE INTEGRATION

This plan defines future end-to-end verification. It does not authorize live SQL Server writes, database cleanup, migrations, server changes, provider API calls, or credential changes.

Approval status values:

- SAFE NOW: can be run with existing non-persistent unit/mock tests.
- OWNER APPROVAL REQUIRED: requires live database writes, server configuration, credentials, provider calls, or manual deployed testing.
- DESIGN ONLY: not executable until future implementation exists.

## Test Matrix

| ID | Workflow | Preconditions | Steps | Expected result | Evidence | DB impact | Approval |
| --- | --- | --- | --- | --- | --- | --- | --- |
| E2E-01 | Public registration | API test host or deployed app | Submit valid registration | Registered User created; no privileged role assignment | API response, user role assertion | Write if live | OWNER APPROVAL REQUIRED live; SAFE NOW only with isolated test DB |
| E2E-02 | Login/logout | Existing registered user | Login, observe token/session, logout | JWT received; client session cleared on logout | API response, browser/session capture | None for login | OWNER APPROVAL REQUIRED live |
| E2E-03 | Registered-user authorization | Registered JWT | Access registered route/API | Allowed only for Registered User | HTTP status, route guard result | None | SAFE NOW in tests; live requires approval |
| E2E-04 | Anonymous recommendation | Complete forecast/config exists | POST `/api/recommendations` anonymously | Six crops returned; public `UserId` null | API response | Write recommendation if live | OWNER APPROVAL REQUIRED live |
| E2E-05 | Registered recommendation | Registered JWT | POST `/api/recommendations` | Recommendation owned by authenticated user | API response, DB row evidence | Write | OWNER APPROVAL REQUIRED live |
| E2E-06 | Recommendation persistence | Generated recommendation | Inspect persisted parent/crops | Six crop records and evidence snapshots stored | DB query or API detail | Read/write | OWNER APPROVAL REQUIRED live |
| E2E-07 | Registered history | Existing owned records | GET history | Newest-first owned records only | API response | Read | SAFE NOW with isolated test host; live approval |
| E2E-08 | History ownership isolation | Two registered users | User B requests User A detail | Safe not-found/forbidden without disclosure | HTTP status | Read | SAFE NOW in tests |
| E2E-09 | Officer access | Officer JWT | Open officer list | Officer-only access allowed | UI/API evidence | Read | Live approval |
| E2E-10 | Officer pending list | Pending recommendations exist | GET `/api/validations/pending` | Pending list returned | API/UI capture | Read | Live approval |
| E2E-11 | Officer detail | Recommendation exists | GET officer detail | Forecast/evidence/crops shown | API/UI capture | Read | Live approval |
| E2E-12 | Officer validation submission | Officer JWT | Submit Validated/Needs Review | Separate validation row stored; AI results unchanged | API response, DB evidence | Write | OWNER APPROVAL REQUIRED live |
| E2E-13 | Administrator access | Admin JWT | Open `/admin` | Admin-only UI/API access | UI/API evidence | Read | Live approval |
| E2E-14 | Administrator CRUD | Admin JWT and test records | Create/update/active-status supported resources | Backend validates and persists supported changes | API/UI/DB evidence | Write | OWNER APPROVAL REQUIRED live |
| E2E-15 | Feature disabled states | Feature flag disabled in isolated config | Call history/officer/weather paths | Controlled disabled responses | HTTP status/body | None | SAFE NOW with test config |
| E2E-16 | Weather model inference | Python artifacts present | POST 30 observations to Python `/forecast` | Seven predictions returned | pytest/API response | None | SAFE NOW with local tests |
| E2E-17 | Forecast persistence | Backend + Python configured | POST `/api/forecasts` with 30 observations | Seven `ForecastRecord` rows one `ForecastRunId` | API response, DB read | Write | OWNER APPROVAL REQUIRED live |
| E2E-18 | Latest complete forecast selection | Multiple valid/invalid runs in test DB | Query latest forecast/recommendation path | Latest valid run selected; invalid runs excluded | API/test assertion | Test DB write | SAFE NOW with in-memory; live approval |
| E2E-19 | Crop scoring and ranking | Valid config and forecast | Generate recommendation | Ranking follows approved deterministic order | API/test assertion | Write if persisted | SAFE NOW isolated; live approval |
| E2E-20 | Insufficient evidence | Missing factor configuration | Generate recommendation | Null scores/category/rank where insufficient | API/test assertion | Write if persisted | SAFE NOW isolated |
| E2E-21 | Invalid forecast runs | Duplicate/missing target dates | Select latest run | Invalid runs excluded | Unit/integration assertion | Test DB write | SAFE NOW isolated |
| E2E-22 | API validation errors | Malformed requests | Submit invalid district, soil, paging, status | Safe 400/401/403/404/503 as appropriate | HTTP evidence | None | SAFE NOW |
| E2E-23 | SQL Server unavailable | Backend configured to unavailable DB | Call read/write endpoints | Safe 503/500; no secrets leaked | Logs/API response | Server config | OWNER APPROVAL REQUIRED |
| E2E-24 | Python service unavailable | Backend points to stopped Python service | POST forecast | Safe service-unavailable response | API response | None | OWNER APPROVAL REQUIRED if server config changes |
| E2E-25 | JWT expiry/invalid tokens | Expired/invalid tokens | Access protected APIs | 401/403; no fallback auth | HTTP evidence | None | SAFE NOW with test host |
| E2E-26 | Angular desktop rendering | Dev server/browser | Exercise public/recommendation/history/officer/admin pages | Layout usable at desktop size | Screenshots | None | OWNER APPROVAL REQUIRED if server/dev env setup needed |
| E2E-27 | Angular mobile rendering | Browser mobile viewport | Exercise key pages | Mobile layout readable/no overlap | Screenshots | None | OWNER APPROVAL REQUIRED if browser automation unavailable |
| E2E-28 | Keyboard/accessibility checks | Browser/manual QA | Tab through forms/routes | Focus order, labels, errors usable | Checklist/screenshots | None | OWNER APPROVAL REQUIRED manual QA |
| E2E-29 | Security/privilege escalation | Multiple roles | Attempt cross-role endpoints and body spoofing | Backend rejects; no UserId/officer spoofing | API assertions | None | SAFE NOW in tests |
| E2E-30 | Historical evidence preservation | Existing recommendation then config changes | View saved history detail | Stored evidence unchanged; no recalculation | API/DB evidence | Write config changes | OWNER APPROVAL REQUIRED live |

## Test Data Requirements

- Dedicated registered, officer, and administrator users.
- Non-production JWT signing setup.
- Exactly six approved crops only.
- Controlled forecast runs with valid and invalid cases.
- Suitability configuration seeded only from approved values.
- Recommendation records with complete, partial, and insufficient evidence.
- Feature flag variants.
- Python forecast fixture with 30 chronological observations.

Do not use production data or protected User Secrets without owner approval.

## Rollback And Cleanup Strategy

For live or persistent tests, owner must approve:

1. dedicated test database or schema,
2. isolated test users,
3. migration state,
4. cleanup SQL or scripted rollback,
5. backups/snapshots before destructive actions.

No Phase 12 live cleanup was performed.

## Evidence To Capture

- command output with pass/fail counts
- API request/response examples with secrets redacted
- screenshots for Angular desktop/mobile QA
- database row-count/read-only verification where approved
- feature-flag configuration used
- source/provider provenance for weather tests
- limitations and failed/skipped tests

## Safe Test Environment Strategy

Safe now:

- backend unit/API tests using isolated test host/in-memory or approved non-persistent harness
- Angular Vitest tests with mocked APIs
- Python pytest with local fixtures/artifacts
- source-level contract checks

Not safe without separate approval:

- live SQL Server write tests
- migrations
- provider API ingestion
- server configuration changes
- browser/dev-server workflows requiring environment changes
- credential/User Secrets edits
