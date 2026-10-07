---
name: anuradhapura-ai-development
description: Develop and maintain the MSc AI-Driven Weather Forecasting and Crop Recommendation System for Anuradhapura District. Use this skill whenever planning, scaffolding, coding, reviewing, testing, refactoring, documenting, or integrating any part of this project so implementation stays aligned with the approved dissertation requirements, architecture, naming conventions, feature flags, ML workflow, and scope boundaries.
---

# Anuradhapura AI Development Skill

Use this skill for every software-development task in this MSc project.

## First action

1. Read `references/project-specification.md` before making design or implementation decisions.
2. Read `references/reference-file-timing.md` before asking for or using dissertation/diagram reference files.
3. If the requested work belongs to a specific phase, work only within that phase unless the user explicitly authorizes moving forward.
4. Inspect the repository before changing code.

## Source-of-truth rule

The approved project requirements, final dissertation decisions, and final diagrams supplied by the user are authoritative. If a repository assumption, generated design, or implementation idea conflicts with them, do not silently reconcile the conflict. Report it and request clarification.

Do not invent agricultural facts, numeric suitability weights, suitability thresholds, ML accuracy values, data sources, user roles, features, or deployment assumptions.

## Development behaviour

- Follow the approved technology stack and three-tier backend architecture.
- Keep controllers thin and business rules outside the presentation layer.
- Keep data-access concerns outside the business layer.
- Use dependency injection and consistent naming conventions.
- Use lightweight, configuration-driven feature flags only for approved functionality.
- Keep the system appropriate for an MSc prototype; avoid unnecessary enterprise complexity.
- Do not introduce out-of-scope features.
- Do not hard-code unverified agricultural configuration.
- Do not fabricate ML data or evaluation results.
- Keep time-series splitting chronological.
- Keep the ML weather model separate from the explainable crop-suitability engine.

## Phase control

Use this order unless the user explicitly changes it:

1. Solution and project structure
2. Database foundation
3. Authentication and RBAC
4. Admin configuration
5. Python forecasting-service skeleton
6. Backend forecast integration
7. Suitability engine
8. Recommendation flow
9. Agricultural Officer validation
10. Angular interfaces
11. Testing

Do not automatically jump to the next phase when a design decision or domain assumption requires approval.

## Required end-of-phase report

At the end of each phase, report:

- what was created or changed
- files changed
- database/schema changes
- APIs added or changed
- tests added or run
- feature flags affected
- assumptions made
- unresolved risks or TODOs
- anything requiring user approval

## Naming and consistency

Use the exact project terminology in `references/project-specification.md`. Keep domain names consistent between code, database, APIs, diagrams, and dissertation documentation.

## Reference materials

When the user provides final requirements or diagrams, treat them as supporting authoritative references for the relevant phase. Do not use unrelated example dissertations or lecturer teaching materials as software requirements unless the user explicitly asks.

## Reusable phase prompt

For a new phase, use `assets/phase-task-template.md` as a checklist and adapt it to the current repository state.
