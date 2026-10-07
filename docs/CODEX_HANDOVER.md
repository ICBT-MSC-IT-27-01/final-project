# Codex Project Handover

## 1. Project Identity

Project title: **AI-Driven Weather Forecasting and Crop Recommendation System for Anuradhapura District**

Project type: MSc development/research project

Geographic scope: Anuradhapura District, Sri Lanka

The system combines 7-day weather forecasting, explainable crop suitability and recommendation, recommendation history, optional Agricultural Officer validation, and administration/configuration features.

## 2. Fixed Crop Scope

The approved MSc crop scope is exactly:

1. Paddy
2. Maize
3. Green Gram
4. Cowpea
5. Groundnut
6. Chilli

Do not expand or replace this crop scope without explicit project-owner approval.

## 3. Weather Forecasting Scope

Approved forecasting scope:

- Forecast horizon: 7 days
- Variables: Rainfall, Temperature, Humidity
- Model type: LSTM
- Forecasting component: Python-based

The LSTM does **not** directly predict the recommended crop. The LSTM predicts future weather conditions. The crop suitability engine later consumes those forecast values and evaluates the six supported crops.

Conceptual flow:

```text
Historical Weather Data
    ->
Trained LSTM
    ->
7-Day Forecast
(Rainfall, Temperature, Humidity)
    ->
Crop Suitability Engine
    ->
Crop Environmental Requirements
+ Soil Compatibility
+ Configurable Weights/Thresholds
    ->
Suitability Scores
    ->
Suitability Category
    ->
Explanation / Recommendation
```

## 4. Crop Suitability / Recommendation Design

Crop recommendation is intentionally separate from the LSTM weather model.

Approved core suitability factors only:

- Rainfall suitability
- Temperature suitability
- Humidity suitability
- Soil compatibility

Approved categories:

- Highly Suitable
- Suitable
- Moderately Suitable
- Unsuitable

Approved climate-risk indicators:

- Low rainfall
- Heavy rainfall
- High temperature

Weights and thresholds must remain configurable and must not be hard-coded. Do not invent agricultural numeric thresholds.

## 5. User Roles

Approved roles:

- Public User
- Registered User
- Agricultural Officer
- Administrator

Responsibility boundaries:

- Public User: can access core weather forecast information and request/view crop recommendations without compulsory registration.
- Registered User: has public-user functionality, can register, log in, log out, view recommendation history, and view saved recommendation details associated with the authenticated user.
- Agricultural Officer: must authenticate, can review recommendations selected for validation, view forecast evidence and suitability-factor results, assign Pending Validation / Validated / Needs Review, and add an optional comment. Agricultural Officers are review/validation users and cannot edit crop requirements, soil compatibility, suitability weights, thresholds, or system configuration.
- Administrator: must authenticate, can manage crop profiles, environmental requirements, soil compatibility information, configurable suitability weights, configurable category thresholds, users and roles, recommendation records, validation records, and selected system configuration.

Normal registration creates only a Registered User. Users cannot self-assign Agricultural Officer or Administrator roles.

## 6. Approved Technology Stack

Frontend: Angular

Backend: ASP.NET Core Web API

Database: SQL Server

Forecasting: Python + TensorFlow/Keras LSTM

Approved logical flow:

```text
Angular
    ->
ASP.NET Core Web API
    ->
SQL Server
```

```text
ASP.NET Core Web API
    ->
Python Forecasting Component
    ->
Saved LSTM Model / Scaler
```

No specific cloud provider, deployment provider, IIS setup, Docker setup, or hosting platform has been frozen.

## 7. Backend Architecture

Approved logical three-tier architecture:

- Presentation
- Business/Application
- Data Access

Practical backend project structure:

- `AnuradhapuraAI.Api`
- `AnuradhapuraAI.Application`
- `AnuradhapuraAI.Domain`
- `AnuradhapuraAI.Infrastructure`

`Domain` belongs to the business/core side and does not represent a fourth deployment tier.

Important rules:

- Keep controllers thin.
- Keep business logic outside controllers.
- Use dependency injection.
- Keep EF Core/data-access responsibilities separated from business rules.
- Avoid unnecessary over-engineering for the MSc prototype.

## 8. Database Foundation

Approved ten entities:

1. UserRole
2. User
3. Crop
4. CropEnvironmentalRequirement
5. SoilCompatibility
6. SuitabilityConfiguration
7. ForecastRecord
8. Recommendation
9. RecommendationCrop
10. RecommendationValidation

Frozen database decisions:

- `CropEnvironmentalRequirement` uses variable-based rows.
- Supported variable types are `Rainfall`, `Temperature`, and `Humidity`.
- `ForecastRecord.ModelVersion` is nullable.
- There is no separate `RecommendationHistory` table.
- Recommendation history is derived from `Recommendation` records.
- There is no forced `ForecastRecord` -> `Recommendation` foreign key yet.
- Soil source/selection method remains unresolved and must not be invented.

## 9. Completed Development Phases

- Phase 1 - Solution / Project Structure: APPROVED / FROZEN
- Phase 2 - Database Foundation: APPROVED / FROZEN
- Phase 3 - Authentication / RBAC: APPROVED / FROZEN
- Phase 4 - Admin Configuration Foundation: APPROVED / FROZEN
- Phase 5A - ML Pipeline Foundation: APPROVED / FROZEN
- Phase 5B - Real Dataset Training and Evaluation: APPROVED / FROZEN
- Phase 5 - Weather Forecasting Model: COMPLETE / APPROVED / FROZEN
- Phase 6 - Backend Forecast Integration: COMPLETE

Next planned phase: Phase 7 - Suitability Engine

Phase 7 has **not** started yet.

## 10. Authentication / RBAC Implementation

Implemented authentication foundation:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/auth/me`
- JWT Bearer authentication

Authorization policies:

- `AuthenticatedUser`
- `AgriculturalOfficerOnly`
- `AdministratorOnly`

Logout is currently stateless and handled by client-side token removal. Refresh-token and server-side token revocation functionality are not part of the current approved MSc prototype scope.

## 11. Admin Foundation

Existing admin route areas:

- `/api/admin/crops`
- `/api/admin/crop-requirements`
- `/api/admin/soil-compatibility`
- `/api/admin/suitability-config`
- `/api/admin/users`
- `/api/admin/recommendations`
- `/api/admin/validations`

Recommendation and validation access in the admin foundation is read-only where designed.

## 12. Historical Weather Dataset

Actual training dataset:

- `docs/anuradhapura_weather_2010_2025_era5.csv`

Source:

- ERA5 historical/reanalysis weather data accessed through Open-Meteo

Period:

- 2010-01-01 to 2025-12-31

Total daily records:

- 5,844

Variables:

- Temperature
- Rainfall
- Humidity

Quality results:

- Missing values: 0
- Duplicate dates: 0
- Complete daily continuity

Important limitation: ERA5 is reanalysis/modelled historical weather data and is not direct Sri Lanka Department of Meteorology station observation data. The selected dataset represents a representative Anuradhapura location and must not be described as exact microclimate coverage of every location in the entire district.

## 13. ML Training Configuration

- Lookback: 30 days
- Forecast horizon: 7 days
- Input shape: `(samples, 30, 3)`
- Output shape: `(samples, 7, 3)`

Chronological periods:

- Training: 2010-2021
- Validation: 2022-2023
- Test: 2024-2025

Scaler:

- `MinMaxScaler`
- Scaler fitted only on training data.

Model architecture:

```text
Input(30,3)
->
LSTM(64)
->
Dropout(0.2)
->
Dense(21)
->
Reshape(7,3)
```

Hyperparameters:

- Batch size: 32
- Maximum epochs: 100
- Learning rate: 0.001
- Early stopping patience: 10
- Seed: 42
- Loss: MSE
- Optimizer: Adam

Actual training:

- 76 epochs
- Best validation loss epoch: 66
- Best weights restored

## 14. Model Evaluation Results

Persistence baseline overall:

- MAE = 3.584
- RMSE = 8.738

LSTM overall:

- MAE = 3.163
- RMSE = 6.729

Per-variable LSTM:

- Rainfall: MAE = 4.904, RMSE = 10.547
- Temperature: MAE = 0.707, RMSE = 0.891
- Humidity: MAE = 3.879, RMSE = 4.881

Notes:

- LSTM outperformed the persistence baseline.
- Rainfall remains the most difficult target.
- Forecast error generally increases across the seven-day horizon.
- Overall MAE/RMSE combines variables with different units, so per-variable metrics should receive greater emphasis in dissertation evaluation.

## 15. Model Artifacts

Current artifact location:

- `src/forecasting-service/artifacts/v1_era5_2010_2025/`

Expected important files include:

- `v1.keras`
- `v1_scaler.joblib`
- `v1_metadata.json`
- `v1_evaluation.json`
- `v1_training_history.json`
- `v1_sample_forecast.json`

Do not move or regenerate these files during handover. These generated model and preprocessing artifacts are ignored by Git through `.gitignore`.

## 16. Current Testing Status

Verified testing state:

- Phase 5 Python tests: `python -m pytest` from `src/forecasting-service` collected 13 tests and passed 13.
- Backend tests: `dotnet test` from `src/backend` passed 21 tests in `AnuradhapuraAI.Phase3Tests`.

No additional frontend test result is recorded in this handover.

## 17. Features Explicitly Outside Current Scope

The following are outside the approved MSc scope unless explicitly approved later:

- nationwide coverage
- IoT soil sensors
- automated irrigation
- pest/disease prediction
- market-price forecasting
- native mobile application
- full multilingual UI
- nationwide alerts
- yield prediction
- autonomous farming decisions
- NPK-based recommendation
- organic-matter-based recommendation
- land-size recommendation
- planting-time recommendation
- season-specific weighting
- high-humidity climate-risk indicator

## 18. Feature Flags

Approved lightweight feature flags:

- `EnableOfficerValidation`
- `EnableRecommendationHistory`
- `EnableClimateRiskIndicators`
- `EnableWeatherModelIntegration`

These are lightweight/config-driven placeholders and not an enterprise feature-flag platform.

## 19. Important Unresolved / Future Decisions

The following must not be invented by a future developer or Codex session:

- final agricultural numeric thresholds/ranges where not yet approved
- final soil data/source/selection method
- specific production hosting provider
- any additional features outside approved scope
- any architecture/schema change affecting frozen phases

These require explicit project-owner approval.

## 20. Rules for Future Codex Sessions

1. Read this handover document before modifying the project.
2. Read `AGENTS.md` if present.
3. Read `.agents/skills/anuradhapura-ai-development/`.
4. Treat APPROVED/FROZEN decisions as immutable unless the project owner explicitly approves a change.
5. Do not silently expand project scope.
6. Do not invent agricultural values.
7. Do not fabricate model performance.
8. Do not retrain or replace the approved model without approval.
9. Do not modify completed database/auth/admin foundations unnecessarily.
10. Work phase-by-phase.
11. Stop after each phase and provide a completion report.
12. Wait for project-owner approval before starting the next phase.

## 21. Next Development Task

Next planned development phase:

- Phase 7 - Suitability Engine

Do not start Phase 7 without project-owner approval.

## Startup Prompt for a New Codex Session

Read the following before making any changes:

1. `AGENTS.md`, if present
2. `.agents/skills/anuradhapura-ai-development/`
3. `docs/CODEX_HANDOVER.md`

This is an MSc project with approved/frozen development decisions.

Confirm that you understand:

- the project scope
- the six supported crops
- the architecture
- the completed phases
- the trained LSTM model
- the distinction between weather forecasting and crop recommendation
- the current project status
- the next planned phase

Do not modify anything yet. First provide a short project-state summary and wait for approval.
