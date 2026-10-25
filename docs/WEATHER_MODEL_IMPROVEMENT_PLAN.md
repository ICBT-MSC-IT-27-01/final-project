# Weather Forecasting Model Improvement Plan

**Project:** AI-Driven Weather Forecasting and Crop Recommendation System for Anuradhapura District  
**Date:** 22 October 2026  
**Status:** Proposed plan — no model or production artefact is changed by this document.

## 1. Purpose and boundaries

This plan improves the existing Python LSTM weather forecasting component in small, reproducible steps. It is based on the available project reference set in `docs/source of truth`, `docs/MSc_Project_Documents_PRD_SRS_Architecture`, and `docs/MSc_Project_PRD_SRS_System_Design_Separate_Files`.

The work remains within the currently documented forecasting scope:

- Anuradhapura District representative-point weather data.
- Inputs and outputs: daily rainfall, mean temperature, and mean relative humidity.
- A 30-day historical input sequence and direct seven-day forecast.
- An LSTM-based Python model with chronological training, validation, and test periods.
- Evaluation using MAE and RMSE, with a persistence baseline.
- Clear separation between weather prediction and the explainable crop-suitability engine.

The plan does not add crops, weather variables, agricultural factors, live data providers, new evaluation metrics, or a new model family. Those changes require explicit project-owner and, where relevant, supervisor approval.

## 2. Current baseline

The approved `v1` model uses the ERA5-based dataset covering 2010-01-01 to 2025-12-31 (5,844 daily observations). It uses a chronological split: training 2010–2021, validation 2022–2023, and untouched test period 2024–2025.

| Measure | Persistence baseline | LSTM v1 |
| --- | ---: | ---: |
| Overall MAE | 3.584 | 3.163 |
| Overall RMSE | 8.738 | 6.729 |
| Rainfall MAE | 5.493 | 4.904 |
| Rainfall RMSE | 13.916 | 10.547 |
| Temperature MAE | 0.793 | 0.707 |
| Humidity MAE | 4.465 | 3.879 |

The LSTM improves the recorded overall historical test metrics, but rainfall is the most difficult output. These are historical ERA5-based model metrics, not live operational accuracy or crop-recommendation accuracy.

## 3. Improvement roadmap: small to big

### Stage 0 — Freeze the baseline and prepare experiment controls

**Goal:** Make every comparison fair and reproducible.

1. Preserve `v1` model, scaler, metadata, evaluation, and training-history files unchanged.
2. Add an experiment register that records experiment ID, Git commit, dataset fingerprint, fixed date boundaries, random seed, configuration, validation metrics, final test metrics, and artefact locations.
3. Ensure each experiment writes into a new versioned artefact folder; no experiment may overwrite `v1`.
4. Add a regression check that fails if the split dates, feature order, seven-day horizon, or training-only scaler fitting changes accidentally.

**Exit criteria:** A fresh baseline rerun produces saved metadata and metric reports that can be compared with the approved `v1` run.

### Stage 1 — Diagnose before changing the model

**Goal:** Identify the source of forecast error without changing the model.

1. Produce MAE and RMSE by variable and forecast day (day 1 through day 7) for the existing LSTM and persistence baseline.
2. Inspect a small sample of best, typical, and worst historical forecast windows, especially rainfall events.
3. Check whether errors are concentrated in particular calendar months or seasonal periods; report only descriptive findings from the held-out results.
4. Verify that rainfall values remain non-negative after inverse scaling and that all output dates align with actual target dates.

**Exit criteria:** A concise diagnostic report identifies a specific, evidence-based improvement target. No configuration is changed until this report is reviewed.

### Stage 2 — Low-risk configuration tuning

**Goal:** Improve validation performance while retaining the approved LSTM approach.

Run a small, predefined experiment matrix. Change one dimension at a time first; then combine only the strongest settings:

| Configuration area | Candidate values |
| --- | --- |
| Lookback window | 14, 30, 60, 90 days |
| LSTM units | 32, 64, 128 |
| LSTM layers | 1, 2 |
| Dropout | 0.0, 0.1, 0.2, 0.3 |
| Learning rate | 0.0005, 0.001, 0.002 |
| Batch size | 16, 32, 64 |

Use the validation period only to choose configurations. Keep epochs, early stopping, seed strategy, chronological date boundaries, features, horizon, and persistence comparator fixed unless a later approved experiment explicitly changes one.

**Decision rule:** Select a candidate only if it improves validation MAE and/or RMSE without a material deterioration in the other metric. Document the trade-off rather than relying on one aggregate value.

**Exit criteria:** One selected tuned candidate and its complete experiment record are ready for a single untouched-test evaluation.

### Stage 3 — Rainfall-preprocessing experiment

**Goal:** Reduce the influence of rare extreme rainfall values while preserving real extremes.

1. Compare the current rainfall scaling with a `log1p` rainfall transformation followed by scaling.
2. Apply the inverse transformation before calculating MAE and RMSE and before inference results are returned.
3. Keep temperature and humidity preprocessing unchanged for this isolated experiment.
4. Confirm that zero rainfall is handled correctly and no negative rainfall appears after inversion.

This remains an LSTM/preprocessing experiment; it does not add a new weather variable or model family. It should proceed only after Stage 2 establishes a clear configuration baseline.

**Exit criteria:** The validation report demonstrates whether the transform improves rainfall and overall errors. Keep it only if it passes the same selection rule as Stage 2.

### Stage 4 — Loss-function weighting decision (approval gate)

**Goal:** Consider whether the three forecasted variables should contribute differently during LSTM training.

The current model uses mean squared error across rainfall, temperature, and humidity. A weighted loss could be tested to reduce domination by one variable after scaling; however, changing the loss design is a methodological change. Before implementation, obtain explicit approval to test and document a weighted LSTM loss.

If approved, compare the ordinary and weighted losses on validation data under the winning Stage 2/3 configuration. Retain MAE/RMSE as the only reported evaluation metrics.

**Exit criteria:** An approved decision with evidence for keeping the ordinary loss or adopting a documented weighted loss.

### Stage 5 — Final candidate confirmation

**Goal:** Perform one honest test evaluation of the chosen model.

1. Lock the winning configuration based only on earlier validation results.
2. Train once using the defined training process and generate predictions for the untouched 2024–2025 test period.
3. Compare the candidate with the persistence baseline using overall, per-variable, and per-horizon-day MAE/RMSE.
4. Report both improvements and regressions. Do not retry configurations because of the test result.
5. Save model, scaler, metadata, evaluation, training history, sample forecasts, and a short model card under a new version (for example, `v2_experiment_...`).

**Exit criteria:** A reproducible final-comparison report with no fabricated claims of improvement.

### Stage 6 — Integration, regression testing, and controlled promotion

**Goal:** Safely make a demonstrably better model available to the existing service.

1. Confirm prediction API compatibility: exactly 30 approved input observations and seven dated output rows for rainfall, temperature, and humidity.
2. Run Python pipeline, inference API, backend integration, and relevant frontend tests.
3. Preserve the current approved model as a rollback option.
4. Change the active version only through explicit configuration/artefact selection, not by silently replacing `v1` files.
5. Update the README, model metadata, model-evaluation document, and dissertation evidence with actual results and limitations.

**Exit criteria:** The selected model is reproducible, passes technical regression checks, and can be reverted to `v1`.

## 4. Order of execution

1. Stage 0: baseline freeze and experiment register.
2. Stage 1: diagnostics and error analysis.
3. Stage 2: small configuration tuning study.
4. Stage 3: isolated rainfall-transform study, if Stage 2 does not sufficiently address rainfall error.
5. Stage 4: request approval before any weighted-loss experiment.
6. Stage 5: evaluate only the selected candidate once on the untouched test set.
7. Stage 6: integrate and document only if results justify promotion.

## 5. Guardrails and stop conditions

- Never use random train/test splitting or fit preprocessing on validation/test data.
- Do not report a test metric during configuration selection.
- Do not replace or overwrite `v1` artefacts.
- Do not claim operational/live accuracy from historical ERA5 evaluation.
- Do not use the weather model to generate crop-suitability weights or agricultural decisions directly.
- Stop and request approval before adding variables, changing the model family, adding metrics, adopting external providers, or changing the documented seven-day horizon.
- If no candidate clearly improves validation performance, retain `v1` and document the negative result.

## 6. First implementation package

The practical first package is deliberately small:

1. Create experiment configuration and result records.
2. Add per-variable/per-horizon diagnostic output and tests.
3. Rerun the frozen baseline to validate reproducibility.
4. Run the first four lookback experiments: 14, 30, 60, and 90 days, with all other settings equal to `v1`.
5. Review validation results before expanding the tuning matrix.

This sequence produces defensible evidence quickly while keeping the model, scope, and dissertation claims controlled.

## 7. Source documents consulted

- `docs/source of truth/PROJECT_SOURCE_OF_TRUTH_v1.md`
- `docs/MSc_Project_Documents_PRD_SRS_Architecture/01_Requirements/PROJECT_SOURCE_OF_TRUTH_v1.md`
- `docs/MSc_Project_PRD_SRS_System_Design_Separate_Files/01_PRD/PRD_Product_Requirements.docx`
- `docs/MSc_Project_PRD_SRS_System_Design_Separate_Files/02_SRS/SRS_Software_Requirements.docx`
- `docs/phase-5-weather-forecasting.md`
- `docs/PHASE15_FORECAST_MODEL_EVALUATION.md`
- `src/forecasting-service/artifacts/v1_era5_2010_2025/v1_evaluation.json`

**Document limitation:** The locally available source-of-truth baseline says the original signed-off proposal was not found in the supplied archive. This plan therefore relies on the available working PRD/SRS/SDD materials and recorded model evidence; it must be reconciled if an original supervisor-approved proposal is later located.
