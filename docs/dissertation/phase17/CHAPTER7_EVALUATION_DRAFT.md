# Chapter 7 Evaluation Draft

Generated: 2026-10-13

Status: proposed draft material.

## 7.1 Evaluation Overview

The evaluation considers technical correctness, model performance, recommendation evidence quality, usability evidence, security, reliability and research limitations. It separates automated technical validation from scientific agronomic validation and human-participant evaluation.

## 7.2 Weather Forecasting Performance

The LSTM was evaluated on the historical ERA5 test period 2024-2025. The persistence baseline repeats the last observed weather vector across the seven-day horizon.

| Metric | Baseline | LSTM v1 | Improvement |
| --- | ---: | ---: | ---: |
| Overall MAE | 3.583540 | 3.163312 | 11.73% |
| Overall RMSE | 8.737924 | 6.729412 | 22.99% |

These overall values combine rainfall, temperature and humidity in different units, so per-variable interpretation is important. The results are historical ERA5 weather metrics only; they are not operational forecast accuracy and not crop recommendation accuracy.

## 7.3 Suitability Scoring Evaluation

The suitability engine passed automated tests for scoring boundaries, deterministic ranking, evidence coverage, missing factors and insufficient-evidence states. The evaluation supports technical correctness of the scoring implementation, not full agronomic validity.

## 7.4 Scientific Evidence Coverage

FAO EcoCrop and related sources provide contextual crop requirement evidence. However, many values are annual, seasonal or qualitative. They were not converted into operational seven-day thresholds without approval. Scientific agronomic validation is therefore partial.

## 7.5 Operational Forecast Integration

Phase 13 verified one controlled operational forecast generation and persistence flow. This proves technical integration, but not long-term operational accuracy. Open-Meteo Forecast API past-days data may differ from ERA5 training data, creating a source distribution-shift threat.

## 7.6 Recommendation System Evaluation

The recommendation system provides explainable factor-level results, ranks the approved six crops where evidence is sufficient and clearly marks missing evidence. The public read-only endpoint avoids unintended recommendation-history writes.

## 7.7 System Usability Evidence

Browser E2E tests demonstrate that core workflows are reachable in desktop and mobile Chrome. Real participant usability testing was not performed, so user satisfaction and field usability remain unverified.

## 7.8 Security And Reliability Evaluation

Role boundaries, protected routes, invalid login and feature-flag behavior are covered by automated tests. Reliability evidence includes controlled failure messages and fail-closed operational generation in E2E. No aggressive load or penetration testing was performed.

## 7.9 Threats To Validity

- ERA5 historical training data and Open-Meteo operational inputs are not proven equivalent.
- Seven-day weather is not the same as full crop-season suitability.
- Crop thresholds are not fully expert-validated for the operational scoring window.
- Browser E2E uses mocked API fixtures.
- UAT and expert validation are not performed.
- Production deployment was not verified.

## 7.10 Research Question Assessment

The project demonstrates that an LSTM weather forecast can be integrated with an explainable crop recommendation prototype. It also demonstrates that technical validation can be automated across backend, frontend and forecasting-service boundaries. The research cannot yet claim agronomic effectiveness or production suitability without further expert and field validation.
