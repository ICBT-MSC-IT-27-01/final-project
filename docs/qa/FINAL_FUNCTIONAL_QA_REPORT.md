# Final Functional QA Report

## 18 October 2026 status addendum

This report's original verdict records the pre-approved-configuration audit on 17 October 2026. It is retained as historical evidence and is superseded for current technical status by this addendum and `QA_TEST_CASES.md`.

**Current technical verdict: PASS — ready for an MSc technical demonstration, subject to the boundaries below.**

- Strict seven-day rainfall, temperature and humidity bands for the six approved crops, owner-approved weights/categories, and optional-soil re-normalisation are configured and regression-tested. `QA-001` is PASS (technical), not a claim of real-world agronomic outcome validation.
- Local development has authorized `EnableRecommendationHistory` and `EnableOfficerValidation`; a safe QA recommendation was persisted, reviewed by the authorized Officer account, validated, and retained in history.
- Authorized Administrator, Registered User and Agricultural Officer technical workflows have been tested. Angular now persists Registered User-generated recommendations and displays saved-history confirmation; Officer review displays stored forecast evidence and all factor results.
- Fresh full regression on 18 October 2026 passed: backend 299/299; Python 23/23 (one upstream Starlette/httpx deprecation warning); Angular 46/46; headed-Chrome E2E 14/14; Angular production build PASS.
- The security dependency advisory recorded in the original report was remediated by the approved compatible package patch; the protected API project file was not changed.

**Still external/not claimed:** real participant UAT, ethics approval where required, and independent Agricultural Officer/DoA agronomic validation. These are acceptance/research activities, not remaining application defects.

**Date:** 17 October 2026  
**Verdict:** **PARTIAL — not ready to claim complete supervisor demonstration readiness.**

## Verified technical behaviour

- Angular, ASP.NET Core and the existing persisted operational forecast started through `qa/run-visible-audit.ps1`; headed Chromium completed the public workflow run.
- `GET /api/forecasts/latest` returned and displayed seven dated rainfall, temperature and relative-humidity values from stored model version `v1`.
- The public recommendation endpoint returned the six approved crops and transparent factor-level insufficiency. It did **not** return a scientific recommendation because the active SQL configuration contains no approved crop requirement, soil compatibility, or suitability configuration rows.
- Seasonal date-range queries for seven days, thirty days, three months, six months, twelve months, and a custom interval returned six real cards. They now include partial-month calendar-day climatology and traceable DoA calendar/Anuradhapura-office sources, but are correctly labelled climate exploration / insufficient agronomic evidence.
- All six crop-to-period searches returned their individual official-calendar source link and Dry Zone applicability statement. They do not assert cultivation windows or exact sowing dates.
- Past, reversed, empty, and over-366-day inputs produced visible validation feedback. Anonymous `/admin` and `/officer` requests redirected to login. Desktop, tablet, and mobile screenshots were captured.

## Evidence

- `qa-evidence/visible-browser-observations.json`
- `qa-evidence/PUB-04-live-recommendation.png`
- `qa-evidence/SP-01-seven-days.png` through `qa-evidence/SP-08-empty.png`
- `qa-evidence/CP-paddy.png` through `qa-evidence/CP-chilli.png`
- `qa-evidence/RSP-01-tablet.png`, `qa-evidence/RSP-02-mobile.png`

## Automated checks

- `dotnet build src/backend/AnuradhapuraAI.slnx --no-restore` — passed, with existing `System.Security.Cryptography.Pkcs` advisory warnings.
- `dotnet test tests/AnuradhapuraAI.Phase3Tests/AnuradhapuraAI.Phase3Tests.csproj --no-build` — 281 passed.
- `npm run build` in `src/frontend` — passed.

## Scientific boundary

The DoA calendar index publishes the individual calendar links and Dry Zone applicability for Cowpea, Chilli, Green Gram, Groundnut and Maize, and Dry/Intermediate rainfed/irrigated calendars for Paddy. The Anuradhapura office confirms district-level Maha/Yala activity. Neither establishes a crop-specific, time-compatible seven-day rainfall/RH/soil scoring threshold or a verified individual cultivation window for this implementation. The application therefore must not display scores, ranks, planting dates, or validated seasonal suitability until those inputs are reviewed and configured.

## Role and inference limits

- Registered history, officer validation, and administrator actions remain **BLOCKED** for live QA because no authorized disposable accounts and records were supplied. Anonymous authorization rejection is verified only.
- Fresh Open-Meteo → 30-day → Python LSTM → persistence execution remains **BLOCKED** because the integration feature flag is intentionally disabled. Stored forecast retrieval is verified; live accuracy is not claimed.
