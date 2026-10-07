# Anuradhapura Weather Forecasting Service

Phase 5 builds the Python training pipeline for daily 7-day forecasts of rainfall, daily mean temperature, and humidity for Anuradhapura District.

Phase 6 exposes the approved trained `v1` model through a minimal HTTP inference API. The service predicts weather only; crop suitability and recommendation remain outside this service.

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

The real `v1` training run writes generated artifacts under `artifacts/v1_era5_2010_2025/`. The approved `v1` artifacts are tracked in Git so another developer can run inference without retraining:

- `v1.keras`
- `v1_scaler.joblib`
- `v1_metadata.json`
- `v1_evaluation.json`
- `v1_training_history.json`
- `v1_sample_forecast.json`

Future experimental artifact folders remain ignored unless explicitly approved.

## Phase 6 Inference API

Start the Python inference service from `src/forecasting-service`:

```powershell
python -m uvicorn forecasting_service.api:app --host 127.0.0.1 --port 8000
```

The HTTP API exposes:

- `GET /health`
- `POST /forecast`

`POST /forecast` expects exactly 30 chronological daily observations with no duplicate dates or gaps. Each observation supplies `Date`, `Temperature`, `Rainfall`, and `Humidity`. The final observation date is used as the deterministic forecast date, and targets are `D + 1` through `D + 7`.

Example request shape:

```json
{
  "observations": [
    {
      "Date": "2025-01-01",
      "Temperature": 25.0,
      "Rainfall": 1.2,
      "Humidity": 80.0
    }
  ]
}
```

The response returns model version, forecast date, and exactly seven forecast rows with predicted `Temperature`, `Rainfall`, and `Humidity`.

The service loads the approved model and scaler from `artifacts/v1_era5_2010_2025/`. It does not fit a scaler, train, fine-tune, or replace the approved model.

## Current Runtime Weather Input Limitation

The trained model requires the previous 30 days of actual/observed weather values. The final live recent-weather data provider has not been approved yet. Phase 6 therefore accepts the 30-day sequence as an integration boundary and uses controlled historical data/test fixtures for verification.
