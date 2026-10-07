from __future__ import annotations

from datetime import date
import math
from typing import Annotated

from fastapi import Depends, FastAPI, HTTPException, Request
from fastapi.responses import JSONResponse
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
