# Complete System QA Report

## Audit result

**Overall verdict: PARTIALLY.** The application follows the approved Angular → ASP.NET Core → SQL Server/Python-LSTM architecture and delivers a real seven-day forecast to the browser. It does not deliver the approved, scored and ranked crop-suitability outcome because the live configuration has no evaluable crop requirements, soil compatibility or thresholds. The separately added seasonal-planning feature is honest historical climate exploration, not an approved replacement for crop recommendations.

**Release readiness: NOT READY** for supervisor demonstration of the complete proposed workflow.

## Scope and method

- Original source: Chapter 3 and Chapter 4 final DOCX files under `D:\My\MSc\Assignment\Final Project\Reports\plan`, plus the V1–V3 diagram files there.
- Implementation inspected: Angular routes/components/services; ASP.NET controllers, application/infrastructure services; LSTM artifacts; configuration; EF/SQL integration code; seasonal extension.
- Real services: ASP.NET Core at `http://127.0.0.1:5055` and Angular at `http://127.0.0.1:4300` started with existing Development configuration. `/health` returned 200.
- Browser: visible Google Chrome driven by Playwright, headed (`headless: false`) with 500 ms slow motion. No mocked API was used by this audit.
- Evidence: `qa-evidence/visible-browser-observations.json` and PNG files in `qa-evidence/`.

## Visible browser findings

- The dashboard made a real `GET /api/forecasts/latest` request (200) and displayed the stored seven-day `v1` forecast.
- The public recommendation page made a real `POST /api/recommendations/evaluate` request (200). It showed seven forecast days and all six crops, but each was unranked, had no score/category, and listed Rainfall, Temperature, Humidity and Soil as unavailable. See `qa-evidence/PUB-04-live-recommendation.png`.
- Seasonal date-range calls made real `POST /api/seasonal-recommendations/by-date-range` requests. Seven-day, 30-day, three-month, six-month, twelve-month and custom future ranges returned 200 and six cards labelled `INSUFFICIENT_AGRONOMIC_EVIDENCE`.
- Every crop-to-period call returned 200 but explicitly stated `INSUFFICIENT_AGRONOMIC_EVIDENCE` and asserted no candidate planting period. This is correct safe disclosure but not the requested business outcome.
- Reversed, past and >366-day ranges returned 400 with visible, specific validation feedback. Empty inputs were prevented by browser required-field validation.
- Desktop (1440×900), tablet (768×1024), and mobile (390×844) screens were captured. Anonymous attempts to `/admin` and `/officer` redirected to login.

## Weather and data-source assessment

- The code path is Angular public dashboard → `GET /api/forecasts/latest` → `ForecastService`/operational-run selector → persisted `ForecastRun`/`ForecastRecord` data. The UI evidence identified model `v1` and seven daily Rainfall/Temperature/Humidity values.
- The forecasting-service source and `v1_era5_2010_2025` artifact metadata confirm the designed 30-day, three-variable, seven-day LSTM flow. Existing documentation identifies ERA5 2010–2025 as the historical dataset and Open-Meteo as the operational-input adapter when enabled.
- This run did not enable feature flags, generate a new operational run, call Open-Meteo, call Python inference, or write SQL records. It therefore verifies retrieval/display of a real stored run—not current external-provider availability or live predictive accuracy.

## Role and security assessment

- Public access to dashboard, recommendations and seasonal planning was verified.
- Anonymous access to officer/admin routes was blocked at the Angular route guard. Backend authorization code and policies were inspected.
- No authorized Registered User, Agricultural Officer or Administrator QA credentials were supplied or discovered. Login, history, officer validation, and admin management were **BLOCKED**, not treated as passing from code inspection or prior mocked E2E.
- No accounts, privileged accounts, recommendation records, validation records, or configuration rows were created or changed.

## Major defects

| ID | Severity | Finding |
|---|---|---|
| QA-001 | Critical | Live crop recommendation has no evaluable requirements/soil/configuration, so it returns no scores, categories or ranking for every approved crop. |
| QA-002 | High | Crop → suitable cultivation periods is a seasonal extension with no candidate periods, locally applicable calendar, Maha/Yala guidance, irrigation assumptions or crop-duration assessment. |
| QA-003 | High | Date Range → recommended crops is climate exploration only; it must not be presented as a validated recommendation workflow. |
| QA-004 | High | Full role workflow proof is unavailable without authorized QA credentials and read-only test records. |
| QA-005 | Medium | Package audit warnings identify high/moderate vulnerabilities in `System.Security.Cryptography.Pkcs` 9.0.11. |

## Required corrective actions

1. Obtain supervisor/agronomist-approved, time-compatible crop requirement ranges, soil source/selection method, weights and thresholds; configure all six crops and prove ranked live results.
2. If seasonal planning is retained, obtain explicit scope approval and verified crop calendars/period rules. Otherwise label it only as exploratory climate context and remove “recommended/suitable periods” wording.
3. Supply non-privileged, authorized QA accounts and disposable read-only/canned records to execute the registered, officer and administrator workflows without altering real data.
4. Run an authorized controlled operational forecast generation test proving Open-Meteo → 30-day preprocessing → Python LSTM → SQL persistence, while retaining the documented model/source compatibility limitation.
5. Upgrade/remediate the vulnerable dependency only under the owner’s protected-project-file/change-control process.

## Assurance statement

No application source code was modified. The protected `src/backend/AnuradhapuraAI.Api/AnuradhapuraAI.Api.csproj` was not modified, staged, reverted, reset, normalized, committed or pushed. This audit added only QA documentation, browser evidence, and QA-only runner scripts outside application source.
