# Project Specification

## 1. Project identity and scope

Project title: **AI-Driven Weather Forecasting and Crop Recommendation System for Anuradhapura District**

Geographical scope: Anuradhapura District, Sri Lanka.

Supported crops:
- Paddy
- Maize
- Green Gram
- Cowpea
- Groundnut
- Chilli

Core forecast horizon: 7 days.

Primary weather variables:
- rainfall
- temperature
- humidity

The project must develop and integrate its own weather-forecasting component rather than relying only on an external forecast API.

Proposed forecasting approach:
- Python-based
- LSTM-based time-series model

The system must also include an explainable crop-suitability and recommendation engine.

## 2. Approved technology stack

Frontend: Angular.

Backend: ASP.NET Core Web API.

Database: Microsoft SQL Server.

Machine learning: Python with an LSTM-based forecasting component.

Communication boundaries:
- Angular communicates with ASP.NET Core Web API.
- ASP.NET Core communicates with SQL Server.
- ASP.NET Core communicates with the Python forecasting component through a clean service/API boundary.

Do not lock the implementation to a particular cloud provider, IIS setup, Docker setup, or hosting platform unless later approved.

## 3. User roles

The system has four user roles:
1. Public User
2. Registered User
3. Agricultural Officer
4. Administrator

### Public User
- Access core weather forecast information.
- Request/view crop recommendations.
- Registration is not compulsory for core functionality.

### Registered User
- Has public-user functionality.
- Can register.
- Can log in and log out.
- Can view recommendation history.
- Can view saved recommendation details.
- Recommendation history is associated with the authenticated user.

### Agricultural Officer
- Must authenticate.
- Can review recommendations selected for validation.
- Can view forecast evidence and suitability-factor results.
- Can assign: Pending Validation, Validated, Needs Review.
- Can add an optional comment.
- Validation history must be retained.
- Must not modify crop requirements, soil compatibility, weights, thresholds, or system configuration.

### Administrator
- Must authenticate.
- Can manage crop profiles.
- Can manage crop environmental requirements.
- Can manage soil compatibility information.
- Can manage configurable suitability weights.
- Can manage configurable category thresholds.
- Can manage users and roles.
- Can view recommendation records.
- Can view validation records.
- Can manage selected system configuration.

Normal registration creates a normal Registered User. Users must not self-assign Agricultural Officer or Administrator privileges.

## 4. Weather forecasting component

Inputs/dataset variables:
- rainfall
- temperature
- humidity

Forecast horizon: 7 days.

Workflow:
1. collect/import historical weather data
2. clean data
3. handle missing values
4. normalize/scale features where appropriate
5. prepare time-series sequences
6. split data chronologically into training, validation, and test sets
7. train LSTM model
8. generate 7-day forecast
9. evaluate with MAE and RMSE
10. compare with a simple baseline
11. make forecast output available to the backend/web system

Rules:
- Do not use random train/test splitting for time-series data.
- Do not fabricate model scores.
- Do not assume fixed split percentages unless later approved.
- Do not add MAPE, R-squared, satellite data, climate indices, or multiple-model competitions unless approved.
- Keep the historical data source configurable.
- Do not hard-code a provider such as OpenWeatherMap.

Separate modules for:
- data ingestion
- preprocessing
- model training
- model evaluation
- inference/prediction
- service integration

## 5. Crop suitability and recommendation engine

The recommendation engine is explainable and rule-based/weighted-scoring based. It is separate from the ML weather model.

Core factors only:
- rainfall suitability
- temperature suitability
- humidity suitability
- soil compatibility

Workflow:
1. retrieve the 7-day forecast
2. retrieve crop environmental requirements
3. retrieve soil compatibility information
4. calculate rainfall suitability for each crop
5. calculate temperature suitability for each crop
6. calculate humidity suitability for each crop
7. calculate soil compatibility score
8. apply configurable weights
9. calculate overall suitability score
10. assign suitability category
11. rank the six crops
12. generate factor-level explanation
13. generate approved climate-risk indicators
14. display results
15. store recommendation history for authenticated users
16. optionally support Agricultural Officer validation

Approved categories:
- Highly Suitable
- Suitable
- Moderately Suitable
- Unsuitable

Approved climate risks:
- Low rainfall
- Heavy rainfall
- High temperature

Do not add NPK, organic matter, irrigation, pest/disease prediction, market price, land size, planting-time recommendations, season-specific weighting, yield prediction, high-humidity risk, or autonomous farming decisions unless later approved.

Weights and thresholds must be configurable. Do not hard-code final agricultural values.

## 6. Database foundation

Use SQL Server.

Core logical entities:
- UserRole
- User
- Crop
- CropEnvironmentalRequirement
- SoilCompatibility
- SuitabilityConfiguration
- ForecastRecord
- Recommendation
- RecommendationCrop
- RecommendationValidation

Implementation fields such as IDs, timestamps, active flags, password hashes, and foreign keys may be added where technically necessary. Do not silently invent new domain-specific agricultural fields. Explain and request approval for domain additions.

Suggested implementation conventions:
- each entity has `Id` as its primary key where practical
- foreign keys use `<EntityName>Id`
- audit timestamps can use `CreatedAt` / `UpdatedAt` where appropriate

Avoid unnecessary duplicate location entities unless a justified requirement emerges.

## 7. Authentication and authorization

Implement secure authentication and role-based authorization.

Requirements:
- public access to core forecast/recommendation functions
- authentication for registered-user history
- authentication for officer validation
- authentication for administrator functions
- secure password hashing
- RBAC
- protection against privilege escalation
- privileged role assignment controlled by administrators

JWT may be used if appropriate, but document the decision and keep the implementation consistent.

## 8. Backend modules

Suggested modules/services:
- Authentication
- User Management
- Crop Management
- Crop Requirement Management
- Soil Compatibility Management
- Suitability Configuration
- Forecast Integration
- Recommendation / Suitability Engine
- Recommendation History
- Agricultural Officer Validation
- Administration
- Audit / Logging where appropriate

Separate controllers/API concerns, application/business services, domain rules, data access, DTOs, and validation.

## 9. Three-tier backend architecture

Use a three-tier architecture.

### Tier 1 — Presentation
- ASP.NET Core Web API
- controllers
- request/response DTOs
- authentication/authorization integration
- input validation
- API error responses

### Tier 2 — Business / Application
- application services
- business rules
- forecast orchestration
- crop suitability logic
- recommendation generation
- recommendation history logic
- officer validation logic
- admin configuration logic
- service interfaces

### Tier 3 — Data Access
- Entity Framework Core
- SQL Server
- DbContext
- entity configurations
- migrations
- data-access abstractions where useful

Rules:
- controllers stay thin
- no core business logic in controllers
- business layer must not depend on HTTP concerns
- data-access logic stays separate from business logic
- use dependency injection
- do not add repository abstractions merely for ceremony if EF Core already provides enough abstraction
- keep architecture appropriate for an MSc prototype

A practical solution layout may use projects/folders such as:
- Api
- Application
- Domain
- Infrastructure

Even if four code projects are used, the logical architecture remains three-tier: Presentation, Business, Data Access. `Domain` belongs to the business/core side rather than becoming a fourth deployment tier.

## 10. Angular frontend modules

Public:
- Home
- Weather forecast view
- Crop recommendation view
- Recommendation explanation
- Climate-risk indicators

Registered User:
- Register
- Login
- Logout
- Recommendation history
- Recommendation details

Agricultural Officer:
- Login
- Pending validation list
- Recommendation review page
- Forecast evidence
- Suitability details
- Validation status
- Optional comment
- Validation history where appropriate

Administrator:
- Login
- Dashboard
- Crop management
- Environmental requirement management
- Soil compatibility management
- Suitability weight management
- Suitability threshold management
- User/role management
- Recommendation record view
- Validation record view

Keep UI simple and usable. Do not add unrelated features.

## 11. API design

Use RESTful endpoints and consistent resource naming.

Suggested groups:
- /api/auth
- /api/users
- /api/roles
- /api/crops
- /api/crop-requirements
- /api/soil-compatibility
- /api/suitability-config
- /api/forecasts
- /api/recommendations
- /api/recommendations/history
- /api/validations
- /api/admin

Exact routes may be improved while remaining REST-oriented and consistent.

Document request DTOs, response DTOs, validation rules, authorization requirements, and error responses.

## 12. Non-functional requirements

Address:
- usability
- performance
- reliability
- security
- role-based access control
- data integrity
- maintainability
- explainability
- auditability
- browser compatibility
- error handling
- model reproducibility
- extensibility
- privacy

Do not over-engineer beyond MSc scope.

## 13. Out of scope

Do not implement unless explicitly approved later:
- nationwide coverage
- IoT soil sensors
- automated irrigation
- pest prediction
- disease prediction
- market-price forecasting
- native mobile application
- full multilingual UI
- nationwide alerts/notifications
- crop yield prediction
- autonomous farming decisions

## 14. Development order

### Phase 1 — Solution and project structure
- inspect existing repository
- propose final folder/project structure
- create backend solution structure
- create Angular project structure
- create Python ML service structure
- define environment configuration pattern
- identify dependencies, immediate risks, and ambiguities
- create only the basic skeleton

### Phase 2 — Database foundation
- define entities and relationships
- configure SQL Server
- create migrations
- seed four roles
- seed six crops
- do not seed unverified agricultural thresholds

### Phase 3 — Authentication and RBAC
- normal-user registration
- login/logout
- RBAC
- admin-controlled privileged role assignment

### Phase 4 — Admin configuration
- crop CRUD
- crop-requirement CRUD
- soil-compatibility CRUD
- suitability weight/threshold configuration
- user/role management

### Phase 5 — Forecasting-service skeleton
- Python structure
- data-loading interfaces
- preprocessing pipeline
- LSTM training pipeline
- evaluation pipeline
- inference interface
- backend service/API interface
- placeholders/mocks only when clearly marked

### Phase 6 — Backend forecast integration
- integrate Python forecasting service
- store/retrieve forecast records where appropriate
- expose 7-day forecast to Angular

### Phase 7 — Suitability engine
- factor scoring
- configurable weights
- overall score
- category assignment
- ranking
- explanations
- approved climate-risk indicators

### Phase 8 — Recommendation flow
- public recommendation generation
- authenticated persistence
- history
- recommendation detail

### Phase 9 — Agricultural Officer validation
- pending validation
- review details
- Validated / Needs Review
- comments
- validation history

### Phase 10 — Angular interfaces
- public pages
- registered-user pages
- officer pages
- admin pages

### Phase 11 — Testing
- backend unit tests
- suitability-engine tests
- authentication/authorization tests
- API integration tests
- practical frontend tests
- ML evaluation scripts/tests
- manual test cases

## 15. Coding standards

Use:
- meaningful names
- small focused methods
- dependency injection
- async database/API operations where appropriate
- DTOs
- request validation
- centralized error handling
- logging
- secure configuration
- environment variables / secrets management
- consistent formatting

Never commit passwords, connection-string secrets, API keys, or private data.

## 16. Naming conventions

### C# / ASP.NET Core
- Classes, interfaces, methods, properties: PascalCase
- Local variables and parameters: camelCase
- Interfaces start with `I`, e.g. `IRecommendationService`
- Services: `RecommendationService`, `ForecastService`, `ValidationService`
- Controllers: plural resource controllers such as `RecommendationsController`
- DTOs: descriptive names such as `CreateRecommendationRequest`, `RecommendationResponse`
- Async methods end with `Async` where appropriate

### Angular / TypeScript
- Classes/interfaces/types: PascalCase
- Variables/methods/properties: camelCase
- Files/folders: kebab-case
- Components: `RecommendationHistoryComponent`
- Services: `RecommendationService`
- Avoid unclear abbreviations

### Python
- Modules/files: snake_case
- Functions/variables: snake_case
- Classes: PascalCase
- Constants: UPPER_SNAKE_CASE
- Prefer descriptive ML variable names

### SQL Server
Preferred convention:
- tables: singular PascalCase
- columns: PascalCase
- primary key: `Id`
- foreign keys: `<EntityName>Id`

Do not mix forms such as `user_id`, `UserID`, `userid`, and `UserId`. Use `UserId`.

### API routes
- lowercase resource routes
- plural resources where appropriate
- avoid verbs unless a genuine action endpoint is needed
- keep terminology aligned with dissertation and diagrams

## 17. Feature flags

Use lightweight, configuration-driven feature flags only where helpful for development, testing, or controlled rollout.

Approved candidate flags:
- EnableOfficerValidation
- EnableRecommendationHistory
- EnableClimateRiskIndicators
- EnableWeatherModelIntegration

Rules:
- configure through appsettings/environment or a lightweight feature-management mechanism
- do not hard-code flags in business rules
- do not introduce an external enterprise feature-flag platform unless later required
- do not use feature flags to introduce unapproved functionality
- document each flag and its default value
- core project scope remains unchanged

## 18. Implementation behaviour

Before large code changes:
1. inspect repository state
2. identify the current phase
3. state intended changes
4. identify dependencies and migration impact
5. flag assumptions
6. implement only approved scope

After changes:
- summarize changes
- list files changed
- list schema/API changes
- list tests and results
- list assumptions
- list TODOs and approval items

## 19. Safety against specification drift

If a requested feature or code change introduces any of the following, stop and ask for approval:
- a new agricultural factor
- a new crop
- a new user role
- a new suitability category
- a new climate-risk indicator
- a new location scope
- a new external provider dependency
- a new privileged permission
- new domain-specific database attributes
- a new ML metric/model family
- a deployment assumption not already approved

Implementation convenience must not silently change the dissertation scope.
