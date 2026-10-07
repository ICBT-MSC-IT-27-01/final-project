# Phase 7A Suitability Specification

## 1. Purpose And Scope

This document prepares the evidence and configuration specification required before implementing the Phase 7 Crop Suitability Engine.

This is **Phase 7A only**. It defines the future suitability-engine contract and evidence status. It does not implement scoring, seed agricultural values, modify schema, create recommendations, or start Phase 7B.

Fixed crops:

1. Paddy
2. Maize
3. Green Gram
4. Cowpea
5. Groundnut
6. Chilli

Approved suitability factors:

- Rainfall
- Temperature
- Humidity
- Soil compatibility

Approved output categories:

- Highly Suitable
- Suitable
- Moderately Suitable
- Unsuitable

Approved climate-risk indicators:

- Low rainfall
- Heavy rainfall
- High temperature

No additional factors, categories, climate risks, crops, or locations are introduced by this specification.

## 2. Current Implementation Constraints Inspected

Inspected domain entities and EF Core configurations:

- `Crop`
- `CropEnvironmentalRequirement`
- `SoilCompatibility`
- `SuitabilityConfiguration`
- `RecommendationCrop`
- `ForecastRecord`

Inspected supporting implementation/docs:

- Phase 6 forecast DTOs and `ForecastRecord`
- Admin configuration API surface
- `AGENTS.md`
- `.agents/skills/anuradhapura-ai-development/`
- `docs/CODEX_HANDOVER.md`
- Phase 6 documentation

Current implementation observations:

- `CropEnvironmentalRequirement` stores one row per `CropId` + `VariableType`, with `MinimumValue`, `MaximumValue`, `Unit`, and `IsActive`.
- The current unique index cannot simultaneously store separate optimal and acceptable ranges for the same crop-variable without a future schema/configuration design decision.
- `SoilCompatibility` stores `CropId`, `SoilType`, `CompatibilityScore`, and `IsActive`.
- `SuitabilityConfiguration` can hold configurable weights and category thresholds through approved configuration keys.
- `RecommendationCrop` already has fields for `RainfallScore`, `TemperatureScore`, `HumidityScore`, `SoilScore`, `OverallScore`, `SuitabilityCategory`, `Explanation`, and `Rank`.
- Phase 6 returns/persists 7 daily forecasts with `ForecastDate`, `TargetDate`, `Rainfall`, `Temperature`, `Humidity`, and `ModelVersion`.

## 3. Evidence Source Priority

Evidence priority for Phase 7B:

1. Sri Lanka Department of Agriculture, FCRDI, RRDI, or equivalent official Sri Lankan agricultural sources.
2. FAO ECOCROP, GAEZ, or equivalent authoritative international agricultural sources.
3. Peer-reviewed scientific literature only where official/authoritative crop sources do not provide the required variable.

Excluded source types:

- blogs
- commercial farming websites
- SEO articles
- unsourced tables
- AI-generated agricultural values

FAO describes ECOCROP as a crop-constraints database containing environmental descriptors such as temperature, annual precipitation, soil pH, and soil characteristics, now accessible through GAEZ after the original database was discontinued ([FAO ECOCROP overview](https://www.fao.org/geospatial/data-and-tools/data-portals/ecocrop/), [FAO land-water ECOCROP page](https://www.fao.org/land-water/resources/tools/databases/ecocrop/en)).

## 4. Environmental Requirement Evidence Matrix

Important: rainfall values below are recorded in their source time basis. ECOCROP rainfall values are annual rainfall. They must not be directly compared to a 7-day forecast until a temporal-alignment method is approved.

### Paddy

| Variable | Minimum | Maximum | Unit | Time Basis | Requirement Type | Source | Source Authority | Evidence Status | Notes |
| --- | ---: | ---: | --- | --- | --- | --- | --- | --- | --- |
| Temperature | 20 | 30 | deg C | Crop climatic range | Optimal/preferred | [FAO ECOCROP Oryza sativa](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=1574) | FAO/ECOCROP | SUPPORTED | Absolute range also listed as 10-36 deg C. |
| Temperature | 10 | 36 | deg C | Crop climatic range | Acceptable/absolute | [FAO ECOCROP Oryza sativa](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=1574) | FAO/ECOCROP | SUPPORTED | Use only if Phase 7B supports optimal vs absolute ranges. |
| Rainfall | 1500 | 2000 | mm | Annual rainfall | Optimal/preferred | [FAO ECOCROP Oryza sativa](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=1574) | FAO/ECOCROP | SUPPORTED WITH TEMPORAL CAVEAT | Annual rainfall, not 7-day rainfall. Irrigated paddy may depend on field water management, not rainfall alone. |
| Rainfall | 1000 | 4000 | mm | Annual rainfall | Acceptable/absolute | [FAO ECOCROP Oryza sativa](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=1574) | FAO/ECOCROP | SUPPORTED WITH TEMPORAL CAVEAT | Do not convert to weekly thresholds without approval. |
| Humidity | Not numeric | Not numeric | Relative humidity | General crop note | Descriptive/preferred | [FAO ECOCROP rice crop view](https://ecocrop.apps.fao.org/ecocrop/srv/en/cropView?id=8143) | FAO/ECOCROP | PARTIALLY SUPPORTED | Source notes rice prefers medium to high humidity, but no crop-wide numeric RH range is provided. RRDI lists Sri Lankan rice temperature/RH studies, but this specification does not extract numeric ranges from abstracts alone. |
| Soil | 5.5 | 7.0 | pH | Soil pH | Optimal/preferred | [FAO ECOCROP Oryza sativa](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=1574) | FAO/ECOCROP | SUPPORTED | ECOCROP also notes wide texture and poorly drained/saturated conditions for wet rice. |
| Soil | 4.5 | 9.0 | pH | Soil pH | Acceptable/absolute | [FAO ECOCROP Oryza sativa](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=1574) | FAO/ECOCROP | SUPPORTED | Sri Lankan RRDI map-based reclamation guidance treats pH 4.5-7.5 as no-lime range for paddy soil management, but this is remedial guidance, not a direct crop suitability range ([RRDI paddy soil reclamation](https://doa.gov.lk/rrdi_technology_soilscience_mapbasedsoilreclamation/)). |

### Maize

| Variable | Minimum | Maximum | Unit | Time Basis | Requirement Type | Source | Source Authority | Evidence Status | Notes |
| --- | ---: | ---: | --- | --- | --- | --- | --- | --- | --- |
| Temperature | 18 | 33 | deg C | Crop climatic range | Optimal/preferred | [FAO ECOCROP Zea mays](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=2175) | FAO/ECOCROP | SUPPORTED | Absolute range also listed as 10-47 deg C. |
| Temperature | 10 | 47 | deg C | Crop climatic range | Acceptable/absolute | [FAO ECOCROP Zea mays](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=2175) | FAO/ECOCROP | SUPPORTED | Requires project approval before storing alongside optimal range. |
| Rainfall | 600 | 1200 | mm | Annual rainfall | Optimal/preferred | [FAO ECOCROP Zea mays](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=2175) | FAO/ECOCROP | SUPPORTED WITH TEMPORAL CAVEAT | Annual rainfall, not 7-day rainfall. |
| Rainfall | 400 | 1800 | mm | Annual rainfall | Acceptable/absolute | [FAO ECOCROP Zea mays](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=2175) | FAO/ECOCROP | SUPPORTED WITH TEMPORAL CAVEAT | Do not convert to weekly thresholds without approval. |
| Humidity | Not numeric | Not numeric | Relative humidity | General crop note | Descriptive/risk note | [FAO ECOCROP sweet maize crop view](https://ecocrop.apps.fao.org/ecocrop/srv/en/cropView?id=10981) | FAO/ECOCROP | PARTIALLY SUPPORTED | Source notes very humid conditions are not considered good for maize, but does not provide numeric RH range for Zea mays. |
| Soil | 5.0 | 7.0 | pH | Soil pH | Optimal/preferred | [FAO ECOCROP Zea mays](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=2175) | FAO/ECOCROP | SUPPORTED | Soil fertility high optimal; depth medium optimal. |
| Soil | 4.5 | 8.5 | pH | Soil pH | Acceptable/absolute | [FAO ECOCROP Zea mays](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=2175) | FAO/ECOCROP | SUPPORTED | Soil texture/drainage details incomplete in ECOCROP data sheet. |

### Green Gram

| Variable | Minimum | Maximum | Unit | Time Basis | Requirement Type | Source | Source Authority | Evidence Status | Notes |
| --- | ---: | ---: | --- | --- | --- | --- | --- | --- | --- |
| Temperature | 21 | 36 | deg C | Crop climatic range | Optimal/preferred | [FAO ECOCROP Vigna radiata](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=2150) | FAO/ECOCROP | SUPPORTED | Absolute range also listed as 8-40 deg C. |
| Temperature | 8 | 40 | deg C | Crop climatic range | Acceptable/absolute | [FAO ECOCROP Vigna radiata](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=2150) | FAO/ECOCROP | SUPPORTED | Use only with approved range model. |
| Rainfall | 650 | 900 | mm | Annual rainfall | Optimal/preferred | [FAO ECOCROP Vigna radiata](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=2150) | FAO/ECOCROP | SUPPORTED WITH TEMPORAL CAVEAT | Annual rainfall, not 7-day rainfall. |
| Rainfall | 500 | 1250 | mm | Annual rainfall | Acceptable/absolute | [FAO ECOCROP Vigna radiata](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=2150) | FAO/ECOCROP | SUPPORTED WITH TEMPORAL CAVEAT | Do not convert to weekly thresholds without approval. |
| Humidity | Not numeric | Not numeric | Relative humidity | General crop note | Descriptive/risk note | [FAO ECOCROP Vigna radiata crop view](https://ecocrop.apps.fao.org/ecocrop/srv/en/cropView?id=2150) | FAO/ECOCROP | PARTIALLY SUPPORTED | Source notes low to medium humidity and that excessive rainfall/humidity at flowering may reduce yields, but provides no numeric RH range. |
| Soil | 5.5 | 6.2 | pH | Soil pH | Optimal/preferred | [FAO ECOCROP Vigna radiata](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=2150) | FAO/ECOCROP | SUPPORTED | Medium/organic texture optimal; well-drained conditions listed. |
| Soil | 4.3 | 8.3 | pH | Soil pH | Acceptable/absolute | [FAO ECOCROP Vigna radiata](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=2150) | FAO/ECOCROP | SUPPORTED | Absolute texture listed as heavy/medium/light. |

### Cowpea

| Variable | Minimum | Maximum | Unit | Time Basis | Requirement Type | Source | Source Authority | Evidence Status | Notes |
| --- | ---: | ---: | --- | --- | --- | --- | --- | --- | --- |
| Temperature | 20 | 30 | deg C | Crop climatic range | Sri Lanka cultivation guidance | [DOA RARDC Kilinochchi crops page](https://doa.gov.lk/fcrdi-rardckilinochchi-crops/) | Sri Lanka DOA/RARDC | SUPPORTED | DOA states cowpea is warm-weather crop grown at 20-30 deg C. ECOCROP provides optimal 20-35 deg C and absolute 10-40 deg C. |
| Temperature | 20 | 35 | deg C | Crop climatic range | Optimal/preferred | [FAO ECOCROP Vigna unguiculata](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=10835) | FAO/ECOCROP | SUPPORTED | Use if broader ECOCROP optimal range is approved over Sri Lankan 20-30 guidance. |
| Temperature | 10 | 40 | deg C | Crop climatic range | Acceptable/absolute | [FAO ECOCROP Vigna unguiculata](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=10835) | FAO/ECOCROP | SUPPORTED | Requires decision on optimal vs acceptable storage. |
| Rainfall | 600 | 1500 | mm | Annual rainfall | Optimal/preferred | [FAO ECOCROP Vigna unguiculata](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=10835) | FAO/ECOCROP | SUPPORTED WITH TEMPORAL CAVEAT | Annual rainfall, not 7-day rainfall. |
| Rainfall | 400 | 4100 | mm | Annual rainfall | Acceptable/absolute | [FAO ECOCROP Vigna unguiculata](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=10835) | FAO/ECOCROP | SUPPORTED WITH TEMPORAL CAVEAT | Do not convert to weekly thresholds without approval. |
| Humidity | Not found | Not found | Relative humidity | Crop-specific RH | Not identified | N/A | NOT YET SUPPORTED | No reliable numeric crop-specific RH range was found in the reviewed authoritative sources. |
| Soil | 4.5 | 8.0 | pH | Soil pH | Sri Lanka cultivation guidance | [DOA RARDC Kilinochchi crops page](https://doa.gov.lk/fcrdi-rardckilinochchi-crops/) | Sri Lanka DOA/RARDC | SUPPORTED | DOA says cowpea can grow from sandy loam to clay and pH 4.5-8.0; well-drained pH 6-7 also stated. |
| Soil | 6.0 | 7.0 | pH | Soil pH | Preferred local soil note | [DOA RARDC Kilinochchi crops page](https://doa.gov.lk/fcrdi-rardckilinochchi-crops/) | Sri Lanka DOA/RARDC | SUPPORTED | DOA notes sandy to clay loam, well-drained, pH 6-7; cowpea sensitive to waterlogging. |
| Soil | 5.5 | 7.5 | pH | Soil pH | Optimal/preferred | [FAO ECOCROP Vigna unguiculata](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=10835) | FAO/ECOCROP | SUPPORTED | ECOCROP is broader than DOA preferred pH note. |

### Groundnut

| Variable | Minimum | Maximum | Unit | Time Basis | Requirement Type | Source | Source Authority | Evidence Status | Notes |
| --- | ---: | ---: | --- | --- | --- | --- | --- | --- | --- |
| Temperature | 22 | 32 | deg C | Crop climatic range | Optimal/preferred | [FAO ECOCROP Arachis hypogaea](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=2199) | FAO/ECOCROP | SUPPORTED | Absolute range also listed as 10-45 deg C. |
| Temperature | 10 | 45 | deg C | Crop climatic range | Acceptable/absolute | [FAO ECOCROP Arachis hypogaea](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=2199) | FAO/ECOCROP | SUPPORTED | Use only with approved optimal/absolute design. |
| Rainfall | 600 | 1500 | mm | Annual rainfall | Optimal/preferred | [FAO ECOCROP Arachis hypogaea](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=2199) | FAO/ECOCROP | SUPPORTED WITH TEMPORAL CAVEAT | Annual rainfall, not 7-day rainfall. |
| Rainfall | 400 | 4000 | mm | Annual rainfall | Acceptable/absolute | [FAO ECOCROP Arachis hypogaea](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=2199) | FAO/ECOCROP | SUPPORTED WITH TEMPORAL CAVEAT | Do not convert to weekly thresholds without approval. |
| Humidity | Not found | Not found | Relative humidity | Crop-specific RH | Not identified | N/A | NOT YET SUPPORTED | No reliable numeric crop-specific RH range was found in the reviewed authoritative sources. |
| Soil | 6.5 | 7.0 | pH | Soil pH | Local ideal note | [DOA RARDC Kilinochchi crops page](https://doa.gov.lk/fcrdi-rardckilinochchi-crops/) | Sri Lanka DOA/RARDC | SUPPORTED | DOA says deep, well-drained sandy loam/clay loam, pH 6.5-7.0, high fertility ideal. |
| Soil | 5.5 | 6.5 | pH | Soil pH | Optimal/preferred | [FAO ECOCROP Arachis hypogaea](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=2199) | FAO/ECOCROP | SUPPORTED | ECOCROP optimal pH differs from DOA ideal pH note. |
| Soil | 4.5 | 8.5 | pH | Soil pH | Acceptable/absolute | [FAO ECOCROP Arachis hypogaea](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=2199) | FAO/ECOCROP | SUPPORTED | ECOCROP lists well-drained soil. |

### Chilli

| Variable | Minimum | Maximum | Unit | Time Basis | Requirement Type | Source | Source Authority | Evidence Status | Notes |
| --- | ---: | ---: | --- | --- | --- | --- | --- | --- | --- |
| Temperature | 17 | 30 | deg C | Crop climatic range | Optimal/preferred | [FAO ECOCROP Capsicum annuum](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=618) | FAO/ECOCROP | SUPPORTED | Absolute range also listed as 8-35 deg C. |
| Temperature | 8 | 35 | deg C | Crop climatic range | Acceptable/absolute | [FAO ECOCROP Capsicum annuum](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=618) | FAO/ECOCROP | SUPPORTED | Use only with approved optimal/absolute design. |
| Rainfall | 600 | 1250 | mm | Annual rainfall | Optimal/preferred | [FAO ECOCROP Capsicum annuum](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=618) | FAO/ECOCROP | SUPPORTED WITH TEMPORAL CAVEAT | Annual rainfall, not 7-day rainfall. |
| Rainfall | 500 | 1700 | mm | Annual rainfall | Acceptable/absolute | [FAO ECOCROP Capsicum annuum](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=618) | FAO/ECOCROP | SUPPORTED WITH TEMPORAL CAVEAT | Do not convert to weekly thresholds without approval. |
| Humidity | Not numeric | Not numeric | Relative humidity | General crop note | Descriptive/preferred | [FAO ECOCROP Capsicum annuum crop view](https://ecocrop.apps.fao.org/ecocrop/srv/en/cropView?id=618) | FAO/ECOCROP | PARTIALLY SUPPORTED | Source notes moderate to high humidity, and low humidity with high temperatures may reduce fruit set, but no numeric RH range is provided. |
| Soil | 5.5 | 6.8 | pH | Soil pH | Optimal/preferred | [FAO ECOCROP Capsicum annuum](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=618) | FAO/ECOCROP | SUPPORTED | ECOCROP lists medium/organic soil texture and well-drained conditions. |
| Soil | 4.5 | 7.0 | pH | Soil pH | Acceptable/absolute | [FAO ECOCROP Capsicum annuum](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=618) | FAO/ECOCROP | SUPPORTED | Use only with approved optimal/absolute design. |

## 5. Humidity Evidence Status

The project design includes humidity suitability, but reviewed authoritative crop sources do not provide complete numeric crop-specific relative-humidity ranges for all six crops.

| Crop | Evidence Status | Evidence Summary | Phase 7B Implication |
| --- | --- | --- | --- |
| Paddy | PARTIALLY SUPPORTED | ECOCROP notes medium to high humidity preference; RRDI lists Sri Lankan rice RH suitability research, but no numeric crop-wide RH range was extracted from an accessible requirement table. | Do not seed numeric RH range without approved source. |
| Maize | PARTIALLY SUPPORTED | ECOCROP sweet maize note says very humid conditions are not considered good; no numeric range found. | Treat as unresolved for numeric scoring. |
| Green Gram | PARTIALLY SUPPORTED | ECOCROP notes low to medium humidity and excess rainfall/humidity at flowering can reduce yields; no numeric range found. | Stage-specific flowering risk may be relevant but cannot become a single crop-wide range without approval. |
| Cowpea | NOT YET SUPPORTED | No reliable numeric or descriptive RH requirement found in reviewed authoritative sources. | Do not score cowpea humidity numerically yet. |
| Groundnut | NOT YET SUPPORTED | No reliable numeric or descriptive RH requirement found in reviewed authoritative sources. | Do not score groundnut humidity numerically yet. |
| Chilli | PARTIALLY SUPPORTED | ECOCROP notes moderate to high humidity and low humidity plus high temperature may reduce fruit set; no numeric range found. | May require stage-specific or descriptive treatment, pending approval. |

## 6. Rainfall Temporal-Alignment Issue

The approved LSTM predicts rainfall for the next 7 days. Most crop requirement sources reviewed here provide rainfall as annual rainfall. Some agronomic sources may also provide seasonal or growing-period water needs. These values are not equivalent to a 7-day forecast.

Do not:

- compare annual rainfall directly with 7-day rainfall
- divide annual rainfall by 52 without scientific justification
- invent weekly rainfall thresholds
- silently convert seasonal rainfall into weekly rainfall

Candidate alignment options for project-owner decision:

1. **7-day rainfall risk only; defer rainfall suitability**
   - Use 7-day forecast rainfall only for approved climate-risk indicators such as low rainfall/heavy rainfall once risk thresholds are approved.
   - Rainfall suitability score remains pending until an approved growing-period water method exists.
   - Most conservative option.

2. **Use season/growing-period rainfall requirements with observed season-to-date rainfall plus 7-day forecast**
   - Aggregate observed rainfall from the current crop season/growing period, add 7-day forecast, and compare against a crop-specific growing-period requirement if sourced.
   - Requires approved season definitions, planting date/reference date, and observed rainfall source.

3. **Use water-balance or crop-water method**
   - Use FAO-style crop water requirement logic, e.g., evapotranspiration and effective rainfall, if project scope later approves it.
   - More scientifically defensible, but likely beyond current MSc prototype unless carefully bounded.

4. **Use configurable rainfall rule entered by Administrator**
   - Admin enters approved 7-day or stage-specific rainfall thresholds from an authoritative source.
   - Requires source evidence before entry; no automatic conversion from annual rainfall.

No option is frozen by this document.

## 7. Forecast Aggregation Candidates

The Phase 6 forecast produces seven daily rows with rainfall, temperature, and humidity.

Candidate aggregation rules:

| Factor | Candidate Aggregation | Evidence/Justification | Status |
| --- | --- | --- | --- |
| Temperature | 7-day mean temperature | Many crop climate tables provide general temperature ranges, so a 7-day mean may approximate near-term expected condition. | PROJECT DESIGN DECISION REQUIRED |
| Temperature | Daily threshold check/count of days outside acceptable range | Better for high-temperature climate-risk flag and acute stress. | PROJECT DESIGN DECISION REQUIRED |
| Humidity | 7-day mean relative humidity | Possible only where numeric humidity ranges are approved. Current evidence is incomplete. | NOT READY |
| Humidity | Descriptive/stage-specific flag | May fit crops where humidity evidence is descriptive or stage-based, e.g., flowering/fruit set notes. | PROJECT DESIGN DECISION REQUIRED |
| Rainfall | 7-day total rainfall | Natural aggregation of daily rainfall forecast, but compatible only with approved 7-day or short-period requirements. | PROJECT DESIGN DECISION REQUIRED |
| Rainfall | Season-to-date + 7-day forecast | More defensible if crop rainfall requirements are seasonal/growing-period. | REQUIRES DATA SOURCE/PLANTING CONTEXT |

## 8. Proposed Configurable Scoring Design

No final weights or thresholds are approved by this document.

Future Phase 7B output fields:

- `RainfallScore`
- `TemperatureScore`
- `HumidityScore`
- `SoilScore`
- `OverallScore`
- `SuitabilityCategory`
- `Explanation`

Weights must come from `SuitabilityConfiguration`. Category thresholds must come from configuration/database values.

### Range-Based Factor Scoring Pseudocode

This method is proposed for variables with approved optimal and acceptable ranges:

```text
function score(value, optimalMin, optimalMax, absoluteMin, absoluteMax):
    if value is missing:
        return NotScored or configured fallback, pending approval

    if optimalMin <= value <= optimalMax:
        return 100

    if value < absoluteMin or value > absoluteMax:
        return 0

    if absoluteMin <= value < optimalMin:
        return interpolate linearly from 0 at absoluteMin to 100 at optimalMin

    if optimalMax < value <= absoluteMax:
        return interpolate linearly from 100 at optimalMax to 0 at absoluteMax
```

If only a single acceptable range is approved and no optimal range exists, Phase 7B must either:

- treat the acceptable range as the scoring plateau with no degradation band, or
- require an Administrator-approved optimal range before scoring.

This decision is pending approval.

### Overall Score Pseudocode

```text
weightedScore =
    rainfallScore * rainfallWeight
  + temperatureScore * temperatureWeight
  + humidityScore * humidityWeight
  + soilScore * soilWeight

overallScore = weightedScore / sum(activeWeights)
```

Rules required before implementation:

- How to handle unscored humidity if evidence is missing.
- Whether weights for unscored factors are redistributed or produce a pending/incomplete result.
- Whether soil compatibility score is 0-100 or another configured scale.

## 9. Category Threshold Structure

Approved categories:

- Highly Suitable
- Suitable
- Moderately Suitable
- Unsuitable

Proposed configurable keys:

- `HighlySuitableMinimum`
- `SuitableMinimum`
- `ModeratelySuitableMinimum`

No numeric boundary values are approved in this document. If no active threshold configuration exists during Phase 7B, the engine should fail predictably rather than invent thresholds.

## 10. Climate-Risk Rule Design

Approved climate risks only:

- Low rainfall
- Heavy rainfall
- High temperature

Potential derivation:

- Low rainfall: compare approved rainfall aggregation against crop rainfall lower bound or approved risk threshold.
- Heavy rainfall: compare approved rainfall aggregation against crop rainfall upper bound or approved risk threshold.
- High temperature: compare 7-day mean and/or any daily forecast temperature against approved crop temperature upper bound or approved risk threshold.

No numeric climate-risk thresholds are approved by this document. Prefer linking risks to approved crop environmental requirements/configuration rather than unrelated constants.

## 11. Soil Compatibility Design

The system already has `SoilCompatibility`, but the runtime soil-data acquisition/selection method is unresolved.

Separate concepts:

### A. Crop Soil Preference Evidence

Crop soil evidence can include:

- soil texture/type
- drainage preference
- pH evidence

Examples from reviewed sources:

- Cowpea: DOA notes sandy loam to clay, pH 4.5-8.0; well-drained sandy to clay loam pH 6-7 and sensitivity to waterlogging.
- Groundnut: DOA notes well-drained sandy loam or clay loam; deep well-drained pH 6.5-7.0 ideal.
- Paddy: RRDI provides paddy soil pH/EC/organic matter reclamation guidance for Sri Lankan paddy fields, while ECOCROP lists wet-rice soil pH and saturated drainage characteristics.

### B. Runtime Soil Selection Method

Still unresolved:

- How a public/registered user selects soil type.
- Whether soil is selected manually, defaulted by area, or later linked to an approved soil map.
- Whether Anuradhapura-specific paddy soil fertility maps can be used for paddy only.

Do not implement NPK, organic matter, sensors, or new soil factors in Phase 7B unless approved.

## 12. Explanation Design

Future recommendation explanations should be human-readable and factor-level.

Example structure:

- rainfall condition compared with approved crop requirement
- temperature condition compared with approved crop requirement
- humidity condition compared with approved crop requirement or marked as not scored/pending evidence
- soil compatibility contribution
- overall suitability result
- applicable approved climate risks

Explanations should state when evidence/configuration is missing instead of hiding the gap.

## 13. Decisions Required Before Phase 7B

### Supported By Evidence

- The six approved crops are seeded and fixed in the current database design.
- ECOCROP supports temperature, annual rainfall, soil pH, and selected soil descriptors for all six crops.
- Sri Lankan DOA/RARDC supports cowpea temperature and soil notes, and groundnut soil notes.
- Sri Lankan RRDI/DOA supports paddy soil management context and paddy soil fertility map availability.
- Phase 6 forecast output can provide 7 daily values for rainfall, temperature, and humidity.

### Project Design Decision Required

1. Rainfall 7-day temporal alignment method.
2. Whether humidity is scored if numeric ranges are missing for some crops.
3. Whether stage-specific humidity evidence may be used and how growth stage is known.
4. Runtime soil acquisition/selection method.
5. Final factor weights.
6. Final suitability-category thresholds.
7. How to store/use both optimal and acceptable ranges when the current schema supports one row per crop-variable.
8. Whether the current `CropEnvironmentalRequirement` schema is sufficient for Phase 7B or whether an approved non-schema workaround/configuration pattern is required.
9. Forecast aggregation rules for temperature, humidity, and rainfall.
10. How to handle incomplete evidence for a factor: fail, omit, redistribute weights, or mark recommendation as incomplete.

## 14. Explicit Non-Implementation Statement

This document does not:

- implement scoring services
- seed agricultural numeric values
- modify database records
- add migrations
- implement recommendation generation
- implement crop ranking
- implement officer validation
- implement Angular UI
- start Phase 7B
- start Phase 8

## 15. Phase 7B Schema Compatibility Correction

The approved runtime mapping keeps the existing one-row-per-crop-variable design.

`CropEnvironmentalRequirement.MinimumValue` and `CropEnvironmentalRequirement.MaximumValue` represent the optimal/preferred range.

The following metadata is required for runtime scoring:

- `AcceptableMinimumValue`: acceptable/absolute lower boundary, nullable.
- `AcceptableMaximumValue`: acceptable/absolute upper boundary, nullable.
- `TimeBasis`: temporal basis of the requirement, nullable.
- `IsCompatibleWithSevenDayForecast`: explicit permission to use the requirement directly with the 7-day forecast, default `false`.

Supported time-basis labels are:

- `Daily`
- `SevenDay`
- `GrowingPeriod`
- `Seasonal`
- `Annual`

The engine must not infer compatibility from `Unit` or from the existence of a requirement row. Annual, seasonal, and growing-period rainfall requirements are not automatically converted to seven-day scoring thresholds.

If an acceptable boundary or time basis is missing, the factor is not evaluable. Missing or non-compatible factors are excluded from overall-score calculation and the remaining evaluable factor weights are re-normalized.
