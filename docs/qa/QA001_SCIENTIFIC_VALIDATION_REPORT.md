# QA-001 Scientific Validation Report

**Date:** 18 October 2026
**Scope:** Anuradhapura District; Paddy, Maize, Green Gram, Cowpea, Groundnut and Chilli only.
**Decision:** The project-owner-approved Phase 3 matrix supports a strict, short-horizon climatic suitability calculation. QA-001 remains **PARTIAL** until an authorized Agricultural Officer completes the live review of a safe persisted recommendation.

## Validated operational method

- Rainfall uses the forecast **seven-day total**. Temperature and relative humidity each use the forecast **seven-day mean**.
- Each approved range is a strict inclusive binary band: `100` inside the range and `0` outside it. There are no fabricated transition margins.
- All six crops have approved ranges for all mandatory weather factors. Full results calculate a weighted overall score, a configured category and a comparable rank.
- Soil remains optional and unscored by default. If it is unavailable, only rainfall (`0.40`), temperature (`0.15`) and humidity (`0.10`) participate; the engine re-normalizes them across `0.65` to effective weights `0.615384...`, `0.230769...` and `0.153846...`.
- If a mandatory weather factor is absent or incompatible, the result remains `PARTIALLY_ASSESSED`, shows factor-level evidence, and suppresses score, category and rank.

## Approved crop matrix

| Crop | Rainfall total mm / 7 days | Mean temperature °C / 7 days | Mean humidity % / 7 days |
|---|---:|---:|---:|
| Paddy | 35–50 | 25–35 | 70–80 |
| Maize | 30–45 | 25–32 | 50–70 |
| Green Gram | 20–35 | 28–30 | 60–70 |
| Cowpea | 20–35 | 27–35 | 50–70 |
| Groundnut | 25–40 | 24–33 | 50–70 |
| Chilli | 25–40 | 20–30 | 50–65 |

The active, versioned data is [`crop-evidence.json`](../agriculture/crop-evidence.json); the approval and configuration record is [`PHASE3_SUITABILITY_CONFIGURATION_TEMPLATE.md`](PHASE3_SUITABILITY_CONFIGURATION_TEMPLATE.md). The original Department of Agriculture calendar sources remain available for seasonal planning only and are not converted into suitability thresholds.

## Approved global configuration

| Item | Value |
|---|---:|
| Rainfall / temperature / humidity / soil weights | 0.40 / 0.15 / 0.10 / 0.35 |
| Highly Suitable / Suitable / Moderately Suitable minimum | 80 / 50 / 25 |
| Cross-crop ranking | Permitted for fully assessed mandatory-weather results |

## Test evidence

- Backend regression: **299/299 passed** on 18 October 2026.
- Tests cover every crop’s exact strict ranges, strict inside/outside scoring, full assessment without soil, and exact three-factor effective weights.
- No LSTM artifact, forecast feature flag, database record, production setting or protected API project file was changed.

## Remaining decision and external validation

The scientific configuration decision is recorded. The single remaining QA-001 blocker is operational: provide an authorized Agricultural Officer account and a safe pending recommendation record, then complete the existing validation workflow and preserve its evidence. No additional thresholds, soil scores, categories or crops are required for this phase.
