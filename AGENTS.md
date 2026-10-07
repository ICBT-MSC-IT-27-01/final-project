# Repository Guidelines

## Project Structure & Module Organization

This repository is for the **AI-Driven Weather Forecasting and Crop Recommendation System for Anuradhapura District**. The current workspace contains project guidance under `.agents/`; application code should follow this layout as it is added:

- `src/backend/` or `backend/`: ASP.NET Core Web API, with clear `Api`, `Application`, `Domain`, and `Infrastructure` boundaries.
- `src/frontend/` or `frontend/`: Angular UI for public users, registered users, Agricultural Officers, and Administrators.
- `src/forecasting-service/` or `forecasting-service/`: Python LSTM weather forecasting modules for ingestion, preprocessing, training, evaluation, and inference.
- `tests/`: backend, suitability-engine, frontend, and ML pipeline checks.
- `.agents/`: agent/project instructions and approved specification references.

Keep the weather model separate from the explainable crop-suitability engine.

## Build, Test, and Development Commands

The implementation scaffold has not been generated yet, so confirm commands after each project is created:

- `dotnet build`: build the ASP.NET Core solution.
- `dotnet test`: run backend tests.
- `npm install` then `npm start`: install and run the Angular app.
- `npm test`: run frontend tests.
- `python -m pytest`: run Python forecasting-service tests.

Document any new command in the relevant project README.

## Coding Style & Naming Conventions

Use meaningful names, dependency injection, DTOs, async calls where appropriate, and centralized validation/error handling. C# classes, methods, and properties use `PascalCase`; parameters and locals use `camelCase`; interfaces start with `I`. Angular files and folders use `kebab-case`; TypeScript classes/interfaces use `PascalCase`. Python modules and functions use `snake_case`; classes use `PascalCase`. SQL Server tables and columns should use singular `PascalCase`, with `Id` primary keys and `<EntityName>Id` foreign keys.

## Testing Guidelines

Add tests with each feature. Prioritize suitability scoring, authentication/RBAC, API behavior, and chronological ML data splitting. Do not fabricate model metrics; report MAE/RMSE only from reproducible evaluation scripts. Name tests after the behavior under test, for example `GenerateRecommendation_ReturnsRankedCrops`.

## Commit & Pull Request Guidelines

No Git history is present in this directory yet. Use concise imperative commits, such as `Add backend solution skeleton` or `Implement crop suitability scoring`. Pull requests should include a summary, tests run, schema/API changes, screenshots for UI changes, assumptions, and any dissertation-scope approvals needed.

## Security & Configuration Tips

Never commit passwords, API keys, private datasets, or real connection strings. Keep data sources and feature flags configurable. Do not add new crops, roles, suitability factors, climate risks, external providers, or deployment assumptions without explicit approval.
