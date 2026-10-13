# Phase 15 Scientific Crop Requirements

Generated: 2026-10-13

Project: AI-Driven Weather Forecasting and Crop Recommendation System for Anuradhapura District

## Evidence Classification

Phase 15 uses these evidence classes:

- `VERIFIED`: directly supported by an accessible source and compatible with the scoring interpretation.
- `CONDITIONAL`: source-supported, but dependent on crop stage, season, location, management, or time basis.
- `UNVERIFIED`: plausible or existing project value without sufficient traceable evidence.
- `MISSING`: no defensible value identified.
- `TEST_ONLY`: synthetic fixture used only for deterministic software tests.

## Scientific Source Matrix

The main source used for crop requirement values is FAO EcoCrop, accessed through the FAO EcoCrop portal. FAO describes EcoCrop as a crop constraints database with environmental descriptors including temperature, annual precipitation, pH, climate zone and soil characteristics.

FAO/IIASA GAEZ and FAO water-requirement guidance were used as supporting context, not as direct seven-day scoring thresholds.

| Crop | Scientific name | Source | Temperature evidence | Rainfall/water evidence | Soil evidence | Humidity evidence | Phase 15 classification |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Paddy | `Oryza sativa` | FAO EcoCrop data sheet | Optimal 20-30 deg C; absolute 10-36 deg C | Annual rainfall optimal 1500-2000 mm; absolute 1000-4000 mm | pH 5.5-7 optimal; poorly drained/saturated wet-rice soil context | Not numeric | CONDITIONAL |
| Maize | `Zea mays ssp. mays` | FAO EcoCrop data sheet; FAO/IIASA GAEZ crop profile | EcoCrop optimal 18-33 deg C; GAEZ describes optimum growth around 20-30 deg C | EcoCrop annual rainfall optimal 600-1200 mm; GAEZ notes annual 500-2000 mm depending on cultivar/environment | pH 5-7 optimal; well-drained soils | Qualitative note that very humid conditions are not good | CONDITIONAL |
| Green Gram | `Vigna radiata` | FAO EcoCrop data sheet | Optimal 21-36 deg C; absolute 8-40 deg C | Annual rainfall optimal 650-900 mm; absolute 500-1250 mm | pH 5.5-6.2 optimal; well-drained soils | Qualitative low-to-medium humidity preference; no numeric range | CONDITIONAL |
| Cowpea | `Vigna unguiculata ssp. unguiculata` | FAO EcoCrop data sheet | Optimal 20-35 deg C; absolute 10-40 deg C | Annual rainfall optimal 600-1500 mm; absolute 400-4100 mm | pH 5.5-7.5 optimal; well-drained soils | Missing numeric evidence | CONDITIONAL |
| Groundnut | `Arachis hypogaea` | FAO EcoCrop data sheet; FAO water guidance | Optimal 22-32 deg C; absolute 10-45 deg C | EcoCrop annual rainfall optimal 600-1500 mm; FAO water guidance lists seasonal crop water need 500-700 mm | pH 5.5-6.5 optimal; well-drained soils | Missing numeric evidence | CONDITIONAL |
| Chilli | `Capsicum annuum` | FAO EcoCrop data sheet; FAO water guidance for peppers | Optimal 17-30 deg C; absolute 8-35 deg C | Annual rainfall optimal 600-1250 mm; absolute 500-1700 mm | pH 5.5-6.8 optimal; well-drained soils | Missing numeric evidence | CONDITIONAL |

## Accepted Requirement Values

No source-derived numeric crop requirement was activated as a live seven-day scoring threshold in Phase 15.

Reason:

- FAO EcoCrop rainfall values are explicitly annual rainfall values.
- FAO crop water values are seasonal or growth-stage water requirements, not seven-day rainfall suitability thresholds.
- Temperature ranges describe crop ecological suitability or growth conditions, not necessarily a validated seven-day forecast decision rule.
- Relative humidity was qualitative or missing for most crops.
- Soil values are useful for admin configuration but require a supported local soil-selection workflow before they should affect public recommendations.

## Rejected or Context-Only Values

The following values were documented but not activated for operational scoring:

- Annual rainfall ranges from EcoCrop.
- Seasonal crop water need values from FAO water-harvesting guidance.
- General crop-cycle lengths.
- Qualitative humidity descriptions.
- Generic soil pH and texture preferences without local soil input.

## Test-Only Values

Automated tests use synthetic ranges labelled as synthetic 7-day fixtures. These are `TEST_ONLY` and must not be copied into production-like agronomic configuration.

## Current Scientific Status

Scientific validation is `PARTIAL`.

The project has traceable contextual crop requirement evidence, but not a fully validated set of seven-day operational scoring thresholds for all six crops.
