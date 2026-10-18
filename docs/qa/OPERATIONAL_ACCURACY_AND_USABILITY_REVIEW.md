# Operational accuracy and plain-language usability review

Date: 18 October 2026. Scope: public forecast and six-crop recommendation for Anuradhapura District. This record separates completed technical checks from validation that needs external evidence or participants.

## Completed technical review

- The latest API forecast and read-only crop evaluation referenced the same persisted seven-day run (`b9d8598f-650d-451d-96b6-2f282e036b57`, origin 16 October; target dates 17–23 October). Its [raw public API response](forecast-run-2026-10-16.json) was saved before a newer run could replace it. Each paired daily rainfall, temperature and humidity value matched exactly.
- **Issuance-time finding:** the saved run was created at 21:08:02 Sri Lanka time on 17 October. Its first target date is 17 October, so the first target day had already begun before the forecast was issued. The run cannot support a claim of seven complete forward calendar days, even after later observations arrive. The public forecast and recommendation pages now warn about this timing, and the operational comparison tool withholds full-run accuracy metrics. A newly issued run must start with a future local calendar day to support that claim.
- The seven-day rainfall total was 63.44 mm; mean temperature 26.67 °C; mean relative humidity 87.06%. Each returned weighted crop score recalculated from its factor scores and effective weights. All six returned `Unsuitable`. This verifies calculation and display consistency, not environmental truth.
- The numeric crop bands and weights are the project-owner-approved strict seven-day configuration in [the Phase 3 matrix](PHASE3_SUITABILITY_CONFIGURATION_TEMPLATE.md). An Agricultural Officer already completed the documented safe application-workflow review in [QA-001](QA001_SCIENTIFIC_VALIDATION_REPORT.md). Neither fact establishes observed crop outcomes.
- The [Department of Agriculture crop calendars](https://doa.gov.lk/naicc-publications-crop-calender/) support seasonal context, not the numeric seven-day bands. [Open-Meteo historical documentation](https://open-meteo.com/en/docs/historical-weather-api) identifies ERA5 as reanalysis, not local station measurement. The historical v1 model metrics in [Phase 15](../PHASE15_FORECAST_MODEL_EVALUATION.md) must not be presented as live operational accuracy.
- A later real-world spot check found an [official Anuradhapura three-hourly observation](official-anuradhapura-spot-observation-2026-10-18.json) dated 18 October 2026 at 14:30 local: 33.3 °C and 63% relative humidity. The app predicts a **daily mean** of 26.58 °C and 87.61% for 18 October. An afternoon station reading and a modelled daily mean have different time and space bases, so their numerical difference is **not** a forecast error or validation result. The source's rainfall fields have no demonstrated midnight-to-midnight meaning and were not compared.
- NOAA's [Synoptic Summary of the Day](https://www.ncei.noaa.gov/products/global-historical-climatology-network-hourly/synoptic-summary-of-the-day) uses 00–23 UTC and warns that synoptic rainfall may be underreported. The Sri Lankan Meteorology site labels its 24-hour rainfall period as ending at 08:30 local. Neither daily total can be paired directly with this application's local calendar-day rainfall without aligned hourly measurements.
- The public UI was reviewed for unclear score meaning, soil handling, expired periods, broken evidence links and mobile reachability. It now states the strict 0/100 factor rule, shows the three aggregate weather values, labels unassessed factors, and warns when every crop is unsuitable. Automated browser tests are a technical accessibility/usability check, not a study with intended users.
- Verification after these changes: Angular production build passed; 44 Angular unit tests, 14 desktop/mobile browser tests and five operational-validation tests passed on 18 October 2026. No database schema, API contract or feature flag changed.

## Independent forecast-outcome check

Status: **PENDING COMPLETE DAILY DATA**. The current run's full seven-day period has not yet ended. The official spot observation above cannot establish a daily mean or full-day rainfall total, and no complete set of local daily station observations is available for this run. Do not publish MAE/RMSE from partial outcomes or substitute another forecast as ground truth.

When observations are available, record station/dataset identity, coordinates, measurement method, local timezone, units, and the origin plus all seven target dates. Save the persisted forecast JSON and run the [operational validation command](../../src/forecasting-service/README.md#operational-forecast-outcome-check). It refuses to calculate metrics until all eight observation dates are present **and** confirms that each target date was still fully in the future when the run was issued. It then reports rainfall, temperature and humidity MAE/RMSE against both the LSTM forecast and a persistence baseline. A single complete run supports only a single-run result; repeat across multiple non-overlapping origins before making a general skill claim. Keep raw station data outside the repository if private.

## Intended-user comprehension study

Status: **PENDING PARTICIPANTS**. No nontechnical users were recruited or represented as having participated. A facilitator can use the current application without coaching the participant through these tasks:

1. Find the date range and say whether the forecast is still current.
2. Find the seven-day rainfall total and explain whether it is a prediction or a measurement.
3. Check the six crops with soil unknown. State whether any crop is suitable and whether the first-listed crop should be planted solely because it is first.
4. Explain what a factor score of 0 or 100 means and whether an overall score is a harvest-success probability.
5. Enter an unrecognised soil type and identify that soil was not assessed.
6. Find the source and limitations of a crop result, then return to the main result.

For each participant, record the task, completion without help (yes/no), any wrong decision, time or hesitation, words the participant misunderstood, device and assistive technology if relevant, and a suggested change. Use a participant code rather than a name. Prioritise any misunderstanding that could turn an `Unsuitable` result into a planting decision; revise the UI and repeat that task. Do not mark this study complete until actual participant records exist.

## Agronomic outcome review

Status: **PENDING OUTCOME EVIDENCE**. The existing Officer workflow review validates that a recommendation was reviewable and recorded. A separate agronomic reviewer should compare the seven-day rule assumptions with local crop stage, rainfed/irrigated conditions and observed cultivation outcomes before anyone claims field-level predictive suitability. Record the reviewer, date, crop, source observations, applicable conditions, decision and limitations. No new factors, thresholds or crops should be applied without the project owner's approval.
