# Weather Model Improvement — Stages 0 to 3 Results

**Project:** AI-Driven Weather Forecasting and Crop Recommendation System for Anuradhapura District  
**Run date:** 22 October 2026  
**Status:** Validation-only experiment results; no production model promotion has occurred.

## Scope retained

The completed work retained the approved weather-model scope:

- inputs/outputs: rainfall, mean temperature, and humidity;
- LSTM model family;
- direct seven-day forecast horizon;
- chronological training (2010–2021), validation (2022–2023), and untouched test (2024–2025) periods;
- MAE and RMSE with the persistence baseline; and
- separation from crop-suitability logic.

The active `v1` artefacts were not changed.

## Stage 0 — Reproducibility controls

Implemented:

- versioned experiment-output directories;
- an append-only JSONL experiment register that records configuration, Git commit, dataset SHA-256 fingerprint, evaluation split, metrics, and artefact directory;
- explicit validation-only evaluation mode, preventing test metrics from being used to select a candidate; and
- a stable dataset-fingerprint check and rainfall-transform round-trip test.

The experiment dataset fingerprint was:

`368d0a212bfe1493d49412a9cfbb7aa7b15b1d833823f82c576eaa5d535ada3e`

Generated local artefacts are in `src/forecasting-service/artifacts/model-improvement/` and remain ignored by Git. The register is `artifacts/model-improvement/experiments/experiment_register.jsonl`.

## Stage 1 — Diagnostics

All candidate evaluations record allowed MAE/RMSE diagnostics by weather variable and forecast horizon day. The 60-day candidate's validation evidence shows:

| Validation diagnostic | Persistence | 60-day LSTM |
| --- | ---: | ---: |
| Overall MAE | 3.106 | 2.762 |
| Overall RMSE | 6.026 | 4.772 |
| Rainfall MAE | 4.705 | 4.288 |
| Rainfall RMSE | 9.100 | 7.041 |
| Temperature MAE | 0.720 | 0.640 |
| Temperature RMSE | 0.945 | 0.815 |
| Humidity MAE | 3.894 | 3.359 |
| Humidity RMSE | 5.025 | 4.251 |

Forecast error rises across the horizon: the selected LSTM's combined MAE is 2.309 on day 1 and 2.982 on day 7. Rainfall remains the largest-error output and is therefore the focus of the isolated Stage 3 test.

## Stage 2 — Lookback study

All runs used the same configuration as `v1` except for lookback length. They used only the validation period for candidate selection.

| Candidate | Lookback | Validation MAE | Validation RMSE | Decision |
| --- | ---: | ---: | ---: | --- |
| `lookback_14d_validation` | 14 days | 2.793 | 4.817 | Not selected |
| `lookback_30d_validation` | 30 days | 2.811 | 4.785 | Not selected |
| `lookback_60d_validation` | 60 days | **2.762** | **4.772** | Selected for next gate |
| `lookback_90d_validation` | 90 days | 2.772 | 4.772 | Not selected |

The 60-day LSTM has the lowest validation MAE and RMSE of this predefined experiment set. It also improves on the validation persistence baseline. This is not a test-period or operational-accuracy claim.

## Stage 3 — Rainfall transformation study

The selected 60-day configuration was rerun with `log1p` rainfall transformation before scaling and inverse transformation before evaluation. Temperature and humidity preprocessing were unchanged.

| Candidate | Rainfall transform | Validation MAE | Validation RMSE | Decision |
| --- | --- | ---: | ---: | --- |
| 60-day LSTM | None | 2.762 | **4.772** | Retain |
| 60-day LSTM | `log1p` | **2.574** | 4.982 | Discard |

The transform lowered MAE but worsened RMSE by 0.210. Per the approved plan's decision rule, it is discarded because it materially degrades the other allowed error measure.

## Current selected candidate

`lookback_60d_validation` is the current selected candidate for the next approval gate. It uses:

- 60-day lookback;
- one LSTM layer with 64 units;
- dropout 0.2;
- Adam learning rate 0.001;
- batch size 32;
- maximum 100 epochs with early-stopping patience 10;
- untransformed rainfall and training-only MinMax scaling; and
- the existing three weather variables and seven-day output.

## Next step — approval required

Stage 4 proposes testing a weighted LSTM loss. This is a methodological change and has **not** been implemented. Explicit project-owner approval is required before it can proceed.

If approval is not granted, the next permitted action is Stage 5: train the already selected 60-day configuration once and evaluate it once on the untouched 2024–2025 test period. That result must be reported as a final comparison and must not be used for further tuning.
