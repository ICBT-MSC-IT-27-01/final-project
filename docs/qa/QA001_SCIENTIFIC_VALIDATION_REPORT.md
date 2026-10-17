# QA-001 Scientific Validation Report

**Date:** 17 October 2026
**Scope:** Anuradhapura District; Paddy, Maize, Green Gram, Cowpea, Groundnut and Chilli only.
**Decision:** Full seven-day crop suitability scoring is not scientifically supported by the repository evidence. QA-001 remains **PARTIAL**.

## Method and time-scale decision

The operational LSTM produces seven daily values for rainfall, temperature and relative humidity. A requirement is evaluated only when its unit, aggregation and time basis explicitly fit that output. Annual/seasonal rainfall values were not divided, scaled or otherwise converted to seven-day values. Optional soil is not inferred from location or made mandatory.

The approved partial output is therefore a **seven-day mean air-temperature compatibility indicator**, not a crop-suitability score, category, or recommendation. It is shown with `PARTIALLY_ASSESSED`, no overall score, no suitability category and no comparable crop rank.

## Evidence by crop

| Crop | Temperature evidence usable as partial indicator | Rainfall evidence | Humidity evidence | Soil evidence | Result |
|---|---|---|---|---|---|
| Paddy | FAO ECOCROP *Oryza sativa*: optimal 20–30 deg C; acceptable 10–36 deg C. Seven-day mean temperature only. | 1,500–2,000 mm optimal; 1,000–4,000 mm acceptable, **annual**. Not evaluated. | No crop-specific seven-day threshold. | No validated selected-soil compatibility. | PARTIALLY_ASSESSED |
| Maize | FAO ECOCROP *Zea mays ssp. mays*: 18–33 deg C; 10–47 deg C. Seven-day mean only. | 600–1,200 mm; 400–1,800 mm, **annual**. Not evaluated. | No seven-day threshold. | No validated selected-soil compatibility. | PARTIALLY_ASSESSED |
| Green Gram | FAO ECOCROP *Vigna radiata*: 21–36 deg C; 8–40 deg C. Seven-day mean only. | 650–900 mm; 500–1,250 mm, **annual**. Not evaluated. | No seven-day threshold. | No validated selected-soil compatibility. | PARTIALLY_ASSESSED |
| Cowpea | FAO ECOCROP *Vigna unguiculata ssp. unguiculata*: 20–35 deg C; 10–40 deg C. Seven-day mean only. | 600–1,500 mm; 400–4,100 mm, **annual**. Not evaluated. | No seven-day threshold. | No validated selected-soil compatibility. | PARTIALLY_ASSESSED |
| Groundnut | FAO ECOCROP *Arachis hypogaea*: 22–32 deg C; 10–45 deg C. Seven-day mean only. | 600–1,500 mm; 400–4,000 mm, **annual**. Not evaluated. | No seven-day threshold. | No validated selected-soil compatibility. | PARTIALLY_ASSESSED |
| Chilli | FAO ECOCROP *Capsicum annuum*: 17–30 deg C; 8–35 deg C. Seven-day mean only. | 600–1,250 mm; 500–1,700 mm, **annual**. Not evaluated. | No seven-day threshold. | No validated selected-soil compatibility. | PARTIALLY_ASSESSED |

## Exact sources and applicability

- Temperature and rainfall values: [FAO ECOCROP Oryza sativa](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=1574), [Zea mays ssp. mays](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=238663), [Vigna radiata](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=2150), [Vigna unguiculata ssp. unguiculata](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=10835), [Arachis hypogaea](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=2199), and [Capsicum annuum](https://ecocrop.apps.fao.org/ecocrop/srv/en/dataSheet?id=618). The exact structured records and units are retained in [`crop-evidence.json`](../agriculture/crop-evidence.json). Temperature is `RESEARCH_SUPPORTED_NOT_LOCALLY_VALIDATED`; it supports a short-horizon indicator only. Rainfall is `INCOMPATIBLE_TEMPORAL_SCALE` because its unit is annual millimetres.
- Crop calendars: Sri Lankan Department of Agriculture / AFACI records referenced in [`SIX_CROP_SCIENTIFIC_EVIDENCE_MATRIX.md`](../agriculture/SIX_CROP_SCIENTIFIC_EVIDENCE_MATRIX.md). They support conditional seasonal calendar windows, not seven-day weather suitability scoring.
- Provenance and prior decision: [`CROP_REQUIREMENT_PROVENANCE.md`](../agriculture/CROP_REQUIREMENT_PROVENANCE.md) and [`QA001_RECOMMENDATION_FIX_RESULTS.md`](QA001_RECOMMENDATION_FIX_RESULTS.md).
- Chapters 1–4 identifies rainfall, temperature, humidity, soil and crop requirements as relevant factors, but provides no crop-specific, local, seven-day-compatible thresholds or validated weights.

## Supported and unsupported calculations

**Supported now**

- Mean of the seven forecast daily air temperatures versus each retained FAO temperature range.
- Factor-level explanation, source link, unit, time basis, evidence status and limitation.
- Explicitly unavailable rainfall, humidity and soil factors.

**Not supported now**

- Seven-day rainfall scoring from annual rainfall requirements.
- Relative-humidity scoring for any of the six crops.
- Soil scoring without a validated user-selected soil compatibility record.
- Missing-factor renormalisation into an overall score when the missing factors are mandatory for full assessment.
- Suitability categories, cross-crop rankings, climate-risk thresholds, or a claim that a partial temperature indicator is a crop recommendation.

## Expert / supervisor decisions required

1. Supply or approve crop-specific rainfall criteria expressed for the same seven-day aggregation used by the forecast, including source, local applicability, crop stage and interpretation rule for all six crops.
2. Supply or approve crop-specific seven-day relative-humidity criteria with the same metadata for all six crops, or formally remove humidity from the full-assessment method.
3. Approve validated soil compatibility records for named user-selected soil types, or formally confirm soil remains optional and define the scientifically valid full-assessment rule without it.
4. Approve factor weights and category thresholds only after decisions 1–3 make a complete assessment scientifically valid.
5. Obtain Agricultural Officer/local agronomist validation of the resulting method before changing QA-001 to PASS.

## Implementation consequence

No recommendation-engine code, feature flag, database configuration, model artifact or LSTM training was changed. The current `PARTIALLY_ASSESSED` presentation is the maximum defensible result under the available evidence.
