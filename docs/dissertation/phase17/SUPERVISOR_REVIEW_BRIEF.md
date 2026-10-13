# Phase 17 Supervisor Review Brief

Generated: 2026-10-13

## Project Overview

The project is an AI-Driven Weather Forecasting and Crop Recommendation System for Anuradhapura District, Sri Lanka. It combines a Python LSTM weather forecasting component, an ASP.NET Core Web API, SQL Server persistence, an Angular frontend, and an explainable weighted crop suitability engine.

## Implemented Capabilities

- Seven-day weather forecasting pipeline for rainfall, mean temperature and mean relative humidity.
- Historical ERA5-based training and evaluation workflow.
- Backend forecast integration and operational ForecastRun persistence.
- Public forecast and read-only crop recommendation evaluation.
- Recommendation history behind a feature flag.
- Registered user, Agricultural Officer and Administrator role boundaries.
- Agricultural Officer validation workflow.
- Angular user interfaces and browser E2E tests.

## Current Evidence Status

| Area | Status |
| --- | --- |
| Technical implementation | Complete for prototype scope |
| Historical weather model evaluation | Complete against ERA5 test split |
| Operational forecast integration | Technically verified once in controlled development context |
| Scientific agronomic validation | Partial |
| Real participant UAT | Not performed |
| Agricultural Officer expert validation | Not performed |
| Ethics approval | Pending |
| Production readiness | Not claimed |

## Supervisor Input Needed

1. Confirm whether draft Chapter 5-8 structure matches university expectations.
2. Confirm how to present partial agronomic validation without overstating recommendation accuracy.
3. Confirm whether real participant UAT is required before submission or can remain future work.
4. Confirm required citation style and figure numbering.
5. Confirm whether additional source-compatibility analysis between ERA5 and Open-Meteo is needed.
