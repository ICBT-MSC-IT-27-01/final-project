# Phase 1 — Scope and Technical Baseline Confirmation

**Project:** AI-Driven Weather Forecasting and Crop Recommendation System for Anuradhapura District
**Completed:** 17 October 2026
**Authority:** Project-owner instruction in this work session: Seasonal Planning is treated as approved project scope.

## Approved project outcome

The system is an Anuradhapura District decision-support web application. It provides a seven-day LSTM weather forecast for rainfall, temperature and relative humidity, then presents explainable crop-related information for exactly six crops: Paddy, Maize, Green Gram, Cowpea, Groundnut and Chilli. It supports Public Users, Registered Users, Agricultural Officers and Administrators.

The system is not an autonomous farming system and must not claim guaranteed crop success. The LSTM remains a seven-day weather model; historical seasonal planning is separately labelled historical climate and calendar guidance, not a long-range LSTM forecast.

## Scope baseline

| Area | Baseline decision |
|---|---|
| Location | Anuradhapura District, Sri Lanka only |
| Crops | Paddy, Maize, Green Gram, Cowpea, Groundnut and Chilli only |
| Weather variables | Rainfall, temperature and relative humidity |
| Forecast model | Existing Python LSTM v1, seven-day horizon; do not retrain unless a genuine technical defect is identified |
| Architecture | Angular → ASP.NET Core API → SQL Server / Python FastAPI; Open-Meteo is the operational observation source and ERA5 is the historical training/evaluation source |
| Suitability factors | Rainfall, temperature, humidity and optional soil only |
| Seasonal Planning | **Approved scope by project-owner instruction.** It remains calendar/climate guidance, with its current evidence limitations disclosed. |
| Out of scope | New crops, locations, agricultural factors, model families, IoT, irrigation, pests, disease, market price, yield prediction, mobile app, autonomous decisions and full multilingual UI |

## Current technical baseline

| Component | Current state |
|---|---|
| Backend | ASP.NET Core Api/Application/Domain/Infrastructure implementation with SQL Server persistence, JWT/RBAC, forecast/recommendation/admin/officer services |
| Frontend | Angular public forecast/recommendation/seasonal pages plus role-protected history, officer and administrator pages |
| Forecasting service | Python FastAPI with saved LSTM v1, scaler, metadata, chronological evaluation and persistence baseline |
| Forecast workflow | Open-Meteo → LSTM v1 → SQL forecast run/records → Angular dashboard/recommendation view, previously verified end-to-end |
| QA-001 | PARTIAL. Temperature-only indicators are supported; full crop suitability, categories and ranks require further approved scientific evidence. |
| Officer validation | Implemented; live test awaits authorized Officer credentials and a safe pending recommendation record. |
| Recommendation history | Implemented but disabled by its approved feature flag. |
| Climate risks | Guarded implementation exists, but approved threshold evidence is absent. |

## Preservation rules for subsequent phases

- Do not modify `AnuradhapuraAI.Api.csproj`.
- Do not retrain or replace the working LSTM v1 without a demonstrated technical defect.
- Do not change production settings, existing database records or approved local development settings without explicit authorization.
- Keep soil optional.
- Never convert annual or seasonal rainfall requirements into seven-day thresholds without expert-approved scientific justification.
- Keep QA-001 PARTIAL unless complete valid evidence and expert validation support a full assessment.
- Do not add features or methodologies beyond this baseline.

## Baseline acceptance and next gates

Phase 1 is complete as a scope and technical-baseline record. The next phase requires external agricultural evidence/validation before full crop-suitability scoring can be enabled. The project-owner decision above resolves the Seasonal Planning scope classification; it does not replace required scientific validation for suitability calculations.
