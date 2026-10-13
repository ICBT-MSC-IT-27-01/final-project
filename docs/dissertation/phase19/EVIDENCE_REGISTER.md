# Phase 19 Evidence Register

Generated: 2026-10-13

| Claim | Evidence | Status |
| --- | --- | --- |
| ERA5-derived historical weather data | v1_metadata.json | Supported |
| LSTM improves over persistence baseline | v1_evaluation.json | Supported: MAE 11.73%, RMSE 22.99% |
| Open-Meteo operational past-days adapter | OpenMeteoForecastPastDaysWeatherSource.cs | Technically supported |
| ERA5/Open-Meteo equivalence | No compatibility study | Not verified |
| Real participant UAT | Phase 16-18 reports | Not performed |
| Agricultural Officer expert validation | Phase 15-18 reports | Not performed |

## Metric Verification

- Baseline overall MAE: 3.583540229885058
- LSTM overall MAE: 3.1633121592043656
- MAE improvement: 11.73%
- Baseline overall RMSE: 8.737924137150712
- LSTM overall RMSE: 6.729411662966595
- RMSE improvement: 22.99%

These are historical ERA5 mixed-unit weather metrics, not operational forecast accuracy or crop recommendation accuracy.
