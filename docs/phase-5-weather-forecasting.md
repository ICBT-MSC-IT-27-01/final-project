# Phase 5 Weather Forecasting Training

## Objective

Build a reproducible Python training pipeline for Anuradhapura District daily weather forecasting. The approved targets are rainfall, daily mean temperature, and humidity for the next 7 days.

## Dataset

Phase 5B used the approved ERA5-based CSV at `docs/anuradhapura_weather_2010_2025_era5.csv`. The dataset represents a single Anuradhapura representative point for the MSc prototype; it should not be claimed to capture every microclimate across the district.

The verified schema is `Date`, `Rainfall`, `Temperature`, and `Humidity`. The confirmed period is 2010-01-01 to 2025-12-31 with 5,844 daily observations.

## Data Quality Findings

Profiling found 0 missing values for rainfall, temperature, and humidity; 0 duplicate dates; 0 non-numeric values; and complete daily continuity across the approved period. Descriptive statistics were:

- Rainfall: mean 3.817 mm, std 8.840, min 0.000, max 171.800
- Temperature: mean 27.211 C, std 1.588, min 21.200, max 31.300
- Humidity: mean 79.828%, std 6.579, min 57.000, max 99.000

## Preprocessing

The pipeline preserves genuine weather extremes and only marks impossible or malformed values as missing. No values required interpolation in the Phase 5B run. The configured maximum isolated interpolation gap remains 3 days for future datasets.

## Split and Scaling

The split is strictly date-based and chronological:

- Training: 2010-01-01 to 2021-12-31, 4,383 rows
- Validation: 2022-01-01 to 2023-12-31, 730 rows
- Test: 2024-01-01 to 2025-12-31, 731 rows

`MinMaxScaler` is fitted only on the training period and reused for validation and test data. Validation/test inputs may use immediately preceding historical rows for the 30-day lookback, but target dates remain inside their approved split.

## Model Design

The Phase 5B model is one multivariate, multi-output LSTM using rainfall, temperature, and humidity. It consumes inputs shaped `(samples, 30, 3)` and directly predicts outputs shaped `(samples, 7, 3)`.

Configuration: 1 LSTM layer, 64 units, dropout 0.2, Adam optimizer, MSE loss, batch size 32, learning rate 0.001, maximum 100 epochs, early stopping patience 10, seed 42.

## Baseline and Evaluation

The baseline repeats the last observed weather vector across all 7 forecast days. Evaluation uses MAE and RMSE only.

Untouched test-period results:

- Persistence baseline: overall MAE 3.584, overall RMSE 8.738
- LSTM: overall MAE 3.163, overall RMSE 6.729
- LSTM per variable: Rainfall MAE 4.904 / RMSE 10.547, Temperature MAE 0.707 / RMSE 0.891, Humidity MAE 3.879 / RMSE 4.881

Rainfall remains the most difficult target and should be discussed carefully in dissertation analysis.

## Artifacts

The real Phase 5B run saved:

- Model: `src/forecasting-service/artifacts/v1_era5_2010_2025/v1.keras`
- Scaler: `src/forecasting-service/artifacts/v1_era5_2010_2025/v1_scaler.joblib`
- Metadata: `src/forecasting-service/artifacts/v1_era5_2010_2025/v1_metadata.json`
- Evaluation: `src/forecasting-service/artifacts/v1_era5_2010_2025/v1_evaluation.json`
- Training history: `src/forecasting-service/artifacts/v1_era5_2010_2025/v1_training_history.json`

Generated model and preprocessing artifacts are ignored by Git.

## Limitations

The model is an initial MSc prototype baseline comparison, not a production weather service. Results are from ERA5 representative-point data only, and perfect reproducibility is not guaranteed across hardware/runtime environments because TensorFlow numerical kernels can vary slightly.
