# QA-001 Recommendation Fix Results

Date: 2026-10-17

## Root cause

The reported configuration baseline contained the six crop master records but no active `CropEnvironmentalRequirement`, `SoilCompatibility`, or suitability configuration records usable by the seven-day engine. `SuitabilityConfigurationProvider` therefore returned profiles with no evaluable factors; `CropSuitabilityEngine` correctly returned `InsufficientEvidence` for every crop. This was not an Angular rendering defect.

Direct SQL inspection under the current Windows identity was blocked by LocalDB login failure, so no database changes were made. The conclusion is also confirmed by the actual pre-fix response/UI and the documented live configuration state in `docs/CODEX_HANDOVER.md`.

## Implemented fix

- Added `docs/agriculture/crop-evidence.json` and `CropEvidenceCatalog`.
- The provider supplies a traceable FAO ECOCROP temperature range only where live SQL has no temperature requirement.
- The response carries assessment state, assessment basis, source URLs, and scientific limitations.
- Annual rainfall, humidity and unvalidated soil data remain unavailable rather than receiving fabricated scores.
- `PARTIALLY_ASSESSED` output has no suitability category. Rankings are now made only inside the same assessment state and basis; equal partial indicators remain unranked rather than receiving an arbitrary crop-order rank.

## Actual headed-browser result

Visible Google Chrome ran through Angular on `http://127.0.0.1:4300` against the ASP.NET Core API on 2026-10-17. `POST /api/recommendations/evaluate` returned 200 and rendered all six crops with their real source links.

| Crop | Visible result |
|---|---|
| Paddy | `No comparable rank`, 100 temperature compatibility indicator, `PARTIALLY_ASSESSED` |
| Maize | `No comparable rank`, 100 temperature compatibility indicator, `PARTIALLY_ASSESSED` |
| Green Gram | `No comparable rank`, 100 temperature compatibility indicator, `PARTIALLY_ASSESSED` |
| Cowpea | `No comparable rank`, 100 temperature compatibility indicator, `PARTIALLY_ASSESSED` |
| Groundnut | `No comparable rank`, 100 temperature compatibility indicator, `PARTIALLY_ASSESSED` |
| Chilli | `No comparable rank`, 100 temperature compatibility indicator, `PARTIALLY_ASSESSED` |

The common current forecast mean falls inside all six FAO optimal temperature ranges. The value is intentionally **not** a full crop suitability score, suitability category, or validated crop recommendation.

Evidence: `qa-evidence/PUB-04-live-recommendation.png` and `qa-evidence/visible-browser-observations.json`. Full public audit startup completed with `backend_ready=True frontend_ready=True`; the audit now explicitly waits for the recommendation response and rendered crop cards.

## Safe SQL follow-up

`docs/agriculture/SQL_QA001_READ_ONLY_CONFIGURATION_AUDIT.sql` is a credential-free, non-destructive audit for the authorized database owner. Run it in an approved development SQL session connected to the configured database, then provide the result set to identify active requirement, weight, and soil-compatibility records. It must not be used to seed unsupported rainfall, humidity, or soil thresholds.

## Status

QA-001 is **PARTIALLY RESOLVED**. The prior all-unscored failure is removed and useful evidence-backed partial results render end-to-end. Full approved scoring remains blocked by locally applicable, temporally compatible rainfall, humidity and soil evidence plus approved SQL configuration.
