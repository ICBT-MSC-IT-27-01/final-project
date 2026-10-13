# Phase 15 Forecast Model Evaluation Audit

Generated: 2026-10-13

## Artifact Audited

Forecast model artifacts:

- `src/forecasting-service/artifacts/v1_era5_2010_2025/v1_metadata.json`
- `src/forecasting-service/artifacts/v1_era5_2010_2025/v1_evaluation.json`

Model version:

- `v1`

## Dataset and Split

The metadata records:

- Dataset: `docs/anuradhapura_weather_2010_2025_era5.csv`
- Row count: 5844
- Date range: 2010-01-01 to 2025-12-31
- Features: Rainfall, Temperature, Humidity
- Lookback: 30 days
- Horizon: 7 days
- Chronological train split: 2010-01-01 to 2021-12-31
- Chronological validation split: 2022-01-01 to 2023-12-31
- Chronological test split: 2024-01-01 to 2025-12-31

No random split was identified in the recorded artifact metadata.

## Historical ERA5 Test Metrics

Recorded metrics:

| Metric | Baseline | LSTM v1 |
| --- | ---: | ---: |
| Overall MAE | 3.583540229885058 | 3.1633121592043656 |
| Overall RMSE | 8.737924137150712 | 6.729411662966595 |
| Rainfall MAE | 5.493004926108374 | 4.904241988486377 |
| Rainfall RMSE | 13.91569776673004 | 10.546710224459384 |
| Temperature MAE | 0.7929852216748768 | 0.7071271240535042 |
| Temperature RMSE | 1.0404716626550563 | 0.8912331819204669 |
| Humidity MAE | 4.464630541871921 | 3.878567365073218 |
| Humidity RMSE | 5.858731011408479 | 4.881347236751383 |

Per-horizon metrics are also present in the evaluation artifact for days 1 through 7.

## Interpretation Limits

These metrics are:

- historical ERA5-based weather model metrics
- mixed across rainfall, temperature, and humidity for the overall values
- not crop recommendation accuracy
- not operational Open-Meteo Forecast API accuracy
- not future observed-outcome validation

Predictive accuracy for live operational recommendations remains `NOT VERIFIED`.
