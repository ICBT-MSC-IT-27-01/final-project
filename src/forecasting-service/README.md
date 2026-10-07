# Anuradhapura Weather Forecasting Service

Phase 5 builds the Python training pipeline for daily 7-day forecasts of rainfall, daily mean temperature, and humidity for Anuradhapura District.

## Dataset Status

Phase 5B uses the approved ERA5-based dataset at `../../docs/anuradhapura_weather_2010_2025_era5.csv`. It covers 2010-01-01 to 2025-12-31 with 5,844 daily observations for a single representative Anuradhapura point.

## Expected Input Schema

Preferred daily CSV columns:

```text
Date,Rainfall,Temperature,Humidity
```

`Date` is one daily observation. `Temperature` is daily mean temperature. If a source provides minimum and maximum temperature only, map those fields through the ingestion layer and derive daily mean as `(min + max) / 2`.

## Pipeline

1. Ingest CSV through an explicit column mapping.
2. Profile raw data before modification.
3. Clean malformed/impossible values without removing genuine extremes.
4. Interpolate only small isolated missing gaps, using configurable `max_interpolation_gap_days`.
5. Split chronologically by the approved fixed dates.
6. Fit `MinMaxFeatureScaler` on training data only.
7. Create 30-day lookback windows to predict the next 7 days directly.
8. Train a multivariate multi-output LSTM when TensorFlow is installed and an approved dataset exists.
9. Compare against a simple persistence baseline using MAE and RMSE.
10. Save `.keras` model, preprocessing artifacts, and JSON metadata only for real approved training runs.

## Anti-Leakage Strategy

Chronological splitting is performed before training-dependent scaling. The scaler is fitted only on the training split and reused for validation, test, and future inference. Test data is not used for model selection.

Approved split:

- Training: 2010-01-01 to 2021-12-31
- Validation: 2022-01-01 to 2023-12-31
- Test: 2024-01-01 to 2025-12-31

## Configuration

`TrainingConfig` controls lookback, horizon, split ratios, interpolation gap, LSTM units/layers, dropout, batch size, epochs, learning rate, early stopping patience, seed, output path, and model version.

The initial technical defaults are:

- lookback: 30 days
- forecast horizon: 7 days
- direct multi-step output
- features: Rainfall, Temperature, Humidity
- loss: mean squared error
- optimizer: Adam

These are starting parameters, not claims of optimality.

## Phase 5B Artifacts

The real `v1` training run writes generated artifacts under `artifacts/v1_era5_2010_2025/`. These files are ignored by Git and include the `.keras` model, fitted scaler, evaluation JSON, metadata JSON, training history, and a labelled historical test sample forecast.
