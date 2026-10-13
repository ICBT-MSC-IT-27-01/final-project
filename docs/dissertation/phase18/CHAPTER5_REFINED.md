# Chapter 5 System Development Refined Draft

Generated: 2026-10-13

Status: refined draft for supervisor review. This chapter should be merged into the university template after approved Chapters 1-4 are available.

## 5.1 Development Overview

The system was developed as a modular MSc prototype for Anuradhapura District. It combines an Angular frontend, ASP.NET Core Web API backend, Microsoft SQL Server database and Python forecasting service. The design deliberately separates weather prediction from crop recommendation. The LSTM produces seven-day weather forecasts, while the crop suitability engine evaluates crops through explainable weighted scoring.

## 5.2 Technologies And Development Environment

The frontend uses Angular. The backend uses ASP.NET Core with separate Api, Application, Domain and Infrastructure projects. SQL Server is used for persistence through Entity Framework Core. The forecasting service uses Python, TensorFlow/Keras, scikit-learn and FastAPI-style service boundaries. Development configuration uses appsettings, environment variables and local secret mechanisms rather than committed credentials.

## 5.3 Architecture Implementation

The implemented backend follows a three-tier logical architecture:

- Presentation: controllers, request/response DTOs and authentication middleware.
- Business/Application: forecast orchestration, recommendation orchestration, suitability scoring, validation and admin services.
- Data Access: EF Core DbContext, entity configurations, migrations and SQL Server.

The Domain project contains entities and approved constants, but it remains part of the application core rather than a separate deployment tier.

## 5.4 Database Implementation

The database includes users, roles, crops, crop environmental requirements, soil compatibility, suitability configuration, forecasts, recommendations and validation records. Forecast provenance is represented with `ForecastRun` and child `ForecastRecord` rows, allowing the system to distinguish operational forecast runs from historical or legacy records.

## 5.5 ERA5 Historical Weather Data Preparation

The historical dataset is `docs/anuradhapura_weather_2010_2025_era5.csv`. The model metadata records 5,844 daily rows from 2010-01-01 to 2025-12-31, with no missing values, duplicate dates or continuity gaps. The dataset represents a gridded/reanalysis source for a representative Anuradhapura point and must not be described as complete station coverage of the district.

## 5.6 LSTM Model Development

The model input is a 30-day sequence with three features: rainfall, mean temperature and mean relative humidity. The output is a seven-day sequence for the same three variables. The recorded architecture uses an LSTM layer with 64 units, dropout 0.2, a dense output layer and reshape to seven days by three features. The chronological split is training 2010-2021, validation 2022-2023 and test 2024-2025. The scaler was fitted only on training data.

## 5.7 Forecasting Service And Backend Integration

The Python service loads saved artifacts and returns seven forecast rows. The backend integrates through `PythonForecastingClient`, validating response shape, model version consistency, target dates and weather values. This preserves a clean boundary between ASP.NET Core and the Python model.

## 5.8 Operational Weather Integration

Operational generation uses Open-Meteo Forecast API past-days data as recent input observations. The backend validates a 30-day observation window, calls Python inference and persists one `Operational` forecast run with seven forecast records. Feature flags keep this integration disabled by default unless explicitly enabled in a controlled runtime.

## 5.9 Crop Recommendation And Suitability Scoring

The recommendation engine evaluates only Paddy, Maize, Green Gram, Cowpea, Groundnut and Chilli. It uses rainfall, temperature, humidity and optional soil compatibility. Suitability categories are Highly Suitable, Suitable, Moderately Suitable and Unsuitable. The engine provides factor-level evidence and ranks evaluable crops.

## 5.10 Missing Soil Handling

Soil is optional because no approved live soil-source workflow exists. When soil is missing, the engine renormalizes available valid factor weights rather than assigning a fabricated soil score.

## 5.11 Backend API And Role-Based Access

The backend exposes authentication, forecast, recommendation, validation and admin endpoints. Public users can view forecasts and request read-only recommendations. Registered users can access history when enabled. Agricultural Officers review recommendations but cannot configure agronomic values. Administrators manage configuration and users.

## 5.12 Angular Frontend

The Angular frontend implements public dashboard and recommendation pages, authentication screens, registered history pages, officer review screens and administrator readiness controls. The frontend calls backend APIs only and does not expose provider credentials or call the Python service directly.

## 5.13 Feature Flags And Security Considerations

Feature flags control weather-model integration, recommendation history, officer validation and climate-risk indicators. Default behavior is conservative and fail-closed where a feature could create side effects.

## 5.14 Development Challenges

Major challenges included avoiding unapproved agronomic thresholds, handling source shift between ERA5 and Open-Meteo operational data, preserving read-only public recommendation evaluation and documenting the limits of seven-day crop suitability.

## 5.15 Summary

The implemented system satisfies the technical prototype scope and provides a defensible development foundation for dissertation review. It should be presented as a decision-support prototype with documented limitations, not as a production or autonomous agricultural decision system.
