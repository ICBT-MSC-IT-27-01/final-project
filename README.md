# AI-Driven Weather Forecasting and Crop Recommendation System

This repository contains the MSc prototype for Anuradhapura District. Development follows `.agents/skills/anuradhapura-ai-development/references/project-specification.md`.

## Phase

Current approved status: **Phase 6 - Backend Forecast Integration complete**.

Next planned phase: **Phase 7 - Suitability Engine**. Do not start Phase 7 without project-owner approval.

## Local Commands

```powershell
dotnet build src/backend/AnuradhapuraAI.slnx
cd src/frontend
npm install
npm start
cd ../forecasting-service
python -m pytest
```

The frontend and Python commands require dependencies to be installed first.

## Phase 6 Forecast Integration

The approved Phase 5 `v1` model artifacts are tracked under `src/forecasting-service/artifacts/v1_era5_2010_2025/`.

Start the Python forecasting service from `src/forecasting-service`:

```powershell
python -m uvicorn forecasting_service.api:app --host 127.0.0.1 --port 8000
```

Configure the backend with:

- `FeatureFlags:EnableWeatherModelIntegration`
- `ForecastingService:BaseUrl`

The backend forecast endpoint is `POST /api/forecasts` and requires exactly 30 chronological observed-weather rows. Live recent-weather acquisition is not yet approved; do not add a provider without approval.

## Local JWT Secret

Phase 3 uses JWT bearer authentication. Do not commit real signing keys. For local backend work, set a development key with user secrets or an environment variable:

```powershell
cd src/backend/AnuradhapuraAI.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "<local-development-key-at-least-32-characters>"
```
