# Phase 7B Crop Suitability Engine

## Scope

Phase 7B implements the crop suitability calculation engine only. It does not create recommendations, persist `Recommendation` or `RecommendationCrop` rows, rank crops, implement officer validation, or add Angular UI.

The weather LSTM remains separate. It predicts rainfall, temperature, and humidity only. The suitability engine consumes forecast values plus approved/configured crop requirements and soil compatibility.

Supported crop scope remains exactly:

1. Paddy
2. Maize
3. Green Gram
4. Cowpea
5. Groundnut
6. Chilli

## Engine Boundary

The engine is implemented in the backend application layer under:

`src/backend/AnuradhapuraAI.Application/Suitability/`

It accepts:

- seven forecast days
- optional selected soil type
- one crop profile for each approved crop
- range requirements supplied by the caller/configuration boundary
- soil compatibility entries supplied by the caller/configuration boundary
- suitability factor weights
- suitability category thresholds
- optional climate-risk thresholds

It returns factor-level suitability results, evidence coverage, overall score/category where possible, climate-risk indicators where configured, and a human-readable explanation.

## Forecast Aggregation

Temperature uses the approved Phase 7B aggregation:

```text
Tmean = (T1 + T2 + ... + T7) / 7
```

Humidity uses the 7-day mean only when an approved compatible numeric humidity requirement is supplied.

Rainfall uses the 7-day total only when a supplied requirement explicitly declares compatibility with the 7-day forecast. Annual, seasonal, or growing-period rainfall values are not automatically compared with the 7-day forecast.

## Scoring Formula

For an evaluable numeric factor, Phase 7B uses the approved deterministic piecewise-linear scoring rule:

```text
acceptableMin <= optimalMin <= optimalMax <= acceptableMax

score = 100 inside [optimalMin, optimalMax]

below optimalMin but within acceptable range:
score = 100 * (value - acceptableMin) / (optimalMin - acceptableMin)

above optimalMax but within acceptable range:
score = 100 * (acceptableMax - value) / (acceptableMax - optimalMax)

outside [acceptableMin, acceptableMax]:
score = 0
```

Scores are clamped to `0..100`, and degenerate boundaries are handled safely.

## Weight Normalization

Configured prototype default weights are:

- Rainfall: 25
- Temperature: 25
- Humidity: 25
- Soil: 25

These are project design defaults, not scientifically prescribed agricultural weights.

Unavailable factors are not scored as `0` or `100`. They are excluded from the overall score and the remaining evaluable factor weights are re-normalized:

```text
effectiveWeight_i = configuredWeight_i / sum(configuredWeight of evaluable factors)
overallScore = sum(factorScore_i * effectiveWeight_i)
```

If no factors are evaluable, the engine returns an insufficient-evidence failure instead of inventing an overall score or category.

## Category Thresholds

Approved prototype defaults:

- Highly Suitable: `score >= 80`
- Suitable: `score >= 60 and < 80`
- Moderately Suitable: `score >= 40 and < 60`
- Unsuitable: `score < 40`

These are configurable system-design thresholds, not FAO-prescribed agricultural thresholds.

## Soil Handling

Soil suitability uses the supplied soil type and configured `CompatibilityScore` for the crop/soil pair. A missing soil type or missing configured compatibility entry makes soil not evaluable for that crop.

Phase 7B does not implement automatic soil detection, soil sensors, soil APIs, NPK, organic matter, or irrigation data. Runtime soil acquisition/selection remains a future integration decision.

## Rainfall And Humidity Limitations

Rainfall requirements found in Phase 7A are mostly annual, seasonal, or growing-period values. Phase 7B therefore refuses to score rainfall unless the supplied requirement is explicitly compatible with the 7-day forecast method.

Humidity evidence remains incomplete for the six crops. Phase 7B refuses to score humidity unless a compatible numeric humidity requirement is supplied. Descriptive or growth-stage-specific humidity evidence is not converted into a crop-wide numeric range.

## Climate Risks

Only these approved risks can be emitted:

- Low rainfall
- Heavy rainfall
- High temperature

Risk emission is configuration/evidence driven. If thresholds are not supplied, risks are not fabricated. The `EnableClimateRiskIndicators` flag on the engine request disables all climate-risk output when false.

## Configuration Validation

The engine validates:

- exactly seven daily forecast rows
- chronological forecast target dates without duplicates or gaps
- exactly the six approved crop profiles
- no accidental crop-scope expansion
- non-negative factor weights
- ordered category thresholds
- valid range boundaries
- soil compatibility scores within `0..100`
- duplicate soil compatibility conflicts

Invalid inputs/configuration return controlled failures through the application result contract.

## Recommendation Boundary

The result shape supports later Phase 8 recommendation work by exposing:

- `RainfallScore`
- `TemperatureScore`
- `HumidityScore`
- `SoilScore`
- `OverallScore`
- `SuitabilityCategory`
- `Explanation`

Phase 7B does not implement ranking. Rank and recommendation persistence belong to Phase 8.

## Testing Notes

Automated tests use clearly labelled synthetic fixture values to verify mathematics and behavior. These values are not agricultural recommendations and must not be described as real crop thresholds.
