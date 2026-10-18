# Phase 3 — Approved Suitability Configuration

**Scope:** Anuradhapura District; Paddy, Maize, Green Gram, Cowpea, Groundnut and Chilli only.
**Approval basis:** Project-owner-approved configuration, 18 October 2026.
**Implementation status:** Active through the versioned crop-evidence catalogue. Database Administrator configuration remains an override path.

## Scoring rule and forecast aggregation

Each climatic factor is evaluated as a strict, inclusive binary band: **100** when the forecast aggregate is within the stated range and **0** when it is outside. The aggregation is a seven-day rainfall total for rainfall and a seven-day mean for temperature and relative humidity. No seasonal or annual value is converted to a weekly value.

Soil is optional and unscored by default. If no selected-soil compatibility is configured, it is excluded. The suitability engine re-normalizes the remaining weights: rainfall `0.40 / 0.65 = 0.615384...`, temperature `0.15 / 0.65 = 0.230769...`, and humidity `0.10 / 0.65 = 0.153846...`. It does not create a soil score.

## Approved strict seven-day climatic bands

| Crop | Rainfall total (mm / 7 days) | Mean temperature (°C / 7 days) | Mean relative humidity (% / 7 days) | Production context |
|---|---:|---:|---:|---|
| Paddy | 35–50 | 25–35 | 70–80 | Irrigated or rainfed; Dry Zone |
| Maize | 30–45 | 25–32 | 50–70 | Rainfed or irrigated; Dry Zone / Intermediate Zone |
| Green Gram | 20–35 | 28–30 | 60–70 | Rainfed / residual moisture; Dry Zone |
| Cowpea | 20–35 | 27–35 | 50–70 | Rainfed / dryland; Dry Zone |
| Groundnut | 25–40 | 24–33 | 50–70 | Rainfed (Maha) / irrigated; Dry Zone |
| Chilli | 25–40 | 20–30 | 50–65 | Rainfed / irrigated; Dry Zone |

## Global configuration

| Item | Approved value |
|---|---:|
| Rainfall weight | 0.40 |
| Temperature weight | 0.15 |
| Relative-humidity weight | 0.10 |
| Soil-compatibility weight | 0.35 (applied only when a selected-soil compatibility is configured) |
| Highly Suitable minimum | 80 |
| Suitable minimum | 50 |
| Moderately Suitable minimum | 25 |
| Below Moderately Suitable | Unsuitable |
| Cross-crop ranking | Permitted for results with a full mandatory-weather assessment |

## Configuration and governance

- The catalogue preserves the original Department of Agriculture calendar evidence for seasonal planning; calendar extraction is not used as a score.
- The three climatic factors above are all mandatory. A crop is `PARTIALLY_ASSESSED` when any mandatory factor is unavailable or incompatible; it then shows factor-level information only, with no overall score, category or rank.
- A crop is fully assessed when rainfall, temperature and humidity are all evaluable. Soil can be absent; if present, only an Administrator-entered, crop-specific selected-soil compatibility score can contribute.
- The existing Administrator APIs remain the approved configuration path for database overrides: `POST /api/admin/crop-requirements`, `POST /api/admin/soil-compatibility`, and `POST /api/admin/suitability-config`.
- An authorized Agricultural Officer must still review a safe persisted recommendation before QA-001 can be recorded as a live-workflow PASS.

## Verification sequence

1. Run the recommendation evaluation against a safe persisted seven-day forecast.
2. Confirm each mandatory factor is 100 only inside its inclusive range and 0 outside it.
3. With no soil selection, confirm no soil score is fabricated and the three climatic weights are re-normalized.
4. Confirm full mandatory-weather results receive a weighted overall score, category and rank.
5. Confirm missing mandatory evidence produces `PARTIALLY_ASSESSED` and suppresses the overall score, category and rank.
6. Preserve the evidence snapshot and obtain Agricultural Officer validation using the existing workflow.
