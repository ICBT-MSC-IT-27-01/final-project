# Scientific evidence and prohibited claims

The current technical scoring method is documented in `docs/qa/PHASE3_SUITABILITY_CONFIGURATION_TEMPLATE.md` and versioned in `docs/agriculture/crop-evidence.json`. Its three mandatory weather bands are **project-owner-approved configuration**. `docs/agriculture/SIX_CROP_SCIENTIFIC_EVIDENCE_MATRIX.md` predates that decision and describes narrower independently sourced temperature evidence. These records should remain distinguishable: owner approval permits the prototype calculation; it is not independent agronomic validation of seven-day rainfall and humidity bands.

| Crop | Owner-approved seven-day rainfall total / mean temperature / mean humidity bands | Calendar applicability and unresolved review |
| --- | --- | --- |
| Paddy | 35–50 mm / 25–35 °C / 70–80% | Dry/Intermediate Zone calendar is conditional on irrigation or rainfed context and local zone match. No farm conditions were supplied in this run. |
| Maize | 30–45 mm / 25–32 °C / 50–70% | Dry Zone calendar includes Anuradhapura; selected November range returned climate context only, not a matching establishment window. |
| Green Gram | 20–35 mm / 28–30 °C / 60–70% | Dry Zone calendar includes Anuradhapura; local seasonal conditions still require review. |
| Cowpea | 20–35 mm / 27–35 °C / 50–70% | Dry Zone calendar includes Anuradhapura; local seasonal conditions still require review. |
| Groundnut | 25–40 mm / 24–33 °C / 50–70% | Calendar references Anuradhapura DL-1d; other district subregions require mapping. |
| Chilli | 25–40 mm / 20–30 °C / 50–65% | Dry Zone calendar includes Anuradhapura; local seasonal conditions still require review. |

For all six crops, the DoA calendar extraction JSON records `agronomistValidated=false` and `productionApproved=false`. It supplies approximate establishment months, not exact planting dates, irrigation sufficiency, or full cultivation suitability.

The current strict scoring rule gives 100 inside an approved band and 0 outside. Rainfall uses the seven-day total; temperature and humidity use seven-day means. Soil is optional, and the default calculation excludes it and renormalizes active weights. This explains why the current local forecast can produce six complete scores while all six categories are `Unsuitable`. A rank orders scores; it does not convert an Unsuitable crop into a suitable one.

## Claims to avoid

- Do not call the seven-day owner-approved rainfall/humidity bands independently agronomist-validated or geographically precise without a signed review and source appraisal.
- Do not compare annual or seasonal rainfall figures from older FAO ECOCROP evidence directly with a seven-day forecast total.
- Do not present historical calendar-month climate as a future daily forecast or extend LSTM predictions beyond seven days.
- Do not claim exact sowing dates, irrigation availability, plot-level soil compatibility, predicted yield or guaranteed crop success.
- Do not describe an Agricultural Officer's validation of one recommendation record as validation of the entire agronomic model or all crop bands.

Remaining evidence work is independent agronomic review of the six short-horizon bands, local applicability, soil selection/compatibility, and each calendar window. No new values are proposed here.
