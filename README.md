# AI-Driven Weather Forecasting and Crop Recommendation System

This repository contains the MSc prototype for Anuradhapura District. Development follows `.agents/skills/anuradhapura-ai-development/references/project-specification.md`.

## Phase

Current approved phase: **Phase 1 - Solution and project structure**.

Do not proceed to Phase 2 database foundation until Phase 1 is approved.

## Local Commands

```powershell
dotnet build src/backend/AnuradhapuraAI.slnx
cd src/frontend
npm install
npm start
cd ../forecasting-service
python -m pytest
```

The frontend and Python commands require dependencies to be installed first.

## Local JWT Secret

Phase 3 uses JWT bearer authentication. Do not commit real signing keys. For local backend work, set a development key with user secrets or an environment variable:

```powershell
cd src/backend/AnuradhapuraAI.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "<local-development-key-at-least-32-characters>"
```
