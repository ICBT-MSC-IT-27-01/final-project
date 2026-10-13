# Phase 18 Abstract And Front Matter Draft

Generated: 2026-10-13

Status: draft with placeholders. University template and approved title page data were not available.

## Title Page Placeholder

Title: AI-Driven Weather Forecasting and Crop Recommendation System for Anuradhapura District, Sri Lanka

Student name: `[OWNER TO COMPLETE]`

Student ID: `[OWNER TO COMPLETE]`

Supervisor: `[OWNER TO COMPLETE]`

Institution: `[OWNER TO COMPLETE]`

Submission date: `[OWNER TO COMPLETE]`

## Abstract Draft

This dissertation presents the design, development and evaluation of an AI-driven weather forecasting and crop recommendation prototype for Anuradhapura District, Sri Lanka. The system uses a Python TensorFlow/Keras LSTM model to forecast seven days of rainfall, mean temperature and mean relative humidity from a 30-day input sequence. Historical model development used an ERA5-derived daily weather dataset covering 2010 to 2025. The web application integrates the forecasting service with an ASP.NET Core Web API, Microsoft SQL Server database and Angular frontend.

Crop recommendation is implemented separately from the weather model through an explainable weighted suitability engine. The system evaluates the approved crops Paddy, Maize, Green Gram, Cowpea, Groundnut and Chilli using weather suitability factors and optional soil compatibility. The implementation supports public recommendation evaluation, registered user history, Agricultural Officer validation workflow and Administrator configuration controls.

Technical validation included backend tests, Python forecasting-service tests, Angular tests, production build verification and Playwright browser E2E tests. The LSTM improved over a persistence baseline on historical ERA5 test data, with overall MAE improving by 11.73% and RMSE by 22.99%. These metrics are mixed-unit historical weather metrics and are not crop recommendation accuracy or independently verified operational forecast accuracy.

The project demonstrates technical feasibility and explainable decision-support design. However, scientific agronomic validation remains partial, real participant UAT was not performed, Agricultural Officer expert validation was not performed and ethics approval remains pending. The system should therefore be presented as a technically validated MSc prototype rather than a production-ready agricultural decision system.

## Keywords

Weather forecasting; LSTM; crop recommendation; explainable suitability scoring; Anuradhapura District; Angular; ASP.NET Core; SQL Server; TensorFlow.

## Acknowledgements Placeholder

`[OWNER TO COMPLETE. Do not add names without consent or actual contribution.]`

## Declaration Placeholder

`[OWNER TO COMPLETE USING UNIVERSITY TEMPLATE.]`

## Table Of Contents Placeholder

1. Introduction
2. Literature Review
3. Research Methodology
4. System Analysis and Design
5. System Development
6. System Testing
7. Evaluation and Discussion
8. Conclusion and Future Work
9. References
10. Appendices

## Abbreviations

| Abbreviation | Meaning |
| --- | --- |
| API | Application Programming Interface |
| E2E | End-to-End |
| EF Core | Entity Framework Core |
| ERA5 | ECMWF Reanalysis v5 |
| JWT | JSON Web Token |
| LSTM | Long Short-Term Memory |
| MAE | Mean Absolute Error |
| RMSE | Root Mean Squared Error |
| UAT | User Acceptance Testing |
