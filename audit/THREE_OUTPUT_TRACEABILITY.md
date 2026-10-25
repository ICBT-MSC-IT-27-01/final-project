# Three-output traceability

The three outputs have distinct time bases and services. Crop suitability is a separate assessment that consumes the stored seven-day forecast. A successful HTTP response is a technical result, not agricultural validation.

| Output | UI → HTTP → service → evidence/model → response → UI | Observed state |
| --- | --- | --- |
| OUT-1: seven-day AI weather | `src/frontend/src/app/features/public-dashboard/public-dashboard.component.ts:120` → `src/frontend/src/app/core/api.service.ts:91` → `src/backend/AnuradhapuraAI.Api/Controllers/ForecastsController.cs:10` → `src/backend/AnuradhapuraAI.Infrastructure/Forecasting/ForecastService.cs:95` → `ForecastRun`/`ForecastRecord` in `AnuradhapuraAiDbContext.cs` → `LatestForecastResponse` in `api.models.ts:257` → seven daily cards, provenance and limitations | Local API and browser: seven stored operational days. Separately, Python `/forecast` produced seven dates from 30 historical rows; no fresh external-provider-to-SQL run this turn. |
| OUT-2: crops by date range | `src/frontend/src/app/features/seasonal/seasonal-planning.component.ts:24,141` → `api.service.ts:94` → `SeasonalRecommendationsController.cs:8` → `SeasonalClimateService.cs:23` → `docs/anuradhapura_weather_2010_2025_era5.csv` monthly climate + `docs/English calendar images/evidance json/DOA_SIX_CROP_CALENDAR_MONTH_EXTRACTION_REVIEW.json` → `SeasonalRangeResponse` in `api.models.ts:14` → six cards with conditional calendar status and evidence | 1–30 Nov 2026 returned six crops and `CALENDAR_SUPPORTED_CANDIDATES`. Past dates, reversed ranges and >366 days are rejected by service validation; observed past and >366 responses were 400. |
| OUT-3: periods by crop | `seasonal-planning.component.ts:31,148` → `api.service.ts:97` → `SeasonalRecommendationsController.cs:9` → `SeasonalClimateService.cs:33` and `DoaCalendarMonthCatalog.cs:10` → same historical CSV and DoA calendar JSON → `SeasonalCropResponse` in `api.models.ts:15` → approximate establishment-month windows with source conditions | Each of six crop calls returned 200. For November 2026, Paddy and four other crops had one conditional candidate; Maize returned `CLIMATE_CONTEXT_ONLY` with none. Unsupported crop returned 400. |

## State and time-basis rules

- `Operational` on the stored forecast describes the run's source/status; it does not validate forecast accuracy. The stored run was generated earlier. `v1` metadata reports 30×3 input and 7×3 output; `training/train.py:38-55` splits chronologically and fits the scaler on training rows.
- `CALENDAR_SUPPORTED_CANDIDATE(S)` means the selected dates intersect an approximate documented establishment month. `CLIMATE_CONTEXT_ONLY` means no matching calendar window; monthly historical climatology remains context. Neither state is a long-range LSTM forecast or exact planting advice.
- Crop suitability follows `RecommendationsController.cs:19` → `RecommendationOrchestrationService.cs` → `CropSuitabilityEngine.cs` → `RecommendationRankingService.cs`. Mandatory evidence absence yields factor details without a comparable score/category/rank. A complete scored result can still be `Unsuitable`; current local data produced that category for all six.
- The frontend may display the current seven-day forecast beside seasonal planning as separate context. `seasonal-planning.component.ts:57-59` explicitly says that predicted weather does not change the calendar result.
- `DoaCalendarMonthCatalog.cs` enforces exactly the six approved crop names. `AdminManagementService.cs` now enforces the same names for creation/renaming.

## Evidence inventory

| File | Current check |
| --- | --- |
| Historical CSV | 5,844 continuous daily rows from 2010-01-01 through 2025-12-31; columns `Date,Temperature,Rainfall,Humidity`; SHA-256 `DB88D3072A4336F2ECC5D177ADC9CC940BFACFE870221733F62A8DC36D9A3E97`. |
| DoA calendar extraction JSON | Exactly six crops; SHA-256 `495C9B10AB30A092C8F6F5096470BF472BE6E72B87880A6A0C6DF4207AB148F6`. All six entries currently record `agronomistValidated=false` and `productionApproved=false`. |
| `v1` artifact | Model/scaler/metadata files exist. Metadata reports 30-day lookback, seven-day horizon, 64 units, dropout 0.2, and test target dates 2024-01-01 through 2025-12-31. `v1_evaluation.json` has `test_target_date_range`; it does not contain an explicit `evaluation_split` field. |
