# Phase 8A Schema Compatibility Review

## 1. Audit Scope

Status: **APPROVED / FROZEN DESIGN; IMPLEMENTATION VERIFICATION SEPARATE**

This review records the approved schema compatibility decisions for the controlled Phase 8B schema foundation. Implementation and database verification are separate activities and must be reported after migration/testing.

This is a read-only design review. It does not implement Phase 8B, create migrations, change application source code, modify SQL Server schema/data, seed agricultural values, or change approved Phases 1-7.

Inspected sources:

- `AGENTS.md`
- `.agents/skills/anuradhapura-ai-development/`
- `docs/CODEX_HANDOVER.md`
- `docs/PROJECT_SHARING_CHECKLIST.md`
- `docs/PHASE7_SUITABILITY_SPECIFICATION.md`
- `docs/PHASE8_RECOMMENDATION_WORKFLOW_SPECIFICATION.md`
- Domain entities under `src/backend/AnuradhapuraAI.Domain/Entities/`
- EF configurations under `src/backend/AnuradhapuraAI.Infrastructure/Persistence/Configuration/`
- EF migrations and model snapshot under `src/backend/AnuradhapuraAI.Infrastructure/Persistence/Migrations/`
- Phase 6 forecasting service and DTOs
- Phase 7B suitability DTOs and engine
- Admin DTOs and admin recommendation read service
- Development SQL Server schema and row counts using read-only queries

## 2. Frozen Phase 8A Decisions

Status: **APPROVED / FROZEN**

1. Use the latest complete, valid, persisted seven-day forecast set. Never mix forecast runs.
2. `SoilType` is optional. Missing/unmatched soil is not evaluable and existing Phase 7B normalization applies.
3. Always represent all six approved crops in the response: Paddy, Maize, Green Gram, Cowpea, Groundnut, Chilli.
4. Rank evaluable crops by `OverallScore` descending. Tie-break by evidence coverage, then fixed approved crop order. Clearly identify partial evidence. Insufficient-evidence crops receive no rank.
5. Persist public and registered recommendations using existing `Recommendation` structures. Public `UserId` is nullable and public history is unavailable.
6. Registered users can retrieve only their own recommendation history and details.
7. Unevaluable factor scores are nullable. For insufficient evidence, `OverallScore`, `SuitabilityCategory`, and `Rank` are nullable.

This review does not reopen these decisions.

### Frozen Schema Decisions

1. Add `ForecastRunId` to `ForecastRecord` and `Recommendation`; do not add a separate `ForecastRun` table.
2. Make factor scores, `OverallScore`, `SuitabilityCategory`, and `Rank` nullable on `RecommendationCrop`.
3. Add `EvaluationStatus` with exactly `Complete`, `Partial`, and `InsufficientEvidence`.
4. Add versioned JSON evidence snapshots on `Recommendation` and `RecommendationCrop`.
5. Use a filtered unique rank index; retain uniqueness of `RecommendationId + CropId`.
6. Support public recommendation persistence; do not add a public historical retrieval/token system in this phase.

## 3. Existing Entity/Schema Inventory

Status: **INSPECTED**

### Domain Entities

`Recommendation` currently has:

- `Id`
- nullable `UserId`
- `CreatedAt`
- navigation to `User`
- collection of `RecommendationCrop`
- collection of `RecommendationValidation`

`RecommendationCrop` currently has:

- `Id`
- `RecommendationId`
- `CropId`
- non-nullable `decimal RainfallScore`
- non-nullable `decimal TemperatureScore`
- non-nullable `decimal HumidityScore`
- non-nullable `decimal SoilScore`
- non-nullable `decimal OverallScore`
- non-nullable `string SuitabilityCategory`
- non-nullable `string Explanation`
- non-nullable `int Rank`

`ForecastRecord` currently has:

- `Id`
- `ForecastDate`
- `TargetDate`
- `Rainfall`
- `Temperature`
- `Humidity`
- nullable `ModelVersion`
- `CreatedAt`

`RecommendationValidation` currently points to `Recommendation` and an officer `User`.

`User`, `Crop`, `CropEnvironmentalRequirement`, `SoilCompatibility`, and `SuitabilityConfiguration` match the approved Phases 2, 4, and 7B configuration model.

### SQL Server Schema

Read-only schema verification showed:

| Table | Important Current Columns |
| --- | --- |
| `ForecastRecord` | `Id int NOT NULL`, `ForecastDate date NOT NULL`, `TargetDate date NOT NULL`, `Rainfall decimal(10,2) NOT NULL`, `Temperature decimal(5,2) NOT NULL`, `Humidity decimal(5,2) NOT NULL`, `ModelVersion nvarchar(100) NULL`, `CreatedAt datetimeoffset NOT NULL` |
| `Recommendation` | `Id int NOT NULL`, `UserId int NULL`, `CreatedAt datetimeoffset NOT NULL` |
| `RecommendationCrop` | factor scores `decimal(5,2) NOT NULL`, `OverallScore decimal(5,2) NOT NULL`, `SuitabilityCategory nvarchar(50) NOT NULL`, `Explanation nvarchar(1000) NOT NULL`, `Rank int NOT NULL` |
| `RecommendationValidation` | `RecommendationId int NOT NULL`, `OfficerUserId int NOT NULL`, `Status nvarchar(50) NOT NULL`, `Comment nvarchar(1000) NULL`, `CreatedAt datetimeoffset NOT NULL` |

Current indexes/constraints relevant to Phase 8:

- `IX_ForecastRecord_ForecastDate_TargetDate` is non-unique.
- `IX_Recommendation_UserId` exists.
- `IX_RecommendationCrop_RecommendationId_CropId` is unique.
- `IX_RecommendationCrop_RecommendationId_Rank` is unique.
- `CK_RecommendationCrop_SuitabilityCategory` allows only the four approved suitability categories.
- `Recommendation.UserId` uses `ON DELETE SET NULL`.
- `RecommendationCrop.RecommendationId` and `RecommendationCrop.CropId` use restricted deletes.
- `RecommendationValidation.RecommendationId` and `OfficerUserId` use restricted deletes.

## 4. Forecast Run Provenance Analysis

Status: **APPROVED CORRECTION**

The current `ForecastRecord` schema does **not** contain a reliable forecast-run identity.

Current fields that might appear useful:

- `ForecastDate`
- `TargetDate`
- `ModelVersion`
- `CreatedAt`

Phase 6 currently saves seven forecast rows in one `SaveChangesAsync` call and assigns the same `CreatedAt` value to those rows. However, this is an implementation convention, not an explicit persistent run identity. It is not enforced by the database, not exposed as a run identifier, and cannot safely support the frozen rule "never mix forecast runs" over time.

### Why Existing Fields Are Insufficient

- `ForecastDate + TargetDate` can identify a forecast day, not the run that produced it.
- `ForecastDate + ModelVersion` can group likely related rows, but a model can produce more than one forecast for the same date.
- `CreatedAt` currently behaves like a run timestamp, but it is not named or constrained as a run identifier.
- There is no unique constraint requiring exactly one row per target date within a run.
- There is no field on `Recommendation` to preserve which forecast run was used.

### Alternatives

#### Alternative A: Add `ForecastRunId` To `ForecastRecord` And `Recommendation`

Add a `uniqueidentifier ForecastRunId` grouping value to every `ForecastRecord` row produced by a single Phase 6 forecast generation. Add the same value to `Recommendation` so Phase 8 can persist provenance.

Pros:

- Smallest schema correction.
- Avoids a new table.
- Supports grouping exactly seven forecast rows without relying on timestamps.
- Development database currently has zero `ForecastRecord` rows, so no real backfill is needed in development.

Cons:

- No database-level foreign key can reference a group of many `ForecastRecord` rows directly.
- Run metadata such as generation time/model version still lives redundantly or is inferred from child rows.

#### Alternative B: Add Dedicated `ForecastRun` Parent Entity

Add a `ForecastRun` parent table with one row per run, and make `ForecastRecord.ForecastRunId` and `Recommendation.ForecastRunId` foreign keys.

Pros:

- Strongest referential integrity.
- Natural place for `ForecastDate`, `ModelVersion`, `CreatedAt`, and future run metadata.
- Cleanest audit model.

Cons:

- Adds a new table/domain entity.
- Larger than the minimum correction and affects the approved ten-entity foundation.
- Requires explicit owner approval as an architecture/schema expansion.

#### Alternative C: Reuse Existing Fields

Group by `ForecastDate`, `ModelVersion`, and `CreatedAt`.

Pros:

- No schema change.

Cons:

- Not reliable enough for the frozen Phase 8A rule.
- Does not give `Recommendation` durable provenance.
- Can accidentally mix or lose runs if duplicate dates/version/timestamps occur.

### Recommendation

Approved minimum correction: **Alternative A**.

Add `ForecastRunId uniqueidentifier` to `ForecastRecord` and `Recommendation`, with application-level validation that a selected run has exactly seven rows, consecutive target dates, one `ForecastDate`, one `ModelVersion`, finite weather values, and no duplicate target dates.

The project owner approved no separate `ForecastRun` parent table for this phase.

### Recommended Latest Forecast Selection Rule

Latest should mean **run creation time**, not target date and not server date at recommendation time.

For Alternative A:

1. Group `ForecastRecord` rows by `ForecastRunId`.
2. Exclude groups that do not contain exactly seven rows.
3. Exclude groups with duplicate `TargetDate` values.
4. Exclude groups whose target dates are not consecutive daily dates.
5. Exclude groups whose `ForecastDate`, `ModelVersion`, or weather values are inconsistent or invalid.
6. Order valid groups by max/min shared `CreatedAt` descending, then `ForecastRunId` for deterministic tie behavior.
7. Select the first group.

Do not group unrelated rows merely because dates are consecutive.

## 5. Nullable Factor-Score Analysis

Status: **APPROVED CORRECTION**

Phase 7B application results permit:

- nullable factor scores
- nullable overall score
- nullable suitability category
- unranked insufficient-evidence crops

Current `RecommendationCrop` persistence does not permit this.

Required current-to-future changes:

- `RainfallScore`: `decimal` / `decimal(5,2) NOT NULL` -> nullable.
- `TemperatureScore`: `decimal` / `decimal(5,2) NOT NULL` -> nullable.
- `HumidityScore`: `decimal` / `decimal(5,2) NOT NULL` -> nullable.
- `SoilScore`: `decimal` / `decimal(5,2) NOT NULL` -> nullable.
- `OverallScore`: `decimal` / `decimal(5,2) NOT NULL` -> nullable.
- `SuitabilityCategory`: `string` / `nvarchar(50) NOT NULL` -> nullable.
- `Rank`: `int` / `int NOT NULL` -> nullable.

The existing category check constraint must be changed so NULL is allowed:

```sql
[SuitabilityCategory] IS NULL OR [SuitabilityCategory] IN (...)
```

The existing unique rank index must be changed. SQL Server unique indexes allow only one NULL for a key combination, so a normal unique index on `(RecommendationId, Rank)` would block multiple insufficient-evidence crops with `Rank = NULL`.

Recommended filtered unique index:

```sql
CREATE UNIQUE INDEX IX_RecommendationCrop_RecommendationId_Rank
ON RecommendationCrop (RecommendationId, Rank)
WHERE Rank IS NOT NULL;
```

Impacted current code locations:

- `src/backend/AnuradhapuraAI.Domain/Entities/RecommendationCrop.cs`
- `src/backend/AnuradhapuraAI.Infrastructure/Persistence/Configuration/RecommendationCropConfiguration.cs`
- EF model snapshot and future migration
- `src/backend/AnuradhapuraAI.Application/Admin/AdminDtos.cs`
- `src/backend/AnuradhapuraAI.Infrastructure/Admin/AdminManagementService.cs`
- Tests that construct or assert `RecommendationCrop` rows

## 6. Evaluation Status Design

Status: **APPROVED / FROZEN**

The current schema cannot durably represent:

- `Complete`
- `Partial`
- `InsufficientEvidence`

This status belongs on `RecommendationCrop`, not on `Recommendation`, because evidence availability is crop-specific. A single recommendation may contain one crop with complete evidence, another with partial evidence, and another with insufficient evidence.

Approved durable field:

```text
RecommendationCrop.EvaluationStatus nvarchar(50) NOT NULL
```

Approved values:

- `Complete`
- `Partial`
- `InsufficientEvidence`

Recommended consistency rules:

- `Complete`: `OverallScore`, `SuitabilityCategory`, and `Rank` are normally non-null; all four factor scores are evaluable.
- `Partial`: `OverallScore`, `SuitabilityCategory`, and `Rank` may be non-null; at least one factor score is null and at least one factor score is non-null.
- `InsufficientEvidence`: all factor scores are null, `OverallScore` is null, `SuitabilityCategory` is null, and `Rank` is null.

Do not put status on `Recommendation` unless a later summary status is needed for querying. It can be derived from child crop rows for Phase 8B.

## 7. Historical Evidence Snapshot Design

Status: **APPROVED / FROZEN**

The current schema persists some final outputs but not enough evidence to explain historical recommendations after administrators change configuration.

### A. Data Already Persisted

- `Recommendation.UserId`
- `Recommendation.CreatedAt`
- `RecommendationCrop.CropId`
- factor scores, but currently only non-nullable
- `RecommendationCrop.OverallScore`
- `RecommendationCrop.SuitabilityCategory`
- `RecommendationCrop.Explanation`
- `RecommendationCrop.Rank`

### B. Data Derivable Safely From Immutable Historical Data

Only the crop name is safely derivable from `CropId`, assuming crop seed identity remains stable. Current active crop requirements, weights, thresholds, and soil compatibility are **not** safe historical evidence because administrators can change them.

### C. Data Lost Without Snapshot

- effective factor weights after normalization
- configured factor weights used
- category thresholds used
- factor evaluable/unavailable indicators
- aggregated weather values used by each factor
- climate-risk indicators emitted
- climate-risk thresholds used
- supplied soil type
- forecast run identity
- seven forecast values used
- model version used
- exact explanation components beyond the current 1000-character text

### Minimum Viable Snapshot

Recommended minimum:

On `Recommendation`:

- `ForecastRunId uniqueidentifier NOT NULL`
- `SoilType nvarchar(100) NULL`
- `ForecastSnapshotJson nvarchar(max) NOT NULL`

On `RecommendationCrop`:

- `EvaluationStatus nvarchar(50) NOT NULL`
- `EvidenceSnapshotJson nvarchar(max) NOT NULL`
- optionally increase `Explanation` to `nvarchar(2000)` or keep it as summary text and rely on JSON for detailed evidence

JSON snapshot should include:

- forecast date, target dates, rainfall, temperature, humidity, model version
- supplied soil type
- crop name at evaluation time
- per-factor score/evaluable state
- aggregated values
- configured and effective weights
- unavailable-factor reasons
- category thresholds used
- emitted climate risks
- explanation components

Recommended SQL Server type:

- `nvarchar(max)`
- optional `ISJSON(...) = 1` check constraint if the project wants database validation

Do not duplicate the LSTM artifact, scaler, or ERA5 dataset in recommendation records.

## 8. Public/Registered Ownership Design

Status: **APPROVED / FROZEN**

`Recommendation.UserId` is already nullable in CLR and SQL Server:

- CLR: `int? UserId`
- SQL: `int NULL`
- FK delete behavior: `ON DELETE SET NULL`

This supports public persisted recommendations with `UserId = NULL`.

Registered-user history must query:

```text
Recommendation.UserId == current authenticated user ID
```

Public users must not receive history. Public one-time recommendation generation can return the generated result immediately.

No public historical retrieval/token system is approved in this phase.

Existing admin read-only routes can continue to see all persisted recommendations under `AdministratorOnly`.

## 9. Ranking Persistence Design

Status: **APPROVED CORRECTION**

Ranking is scoped to one `Recommendation`.

Required behavior:

- evaluable crops receive rank values `1..N`
- insufficient-evidence crops receive `Rank = NULL`
- equal scores tie-break by evidence coverage and then approved crop order before assigning final rank numbers

Recommended DB constraints:

- `Rank int NULL`
- filtered unique index on `(RecommendationId, Rank)` where `Rank IS NOT NULL`
- keep unique index on `(RecommendationId, CropId)` so each recommendation has at most one row per crop

Optional consistency check constraints:

- `Rank IS NULL OR Rank >= 1`
- `OverallScore IS NULL OR (OverallScore >= 0 AND OverallScore <= 100)`
- score columns nullable but, when non-null, each must be `0..100`

Do not add ranking weights or new factors.

## 10. Existing-Data Migration Safety

Status: **DEVELOPMENT DATABASE HAS NO FORECAST/RECOMMENDATION DATA**

Read-only development database counts:

| Table | Count |
| --- | ---: |
| `Recommendation` | 0 |
| `RecommendationCrop` | 0 |
| `ForecastRecord` | 0 |
| `RecommendationValidation` | 0 |
| `Crop` | 6 |
| `CropEnvironmentalRequirement` | 0 |
| `SoilCompatibility` | 0 |
| `SuitabilityConfiguration` | 0 |
| `User` | 0 |
| `UserRole` | 3 |

Because `ForecastRecord`, `Recommendation`, and `RecommendationCrop` are empty in development, the proposed nullability and run-identity changes have low development migration risk.

For any non-development database that already contains forecast rows:

- Do not fabricate run identities.
- If rows were created before `ForecastRunId`, either leave `ForecastRunId` nullable for legacy rows or mark them as not eligible for Phase 8 recommendation generation.
- Do not backfill unrelated rows into fake runs based only on consecutive dates.

For any existing `RecommendationCrop` rows:

- Making score/category/rank fields nullable is non-destructive.
- Adding `EvaluationStatus NOT NULL` needs a default/backfill. Existing rows with all old required values can be backfilled as `Complete`.
- Adding JSON snapshot as `NOT NULL` would need a safe default for existing rows, or should initially be nullable before a controlled backfill. Development has no existing rows.

No migration file was generated during this review.

## 11. Exact Minimum Schema Change Table

Status: **APPROVED / FROZEN DESIGN**

| Entity/table | Existing property/column | Existing CLR type | Existing SQL type/nullability | Proposed property/column | Proposed CLR type | Proposed SQL type/nullability | Default value | Index/FK/check constraint changes | Reason | Required or optional | Compatibility/migration risk |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `ForecastRecord` | none | none | none | `ForecastRunId` | `Guid` | `uniqueidentifier NOT NULL` | Application-generated per forecast run | Add index `(ForecastRunId, TargetDate)` unique; optionally index `(ForecastRunId)` | Prevent mixing forecast runs | Required | Low in current dev DB; legacy rows need no fabricated backfill |
| `Recommendation` | none | none | none | `ForecastRunId` | `Guid` | `uniqueidentifier NOT NULL` | None; set when creating recommendation | Index `(ForecastRunId)` | Persist forecast provenance | Required | Low if no existing recommendations; existing rows need nullable-first or backfill strategy |
| `Recommendation` | none | none | none | `SoilType` | `string?` | `nvarchar(100) NULL` | NULL | none | Preserve supplied soil input | Required | Low |
| `Recommendation` | none | none | none | `ForecastSnapshotJson` | `string` | `nvarchar(max) NOT NULL` | None; create from selected forecast | Optional `ISJSON` check | Preserve historical forecast evidence | Required | Low in dev; existing rows need nullable-first/backfill |
| `RecommendationCrop` | `RainfallScore` | `decimal` | `decimal(5,2) NOT NULL` | `RainfallScore` | `decimal?` | `decimal(5,2) NULL` | NULL | Optional score range check allows NULL | Store unevaluable factor as NULL | Required | Low |
| `RecommendationCrop` | `TemperatureScore` | `decimal` | `decimal(5,2) NOT NULL` | `TemperatureScore` | `decimal?` | `decimal(5,2) NULL` | NULL | Optional score range check allows NULL | Store unevaluable factor as NULL | Required | Low |
| `RecommendationCrop` | `HumidityScore` | `decimal` | `decimal(5,2) NOT NULL` | `HumidityScore` | `decimal?` | `decimal(5,2) NULL` | NULL | Optional score range check allows NULL | Store unevaluable factor as NULL | Required | Low |
| `RecommendationCrop` | `SoilScore` | `decimal` | `decimal(5,2) NOT NULL` | `SoilScore` | `decimal?` | `decimal(5,2) NULL` | NULL | Optional score range check allows NULL | Store unevaluable factor as NULL | Required | Low |
| `RecommendationCrop` | `OverallScore` | `decimal` | `decimal(5,2) NOT NULL` | `OverallScore` | `decimal?` | `decimal(5,2) NULL` | NULL | Optional score range check allows NULL | Insufficient evidence has no overall score | Required | Low |
| `RecommendationCrop` | `SuitabilityCategory` | `string` | `nvarchar(50) NOT NULL` | `SuitabilityCategory` | `string?` | `nvarchar(50) NULL` | NULL | Replace check with NULL-or-approved-category | Insufficient evidence has no category | Required | Low |
| `RecommendationCrop` | `Rank` | `int` | `int NOT NULL` | `Rank` | `int?` | `int NULL` | NULL | Replace unique index with filtered unique index where `Rank IS NOT NULL`; optional `Rank >= 1` check | Insufficient evidence is unranked | Required | Low |
| `RecommendationCrop` | none | none | none | `EvaluationStatus` | `string` | `nvarchar(50) NOT NULL` | None; set by app | Check constraint for `Complete`, `Partial`, `InsufficientEvidence` | Durable evidence status | Required | Low in dev; existing rows backfill as `Complete` |
| `RecommendationCrop` | none | none | none | `EvidenceSnapshotJson` | `string` | `nvarchar(max) NOT NULL` | None; set by app | Optional `ISJSON` check | Historical explainability after config changes | Required | Low in dev; existing rows need nullable-first/backfill |
| `RecommendationCrop` | `Explanation` | `string` | `nvarchar(1000) NOT NULL` | `Explanation` | `string` | `nvarchar(1000 or 2000) NOT NULL` | Existing behavior | none | Human-readable summary | Optional increase | Low |
| Optional `ForecastRun` | none | none | none | new table | new entity | parent table with `Id uniqueidentifier PK` | Application-generated | FK from `ForecastRecord` and `Recommendation` | Stronger referential integrity | Optional alternative | Medium; adds entity/table |

Minimum mandatory set before Phase 8B: `ForecastRunId`, nullable recommendation crop scores/category/rank, `EvaluationStatus`, evidence snapshot fields, and rank/category constraint updates.

## 12. Implementation Impact Map

Status: **PROPOSED / NOT EDITED**

### Domain Entities

- `src/backend/AnuradhapuraAI.Domain/Entities/ForecastRecord.cs`
- `src/backend/AnuradhapuraAI.Domain/Entities/Recommendation.cs`
- `src/backend/AnuradhapuraAI.Domain/Entities/RecommendationCrop.cs`
- optional if approved: new `ForecastRun` entity

### EF Core Configuration

- `src/backend/AnuradhapuraAI.Infrastructure/Persistence/Configuration/ForecastRecordConfiguration.cs`
- `src/backend/AnuradhapuraAI.Infrastructure/Persistence/Configuration/RecommendationConfiguration.cs`
- `src/backend/AnuradhapuraAI.Infrastructure/Persistence/Configuration/RecommendationCropConfiguration.cs`
- optional if approved: new `ForecastRunConfiguration.cs`

### DbContext And Migration

- `src/backend/AnuradhapuraAI.Infrastructure/Persistence/AnuradhapuraAiDbContext.cs`
- new EF Core migration, only after owner approval
- `src/backend/AnuradhapuraAI.Infrastructure/Persistence/Migrations/AnuradhapuraAiDbContextModelSnapshot.cs`

### Application DTOs / Services

- new recommendation request/response DTOs in `AnuradhapuraAI.Application`
- new recommendation service interface/implementation
- `src/backend/AnuradhapuraAI.Application/Admin/AdminDtos.cs`
- `src/backend/AnuradhapuraAI.Application/Suitability/SuitabilityDtos.cs` may not need changes but will be consumed by mapping code

### Infrastructure Services

- `src/backend/AnuradhapuraAI.Infrastructure/Forecasting/ForecastService.cs`
- `src/backend/AnuradhapuraAI.Infrastructure/Suitability/SuitabilityConfigurationProvider.cs`
- new recommendation persistence/query service in Infrastructure
- `src/backend/AnuradhapuraAI.Infrastructure/Admin/AdminManagementService.cs`
- `src/backend/AnuradhapuraAI.Infrastructure/DependencyInjection.cs`

### API

- new `RecommendationsController`
- existing `AdminController` read DTO behavior may need nullable-aware response mapping
- authorization policies in `src/backend/AnuradhapuraAI.Api/Program.cs` likely remain unchanged

### Tests

- existing `tests/AnuradhapuraAI.Phase3Tests/ForecastEndpointTests.cs`
- existing `tests/AnuradhapuraAI.Phase3Tests/Phase6ControlledIntegrationTests.cs`
- existing `tests/AnuradhapuraAI.Phase3Tests/CropSuitabilityEngineTests.cs`
- existing `tests/AnuradhapuraAI.Phase3Tests/SuitabilityConfigurationProviderTests.cs`
- existing `tests/AnuradhapuraAI.Phase3Tests/Phase4AdminServiceTests.cs`
- new Phase 8 recommendation workflow tests
- new migration/schema integration tests

## 13. Alternatives And Trade-Offs

Status: **PROPOSED**

### Forecast Provenance

- Reuse `CreatedAt`: no schema change, but not reliable enough.
- Add `ForecastRunId`: smallest reliable run grouping, recommended minimum.
- Add `ForecastRun` table: best referential integrity, but larger schema expansion.

Recommendation: approve `ForecastRunId` as minimum. Approve `ForecastRun` only if DB-level forecast-run referential integrity is required now.

### Evidence Snapshot

- Structured columns for every evidence item: queryable but wide and migration-heavy.
- JSON snapshot: compact, flexible, preserves historical evidence, less queryable.
- Explanation text only: too lossy.

Recommendation: JSON snapshots plus existing summary fields.

### Evaluation Status

- Derive from nullable fields only: no extra column, but historical intent is less clear.
- Store `EvaluationStatus` on `RecommendationCrop`: small, durable, crop-specific.
- Store status on `Recommendation`: too coarse for mixed-evidence crops.

Recommendation: store `EvaluationStatus` on `RecommendationCrop`.

### Public Access

- Immediate response only: no extra schema, safest.
- Sequential ID retrieval: not safe for public data.
- Opaque public token hash: safe if public retrieval is needed.

Recommendation: immediate public response only for Phase 8B unless owner approves token-based retrieval.

## 14. Test Plan

Status: **PROPOSED**

### Unit Tests

- latest complete forecast run selection uses one `ForecastRunId`
- incomplete forecast runs are excluded
- duplicate target dates are excluded
- target date gaps are excluded
- tie-break order is `OverallScore`, evidence coverage, fixed crop order
- insufficient-evidence crops have null rank
- evaluation status is `Complete`, `Partial`, or `InsufficientEvidence`
- public recommendations have nullable `UserId`
- registered history filters by current user

### EF Integration Tests

- nullable factor scores persist as NULL
- nullable `OverallScore`, `SuitabilityCategory`, and `Rank` persist as NULL
- filtered unique rank index permits multiple NULL ranks per recommendation
- filtered unique rank index rejects duplicate non-null ranks per recommendation
- unique `(RecommendationId, CropId)` still prevents duplicate crop rows
- `ForecastRunId + TargetDate` uniqueness prevents duplicate target dates in one run
- JSON snapshot fields persist and, if check constraints are approved, reject invalid JSON
- admin recommendation read DTOs handle nullable values

### SQL Server Migration Verification

- migration applies to empty development database
- migration script contains no destructive table drops
- no agricultural values are seeded
- existing seed crops and roles remain unchanged
- development row counts remain expected
- no existing forecast/recommendation rows are assigned fabricated run identities

### Regression Tests

- Phase 6 forecast endpoint still persists seven forecast records
- Phase 7B suitability engine math remains unchanged
- Phase 7B missing-factor normalization remains unchanged
- authentication/RBAC tests still pass
- Python forecasting tests still pass

## 15. Owner Approval Checklist

Status: **APPROVED / FROZEN**

Approved checklist:

1. Add `ForecastRunId` to `ForecastRecord`.
2. Add `ForecastRunId` to `Recommendation`.
3. Use run creation time (`CreatedAt`) for latest valid run ordering after grouping by `ForecastRunId`.
4. Keep no dedicated `ForecastRun` parent table, or approve adding one for stronger referential integrity.
5. Make `RecommendationCrop` factor scores nullable.
6. Make `RecommendationCrop.OverallScore`, `SuitabilityCategory`, and `Rank` nullable.
7. Replace rank unique index with filtered unique index where `Rank IS NOT NULL`.
8. Add `RecommendationCrop.EvaluationStatus`.
9. Add JSON evidence snapshots.
10. Add `Recommendation.SoilType`.
11. Do not add public retrieval token support in this phase.
12. Do not seed agricultural numeric values during the schema correction.

## 16. Explicit Status

Status: **APPROVED / FROZEN DESIGN; CONTROLLED SCHEMA FOUNDATION IMPLEMENTATION SEPARATE**

This document is an exact schema compatibility and migration design review only.

No code was modified, no migration was generated, no SQL write was performed, no approved model/dataset was changed, no agricultural values were invented or seeded, and Phase 8B was not started.
