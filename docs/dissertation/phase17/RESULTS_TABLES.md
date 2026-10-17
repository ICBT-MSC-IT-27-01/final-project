# Phase 17 Results Tables

Generated: 2026-10-13

## Dataset Characteristics

| Item | Value | Evidence |
| --- | ---: | --- |
| Dataset | ERA5-derived Anuradhapura daily weather | `v1_metadata.json` |
| Date range | 2010-01-01 to 2025-12-31 | `v1_metadata.json` |
| Row count | 5,844 | `v1_metadata.json` |
| Features | Rainfall, Temperature, Humidity | `v1_metadata.json` |
| Missing values | 0 for all features | `v1_metadata.json` |
| Duplicate dates | 0 | `v1_metadata.json` |
| Training period | 2010-01-01 to 2021-12-31 | `v1_metadata.json` |
| Validation period | 2022-01-01 to 2023-12-31 | `v1_metadata.json` |
| Test period | 2024-01-01 to 2025-12-31 | `v1_metadata.json` |

## LSTM Model Architecture

| Item | Value |
| --- | --- |
| Input shape | 30 days x 3 features |
| Forecast horizon | 7 days |
| Output shape | 7 days x 3 features |
| LSTM units | 64 |
| Dropout | 0.2 |
| Optimizer | Adam |
| Learning rate | 0.001 |
| Batch size | 32 |
| Maximum epochs | 100 |
| Early stopping patience | 10 |
| Seed | 42 |

## Historical ERA5 Weather Model Metrics

| Metric | Persistence baseline | LSTM v1 | Improvement |
| --- | ---: | ---: | ---: |
| Overall MAE | 3.583540 | 3.163312 | 11.73% |
| Overall RMSE | 8.737924 | 6.729412 | 22.99% |
| Rainfall MAE | 5.493005 | 4.904242 | 10.72% |
| Rainfall RMSE | 13.915698 | 10.546710 | 24.21% |
| Temperature MAE | 0.792985 | 0.707127 | 10.83% |
| Temperature RMSE | 1.040472 | 0.891233 | 14.34% |
| Humidity MAE | 4.464631 | 3.878567 | 13.13% |
| Humidity RMSE | 5.858731 | 4.881347 | 16.68% |

Formula: `(Baseline - LSTM) / Baseline * 100`. These are historical ERA5 weather metrics. They are not crop recommendation accuracy and not verified operational forecast accuracy.

## Suitability Boundaries

| Score range | Category |
| ---: | --- |
| >= 80 | Highly Suitable |
| >= 60 and < 80 | Suitable |
| >= 40 and < 60 | Moderately Suitable |
| < 40 | Unsuitable |

## Supported Crops

| Crop | Status |
| --- | --- |
| Paddy | Approved |
| Maize | Approved |
| Green Gram | Approved |
| Cowpea | Approved |
| Groundnut | Approved |
| Chilli | Approved |

## Current Regression Results

| Area | Command | Result |
| --- | --- | --- |
| Backend build | `dotnet build .\src\backend\AnuradhapuraAI.Api\AnuradhapuraAI.Api.csproj --no-restore` | PASS, 0 warnings, 0 errors |
| Backend tests | `dotnet test .\tests\AnuradhapuraAI.Phase3Tests\AnuradhapuraAI.Phase3Tests.csproj --no-restore --logger "console;verbosity=minimal"` | PASS, 281 passed, 0 failed, 0 skipped |
| Python tests | `python -m pytest` from `src/forecasting-service` | PASS, 22 passed, 1 upstream Starlette/httpx deprecation warning |
| Angular tests | `npm test -- --watch=false` from `src/frontend` | PASS, 9 files, 39 tests passed |
| Angular build | `npm run build` from `src/frontend` | PASS |
| Browser E2E | `npm run e2e` from `src/frontend` | PASS, 14 passed, 0 failed, 19.2 seconds |
