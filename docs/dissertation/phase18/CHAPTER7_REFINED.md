# Chapter 7 Evaluation And Discussion Refined Draft

Generated: 2026-10-13

Status: refined draft for supervisor review.

## 7.1 Evaluation Overview

The evaluation separates technical validation, model performance evaluation, scientific agronomic evidence and human-participant evidence. This distinction is essential because the system is technically implemented, but agronomic recommendation validity and user acceptance are not fully verified.

## 7.2 Weather Forecasting Evaluation

The model was evaluated against the historical ERA5 test period. The persistence baseline repeats the last observed weather vector over the seven-day horizon.

| Metric | Baseline | LSTM v1 | Improvement |
| --- | ---: | ---: | ---: |
| Overall MAE | 3.583540 | 3.163312 | 11.73% |
| Overall RMSE | 8.737924 | 6.729412 | 22.99% |

Improvement is calculated as `(Baseline Error - LSTM Error) / Baseline Error * 100`. These are mixed-unit aggregate weather metrics across rainfall, temperature and humidity. They are not crop recommendation accuracy and not independently verified operational forecast accuracy.

## 7.3 Recommendation Evaluation

The recommendation engine is technically validated through automated tests and browser workflows. It produces factor-level explanations, identifies missing evidence and avoids fabricating scores where configuration is incomplete. Scientific agronomic validation remains partial because available crop evidence is often annual, seasonal or qualitative rather than validated for seven-day operational scoring.

## 7.4 Operational Integration Evaluation

Phase 13 demonstrated one controlled operational forecast generation and persistence workflow. The result supports technical feasibility but does not establish long-term predictive accuracy. ERA5 training data and Open-Meteo operational past-days data may have different distributions.

## 7.5 Security, Reliability And Usability Evaluation

Automated tests support role security and fail-closed behavior. Browser E2E supports workflow usability at technical level. Real participant usability and expert validation are not performed.

## 7.6 Threats To Validity

- Historical ERA5 evaluation may not transfer directly to operational Open-Meteo inputs.
- Seven-day forecasts cannot fully represent crop-season suitability.
- Crop thresholds are not fully expert validated for operational scoring.
- Browser E2E uses mocked API responses.
- Real participant UAT and Agricultural Officer expert validation are absent.
- Production readiness is not claimed.

## 7.7 Evaluation Conclusion

The system is technically successful as a prototype and provides transparent evidence for forecasts and recommendations. Scientific and operational claims must remain limited until additional agronomic, expert and field validation is completed.
