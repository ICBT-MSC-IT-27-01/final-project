# Phase 15 Temporal Compatibility

Generated: 2026-10-13

## Operational Forecast Basis

The system evaluates recommendations from a seven-day operational forecast:

- Rainfall: seven-day forecast values can be summed for a seven-day total.
- Temperature: seven-day mean can be computed.
- Relative humidity: seven-day mean can be computed.
- Soil: optional user-supplied factor, not forecast-derived.

## Source Time Bases

| Evidence type | Source examples | Original time basis | Direct seven-day scoring status |
| --- | --- | --- | --- |
| EcoCrop rainfall | Crop data sheets | Annual precipitation | Not compatible |
| FAO crop water need | Water-harvesting guidance | Total growing period or growth stage | Not compatible without ET/crop-stage method |
| EcoCrop temperature | Crop ecological descriptors | Climatic/growing-condition range | Conditional; not independently validated as a seven-day rule |
| EcoCrop soil pH/texture/drainage | Crop data sheets | Site/soil property | Requires actual local/user soil input |
| Relative humidity notes | EcoCrop narrative for some crops | Qualitative growing-condition note | Not compatible when non-numeric |

## Rule Applied in Code

The existing `SuitabilityRangeRequirement` has:

- `TimeBasis`
- `IsCompatibleWithSevenDayForecast`

Phase 15 tests prove that annual scientific context values remain unscored when `IsCompatibleWithSevenDayForecast=false`.

## Scientific Decision

No arbitrary conversion was made from:

- annual rainfall to seven-day rainfall
- seasonal crop water need to seven-day rainfall
- crop-cycle water demand to daily or weekly suitability
- qualitative humidity preference to numeric humidity thresholds

This avoids a misleading recommendation score when the evidence does not support the operational time window.
