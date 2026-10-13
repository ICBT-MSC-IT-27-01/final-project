# Phase 14 Crop Requirements

Generated: 2026-10-13

Project: AI-Driven Weather Forecasting and Crop Recommendation System for Anuradhapura District

## Scope

The approved crop scope remains exactly:

1. Paddy
2. Maize
3. Green Gram
4. Cowpea
5. Groundnut
6. Chilli

No additional crops were introduced in Phase 14.

## Configuration Status

The existing backend supports auditable crop environmental requirements through `CropEnvironmentalRequirement`, soil compatibility through `SoilCompatibility`, and factor weights/category thresholds through `SuitabilityConfiguration`.

Phase 14 did not seed new production-like agronomic thresholds because no repository-approved source values were found that could be defensibly mapped directly to the current 7-day forecast scoring model for all six crops.

Current status:

| Crop | Rainfall | Temperature | Humidity | Soil |
| --- | --- | --- | --- | --- |
| Paddy | Missing/Unverified | Missing/Unverified | Missing/Unverified | Missing/Unverified |
| Maize | Missing/Unverified | Missing/Unverified | Missing/Unverified | Missing/Unverified |
| Green Gram | Missing/Unverified | Missing/Unverified | Missing/Unverified | Missing/Unverified |
| Cowpea | Missing/Unverified | Missing/Unverified | Missing/Unverified | Missing/Unverified |
| Groundnut | Missing/Unverified | Missing/Unverified | Missing/Unverified | Missing/Unverified |
| Chilli | Missing/Unverified | Missing/Unverified | Missing/Unverified | Missing/Unverified |

The system therefore returns explicit insufficient-evidence or partial-evidence outcomes when required crop configuration is absent.

## Source Review

Sources reviewed during Phase 14:

- FAO dry-zone Sri Lanka project context: `https://www.fao.org/agroecology/in-action/detail/dry-zone-of-sri-lanka---climate-smart-intensification-of-upland-and-lowland-crop-production-systems/en`
- FAO EcoCrop data portal: `https://www.fao.org/geospatial/data-and-tools/data-portals/ecocrop/en`
- FAO crop water and irrigation guidance: `https://www.fao.org/4/U3160E/u3160e04.htm`
- Sri Lanka Department of Agriculture crop calendar publications: `https://doa.gov.lk/naicc-publications-crop-calender/`

These sources are useful for dissertation research and future configuration review, but they do not by themselves establish validated 7-day rainfall, 7-day mean temperature, 7-day mean relative humidity, and optional soil scoring thresholds for the exact implemented model.

## Compatibility Cautions

Important mapping issues:

- A seasonal or monthly rainfall requirement must not be treated as a 7-day rainfall requirement.
- Crop water requirement values are not the same as observed rainfall suitability.
- General climate suitability ranges may describe crop growth over a season, not a 7-day forecast window.
- Soil suitability is site-specific; district-wide soil assumptions were not introduced.
- Humidity requirements were not inferred from generic climate descriptions.

## Approved Handling

Phase 14 preserves the approved explainable weighted scoring approach:

- Score range: 0 to 100.
- Classification: `Highly Suitable`, `Suitable`, `Moderately Suitable`, `Unsuitable`.
- Missing optional factors are excluded from the effective weight total.
- If no factor is evaluable, the crop is marked `InsufficientEvidence`.
- Rankings are deterministic and only assigned to evaluated crops.

## Future Agronomic Work

Before live agronomic recommendations can be claimed as scientifically validated, the project needs source-reviewed, crop-specific, unit-compatible, time-window-compatible requirement values for the six supported crops. The preferred evidence should be Sri Lanka or dry-zone relevant and should document the units, aggregation period, applicability, and limitations of every configured threshold.
