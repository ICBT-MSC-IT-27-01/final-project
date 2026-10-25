# External review package

Prepared 25 October 2026. This is a review aid, not an approval record. No reviewer name, signature, agronomic decision or supervisor approval has been entered.

## Part A — Independent agronomic review

### Evidence for review

- Current implementation and constraints: [`SCIENTIFIC_EVIDENCE_GAPS.md`](SCIENTIFIC_EVIDENCE_GAPS.md).
- Versioned owner-approved configuration: `docs/agriculture/crop-evidence.json` and `docs/qa/PHASE3_SUITABILITY_CONFIGURATION_TEMPLATE.md`.
- Earlier independent evidence review: `docs/agriculture/SIX_CROP_SCIENTIFIC_EVIDENCE_MATRIX.md`.
- Calendar extraction and current approval fields: `docs/English calendar images/evidance json/DOA_SIX_CROP_CALENDAR_MONTH_EXTRACTION_REVIEW.json`.
- Runtime technical evidence and caveats: [`QA_EXECUTION_LOG.md`](QA_EXECUTION_LOG.md) and `evidence/live-api-summary.json`.

The current scoring prototype compares seven-day forecast rainfall totals and seven-day mean temperature/humidity against the owner-approved bands below. A factor inside a band gets 100 and outside gets 0. Soil is optional and omitted unless configured for a crop. The live technical run returned six complete assessments, all `Unsuitable`; this is not evidence the bands predict agronomic success. The DoA calendar entries currently say `agronomistValidated=false` and `productionApproved=false`.

| Crop | Rainfall, 7-day total | Temperature, 7-day mean | Relative humidity, 7-day mean | Calendar scope needing review |
| --- | --- | --- | --- | --- |
| Paddy | 35–50 mm | 25–35 °C | 70–80% | Dry/Intermediate Zone applicability; irrigation versus rainfed and local zone context |
| Maize | 30–45 mm | 25–32 °C | 50–70% | Dry Zone; November response currently gives climate context only |
| Green Gram | 20–35 mm | 28–30 °C | 60–70% | Dry Zone and local seasonal conditions |
| Cowpea | 20–35 mm | 27–35 °C | 50–70% | Dry Zone and local seasonal conditions |
| Groundnut | 25–40 mm | 24–33 °C | 50–70% | Calendar references Anuradhapura DL-1d; applicability to other district subregions |
| Chilli | 25–40 mm | 20–30 °C | 50–65% | Dry Zone and local seasonal conditions |

### Reviewer decisions

For each factor and crop, please mark **Approve**, **Revise**, or **Insufficient evidence**. If revising, provide an authoritative source, relevant crop stage, geographic scope, unit, time basis, and limitations. Please review whether a seven-day weather window supports this use at all. Review calendar windows separately from the numerical suitability scores.

| Crop / issue | Decision | Source or rationale | Reviewer initials/date |
| --- | --- | --- | --- |
| Paddy rainfall, temperature, humidity, and calendar context | Pending |  |  |
| Maize rainfall, temperature, humidity, and calendar context | Pending |  |  |
| Green Gram rainfall, temperature, humidity, and calendar context | Pending |  |  |
| Cowpea rainfall, temperature, humidity, and calendar context | Pending |  |  |
| Groundnut rainfall, temperature, humidity, and DL-1d scope | Pending |  |  |
| Chilli rainfall, temperature, humidity, and calendar context | Pending |  |  |
| Optional soil compatibility method and evidence | Pending |  |  |
| Use of 7-day weather values for suitability scoring | Pending |  |  |

Reviewer name/qualification: ____________________  Date: __________
Overall disposition: **Pending** / Approved for prototype use / Approved for production use / Revise / Insufficient evidence
Conditions and limitations: ______________________________________________________________

## Part B — Project supervisor diagram and dissertation review

### Files to inspect

Each diagram has editable Mermaid source (`.mmd`) and a PNG export in [`CORRECTED_DIAGRAMS/`](CORRECTED_DIAGRAMS/):

1. `01-system-context`
2. `02-level-0-data-flow`
3. `03-level-1-data-flow`
4. `04-use-cases`
5. `05-authentication-sequence`
6. `06-lstm-workflow`
7. `07-suitability-workflow`
8. `08-deployment-and-outputs`

The per-figure comparison and reasons are in [`DIAGRAM_MISMATCH_REGISTER.md`](DIAGRAM_MISMATCH_REGISTER.md). The original photographed diagrams under `docs/diagrms/` are preserved. No dissertation DOCX was edited.

### Review checklist

- [ ] The diagrams distinguish the seven-day LSTM forecast, date-range calendar candidates, and crop-based candidate cultivation periods.
- [ ] Seasonal planning is not represented as an LSTM prediction.
- [ ] Suitability includes an insufficient-evidence path and does not imply that a rank means suitable.
- [ ] User roles and server-side authorization boundaries match the implementation.
- [ ] Model dimensions, chronological split, and train-only scaler fitting match the implementation.
- [ ] Captions and Chapter 3/4 wording distinguish implemented, technically tested, and scientifically validated claims.
- [ ] Approved final figures and any required caption changes are identified.

Supervisor name: ____________________  Date: __________
Disposition: **Pending** / Approved / Revise
Required revisions or caption wording: ___________________________________________________

## Review boundary

Technical status is summarized in [`IMPLEMENTATION_STATUS_MATRIX.md`](IMPLEMENTATION_STATUS_MATRIX.md). A supervisor's diagram approval does not constitute agronomic validation. An agronomist's review of one crop or recommendation does not automatically validate other crops, the entire model, or all calendar windows.
