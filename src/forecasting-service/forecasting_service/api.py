from __future__ import annotations

from datetime import date
import math
from typing import Annotated

from fastapi import Depends, FastAPI, HTTPException, Request
from fastapi.responses import HTMLResponse, JSONResponse
from pydantic import AliasChoices, BaseModel, ConfigDict, Field, field_validator

from forecasting_service.inference.predictor import (
    ForecastArtifactError,
    ForecastValidationError,
    WeatherForecastPredictor,
    WeatherObservation,
)


app = FastAPI(title="Anuradhapura Forecasting Service", version="0.1.0")


@app.exception_handler(ForecastArtifactError)
async def artifact_error_handler(request: Request, exc: ForecastArtifactError) -> JSONResponse:
    return JSONResponse(status_code=503, content={"detail": str(exc)})


@app.exception_handler(ForecastValidationError)
async def validation_error_handler(request: Request, exc: ForecastValidationError) -> JSONResponse:
    return JSONResponse(status_code=400, content={"detail": str(exc)})


class ObservationRequest(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    observation_date: date = Field(validation_alias=AliasChoices("Date", "date"))
    temperature: float = Field(validation_alias=AliasChoices("Temperature", "temperature"))
    rainfall: float = Field(validation_alias=AliasChoices("Rainfall", "rainfall"))
    humidity: float = Field(validation_alias=AliasChoices("Humidity", "humidity"))

    @field_validator("temperature", "rainfall", "humidity")
    @classmethod
    def finite_weather_value(cls, value: float) -> float:
        if not math.isfinite(value):
            raise ValueError("Weather values must be finite numbers.")
        return value


class ForecastRequest(BaseModel):
    observations: Annotated[list[ObservationRequest], Field(min_length=1)]


class ForecastResponseItem(BaseModel):
    forecastDate: date
    targetDate: date
    temperature: float
    rainfall: float
    humidity: float
    modelVersion: str


class ForecastResponse(BaseModel):
    modelVersion: str
    forecastDate: date
    forecasts: list[ForecastResponseItem]


_predictor: WeatherForecastPredictor | None = None


def get_predictor() -> WeatherForecastPredictor:
    global _predictor
    if _predictor is None:
        _predictor = WeatherForecastPredictor()
    return _predictor


@app.get("/health")
def health(predictor: WeatherForecastPredictor = Depends(get_predictor)) -> dict[str, str]:
    return {"status": "Healthy", "modelVersion": predictor.model_version}


@app.get("/health-ui", response_class=HTMLResponse)
def health_ui() -> str:
    return """<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Anuradhapura Python LSTM Service Health</title><style>
body{align-items:center;background:#f0fdf4;color:#14532d;display:flex;font-family:Arial,sans-serif;justify-content:center;margin:0;min-height:100vh}
main{background:#fff;border:1px solid #bbf7d0;border-radius:14px;box-shadow:0 10px 25px #14532d22;max-width:560px;padding:32px;width:calc(100% - 32px)}
h1{color:#166534;margin-top:0}.status{border-radius:8px;font-weight:bold;margin:18px 0;padding:14px}.checking{background:#fef3c7;color:#92400e}.healthy{background:#dcfce7;color:#166534}.unhealthy{background:#fee2e2;color:#991b1b}
code{background:#f0fdf4;border-radius:4px;padding:2px 5px}.detail{color:#475569}a{color:#15803d}
</style></head><body><main><h1>Python LSTM Forecasting Service</h1><p class="detail">Read-only local model-service health dashboard.</p>
<div id="status" class="status checking">Checking <code>/health</code>…</div><p id="details" class="detail">Loading model service status…</p><p><a href="/health">Open the JSON health response</a></p>
<script>const status=document.getElementById('status'),details=document.getElementById('details');async function checkHealth(){try{const response=await fetch('/health',{cache:'no-store'}),body=await response.json();if(!response.ok||body.status!=='Healthy')throw new Error('Unhealthy response');status.className='status healthy';status.textContent='Healthy';details.textContent=`Last checked: ${new Date().toLocaleString()} · Model: ${body.modelVersion||'Not reported'}`}catch{status.className='status unhealthy';status.textContent='Unavailable';details.textContent='The /health endpoint could not be reached. Confirm that the Python service and trained model are available.'}}checkHealth();setInterval(checkHealth,30000);</script>
</main></body></html>"""


@app.post("/forecast", response_model=ForecastResponse)
def forecast(
    request: ForecastRequest,
    predictor: WeatherForecastPredictor = Depends(get_predictor),
) -> ForecastResponse:
    observations = [
        WeatherObservation(
            observation_date=item.observation_date,
            rainfall=item.rainfall,
            temperature=item.temperature,
            humidity=item.humidity,
        )
        for item in request.observations
    ]
    try:
        predictions = predictor.predict(observations)
    except ForecastValidationError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except ForecastArtifactError as exc:
        raise HTTPException(status_code=503, detail=str(exc)) from exc
    except Exception as exc:
        raise HTTPException(status_code=500, detail="Forecast inference failed.") from exc

    if not predictions:
        raise HTTPException(status_code=500, detail="Forecast inference returned no predictions.")

    return ForecastResponse(
        modelVersion=predictions[0].model_version,
        forecastDate=predictions[0].forecast_date,
        forecasts=[
            ForecastResponseItem(
                forecastDate=item.forecast_date,
                targetDate=item.target_date,
                temperature=item.temperature,
                rainfall=item.rainfall,
                humidity=item.humidity,
                modelVersion=item.model_version,
            )
            for item in predictions
        ],
    )
