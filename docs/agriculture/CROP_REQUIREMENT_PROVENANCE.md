# Crop Requirement Provenance

Machine-readable source: `docs/agriculture/crop-evidence.json` (`stage2-crop-evidence-v1`). The backend loads it through `CropEvidenceCatalog`; values are not anonymous constants in the scoring code.

## Compatibility policy

- `Temperature`: FAO ECOCROP optimal/absolute crop ecological ranges are aggregated as the arithmetic mean of seven forecast daily temperatures. They produce only a `PARTIALLY_ASSESSED` temperature compatibility indicator.
- `Rainfall`: all retained values are explicitly `annual rainfall`; the engine marks them `NOT_EVALUABLE` for a seven-day forecast and does not score them.
- `Relative humidity`: no numeric crop-specific threshold was verified; `NOT_EVALUABLE`.
- `Soil`: no district-wide soil assumption is made. Until a user-selected soil type has a validated compatibility record, this factor is `NOT_EVALUABLE`.
- Category and full rank: only `SUITABILITY_ASSESSED` records may show a suitability category. Partial indicators are ranked only within the same assessment basis and labelled `Indicator #`.

## Evidence status

- DoA crop calendars: `OFFICIAL_APPLICABLE_CALENDAR` where the artifact names Anuradhapura or its Dry Zone/DL-1d context; paddy is conditional on rainfed/irrigated production.
- FAO ECOCROP temperature data: `RESEARCH_SUPPORTED_NOT_LOCALLY_VALIDATED`; global ecological data, not local expert validation.
- FAO ECOCROP rainfall data: `INCOMPATIBLE_TEMPORAL_SCALE` for this seven-day use case.

Scientific validation by an Agricultural Officer or local agronomist remains pending. No evidence in this repository authorizes converting the partial indicators into approved crop recommendations.
