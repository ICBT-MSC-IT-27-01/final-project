# Chapter 5 Development Draft

Generated: 2026-10-13

Status: proposed draft material. Approved dissertation chapter source files were not found in the repository.

## 5.1 Development Overview

The developed prototype implements an AI-driven weather forecasting and crop recommendation system for Anuradhapura District. The system separates weather forecasting from crop suitability evaluation. The Python LSTM predicts seven days of rainfall, mean temperature and mean relative humidity. The ASP.NET Core backend stores and exposes forecast data, coordinates recommendation evaluation and enforces role-based access. The Angular frontend provides public, registered user, Agricultural Officer and Administrator workflows.

## 5.2 Development Environment And Technologies

The approved stack is Angular, ASP.NET Core Web API, Microsoft SQL Server and Python with TensorFlow/Keras. The backend follows the practical project structure `AnuradhapuraAI.Api`, `AnuradhapuraAI.Application`, `AnuradhapuraAI.Domain` and `AnuradhapuraAI.Infrastructure`. The forecasting service is organized into ingestion, preprocessing, training, evaluation, inference and API modules.

## 5.3 System Architecture Implementation

The implemented architecture follows a three-tier logic:

- Presentation: ASP.NET Core controllers and Angular components.
- Business/Application: authentication services, forecast orchestration, suitability scoring, recommendation orchestration and validation services.
- Data Access: EF Core DbContext, entity configurations, migrations and SQL Server tables.

The `Domain` project stores entities and approved constants. It supports the business layer but does not create a fourth deployment tier.

## 5.4 Database Implementation

The database foundation includes `UserRole`, `User`, `Crop`, `CropEnvironmentalRequirement`, `SoilCompatibility`, `SuitabilityConfiguration`, `ForecastRecord`, `Recommendation`, `RecommendationCrop` and `RecommendationValidation`. Later integration added `ForecastRun` provenance to group operational forecast records. SQL Server table and column naming follows singular PascalCase conventions.

## 5.5 Historical Weather Data Preparation

The model uses `docs/anuradhapura_weather_2010_2025_era5.csv`, an ERA5-derived daily weather dataset for a representative Anuradhapura point. The artifact metadata records 5,844 rows from 2010-01-01 to 2025-12-31, with no missing values, duplicate dates or continuity gaps.

## 5.6 TensorFlow LSTM Model Development

The forecasting input shape is 30 days x 3 variables. The output shape is 7 days x 3 variables. The recorded model uses one LSTM layer with 64 units, dropout 0.2, Adam optimizer, learning rate 0.001, batch size 32 and early stopping patience 10. Chronological splits were used: training 2010-2021, validation 2022-2023 and testing 2024-2025. The scaler was fitted on training data only.

## 5.7 Weather Forecasting Service

The Python service loads the trained model and scaler artifacts, validates request shapes and returns seven forecast rows. Tests cover pipeline behavior and inference API behavior. The backend calls the service through `PythonForecastingClient`, preserving a clean service boundary.

## 5.8 Operational Weather Integration

Operational generation uses recent weather observations from Open-Meteo Forecast API past-days data. The backend validates a 30-day observation window, calls Python inference, and persists one `Operational` `ForecastRun` with seven child `ForecastRecord` rows. Default configuration remains fail-closed unless operational integration flags are explicitly enabled.

## 5.9 Crop Recommendation Engine

The recommendation engine is separate from the LSTM. It evaluates the six approved crops using forecast evidence, crop environmental requirements, optional soil compatibility and configurable weights. The public endpoint `POST /api/recommendations/evaluate` is read-only.

## 5.10 Suitability Scoring Implementation

The suitability implementation calculates factor-level scores and combines them using configurable weights. Approved categories are Highly Suitable, Suitable, Moderately Suitable and Unsuitable. The ranking service orders evaluable crops by score, evidence coverage and fixed crop order.

## 5.11 Optional Soil Data Handling

Soil evidence is optional because no approved live soil source or district-wide soil-selection method is implemented. When soil is absent, available valid factor weights are renormalized instead of inventing a soil score.

## 5.12 Backend API Implementation

The backend exposes authentication, forecast, recommendation, validation and admin endpoints. Controllers remain thin and delegate business behavior to application and infrastructure services. JWT authentication and role policies enforce the approved access boundaries.

## 5.13 Angular Frontend Implementation

The Angular frontend includes public dashboard and recommendation views, registration/login, registered history pages, officer validation pages and administrator readiness controls. It calls backend APIs only; it does not call Open-Meteo or Python inference directly.

## 5.14 Authentication And Authorization

Registration creates a Registered User only. Agricultural Officer and Administrator privileges are controlled by administrators. Backend policies and frontend route guards protect officer and admin areas.

## 5.15 Recommendation History And Feature Flags

Recommendation history is feature-flag controlled. The history persistence endpoint fails closed when `EnableRecommendationHistory` is disabled. Weather model integration also remains disabled by default through feature flags.

## 5.16 Integration And Technical Challenges

Key challenges included source compatibility between ERA5 and Open-Meteo operational inputs, avoiding fabricated agronomic thresholds, keeping recommendation evaluation read-only for public use, and preserving evidence snapshots for review workflows.

## 5.17 Chapter Summary

The implemented system meets the technical MSc prototype architecture while preserving scientific caution. It forecasts weather variables, evaluates crop suitability through explainable scoring and supports role-based workflows, but does not claim production readiness or fully validated agronomic recommendation accuracy.
