# Phase 2 — Crop-Suitability Evidence Feasibility Review

**Date:** 18 October 2026
**Scope:** Anuradhapura District; Paddy, Maize, Green Gram, Cowpea, Groundnut and Chilli only.
**Method:** Targeted authoritative-source check authorised by the project owner. It supplements, rather than replaces, the existing repository evidence. No thresholds or configuration values were created from this review.

## Sources checked

| Source | Relevant finding | Configuration decision |
|---|---|---|
| [Sri Lanka DoA crop-calendar index](https://doa.gov.lk/naicc-publications-crop-calender/) | Official calendars are available for the six crops. | Supports seasonal calendar windows only, not seven-day suitability thresholds. |
| [Sri Lanka DoA FCRDI crop guidance](https://doa.gov.lk/fcrdi-downloads/) and [regional field-crop guidance](https://doa.gov.lk/fcrdi-rardckilinochchi-crops/) | Maize is described as suitable at 18–27 °C and needing irrigation every 4–7 days during dry periods; Cowpea is described as a warm-weather crop at 20–30 °C and irrigation every four days initially, then every seven days. Guidance also depends on drainage, soil, irrigation and drought severity. | Useful contextual/local agronomy evidence. It is not a rainfall-total or relative-humidity threshold for all six crops and must not be transformed into one. |
| [Sri Lanka DoA rice-water management guidance](https://doa.gov.lk/rrdi_watermanagement/) | Rice response to water stress varies by growth stage, crop establishment method, rainfall distribution and irrigation availability. | No fixed seven-day rainfall rule can be derived. |
| [FAO crop-water-needs guidance](https://www.fao.org/4/S2022E/s2022e07.htm) | Crop water need is determined by crop stage and crop coefficient; coefficients vary with relative humidity and wind. Reported water needs are for crop stages or total growing periods. | Cannot be used as a direct seven-day rainfall-suitability score because the system has no crop-stage input, reference evapotranspiration, wind, radiation, field water balance or approved irrigation state. |
| Existing [crop-evidence catalogue](../agriculture/crop-evidence.json) | All six crops retain global FAO ECOCROP temperature ranges, but rainfall values are annual and humidity thresholds are absent. | Existing temperature-only `PARTIALLY_ASSESSED` indicator remains the only supported live output. |

## Scientific result

The LSTM supplies seven daily rainfall, temperature and relative-humidity values. The located evidence does **not** establish crop-specific, locally applicable seven-day rainfall or humidity criteria for all six crops. In particular:

- Crop-water/irrigation guidance cannot be treated as rainfall need because irrigation, effective rainfall, soil storage, drainage, growth stage and evapotranspiration differ.
- Relative humidity in the FAO water-balance method modifies crop-coefficient calculations with wind and other inputs; it is not a stand-alone crop suitability threshold.
- Paddy needs separate consideration for rainfed, supplementary-irrigated and irrigated systems; a single district-wide weekly rainfall threshold would be misleading.
- Annual rainfall values remain temporally incompatible and must not be divided by weeks or months.

## Phase 2 decision

No active `CropEnvironmentalRequirement`, `SoilCompatibility`, factor weight or category-threshold record may be added from this research. No full weighted score, suitability category, cross-crop rank or live climate-risk threshold is justified.

The current system must continue to:

- show source-linked seven-day temperature compatibility only as `PARTIALLY_ASSESSED`;
- identify rainfall, humidity and unvalidated soil as not evaluable;
- withhold overall scores, categories and comparable ranks; and
- retain QA-001 as PARTIAL.

## Exact expert decision still required

An authorized Sri Lankan agricultural expert must approve a method for each crop that states: crop stage; rainfed/irrigated context; soil or effective-rainfall assumption; seven-day aggregation and unit; rainfall rule; humidity rule or an approved exemption; local applicability; source; factor weights; category thresholds; and whether the result supports ranking. The approved method must then be tested with a safe, traceable configuration before QA-001 can become PASS.
