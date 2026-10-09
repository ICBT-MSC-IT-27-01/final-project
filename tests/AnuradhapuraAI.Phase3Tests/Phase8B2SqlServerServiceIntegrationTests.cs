using System.Text.Json;
using AnuradhapuraAI.Application.Recommendations;
using AnuradhapuraAI.Application.Suitability;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Persistence;
using AnuradhapuraAI.Infrastructure.Recommendations;
using Microsoft.EntityFrameworkCore;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class Phase8B2SqlServerServiceIntegrationTests
{
    private const string EnabledVariable = "ANURADHAPURA_RUN_PHASE8B2_SQLSERVER_SERVICE_TESTS";
    private const string ConnectionVariable = "ANURADHAPURA_PHASE8B2_SQLSERVER_CONNECTION";
    private const string ExpectedDatabase = "AnuradhapuraAI_Phase8B2_Test";
    private const string ProtectedDatabase = "AnuradhapuraAI";
    private const string EvidenceSchemaVersion = "phase8b2-evidence-v1";

    [Fact]
    public async Task SqlServerServiceIntegration_PersistsPublicRecommendationThroughApplicationService()
    {
        if (!IsIntegrationTestEnabled())
        {
            return;
        }

        var runId = RequireRunId();
        await using var context = await CreateVerifiedContextAsync();
        var evaluation = CreateMixedEvaluation(runId, Guid.NewGuid());

        var result = await CreateService(context, evaluation)
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest($"Synthetic-{runId}-public"));

        Assert.True(result.Succeeded, result.Message);
        var recommendation = await context.Recommendations
            .AsNoTracking()
            .Include(item => item.RecommendationCrops)
            .SingleAsync(item => item.Id == result.Value!.RecommendationId);

        Assert.Null(recommendation.UserId);
        Assert.Equal(evaluation.Forecast.ForecastRunId, recommendation.ForecastRunId);
        Assert.Equal(6, recommendation.RecommendationCrops.Count);
        Assert.Equal([1, 2, 3, 4], recommendation.RecommendationCrops
            .Where(item => item.Rank.HasValue)
            .Select(item => item.Rank!.Value)
            .Order()
            .ToArray());
        Assert.Equal(2, recommendation.RecommendationCrops.Count(item => item.Rank is null));
        AssertSnapshotSchema(recommendation.EvidenceSnapshotJson);
        Assert.All(recommendation.RecommendationCrops, crop => AssertSnapshotSchema(crop.EvidenceSnapshotJson));
    }

    [Fact]
    public async Task SqlServerServiceIntegration_PersistsRegisteredUserAndRejectsInvalidUserContexts()
    {
        if (!IsIntegrationTestEnabled())
        {
            return;
        }

        var runId = RequireRunId();
        await using var context = await CreateVerifiedContextAsync();
        var registeredUser = await AddSyntheticUserAsync(context, runId, ApprovedRoleNames.RegisteredUser, isActive: true);
        var inactiveUser = await AddSyntheticUserAsync(context, runId, ApprovedRoleNames.RegisteredUser, isActive: false);
        var officerUser = await AddSyntheticUserAsync(context, runId, ApprovedRoleNames.AgriculturalOfficer, isActive: true);
        var adminUser = await AddSyntheticUserAsync(context, runId, ApprovedRoleNames.Administrator, isActive: true);
        var evaluation = CreateAllInsufficientEvaluation(runId, Guid.NewGuid());

        var valid = await CreateService(context, evaluation)
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest($"Synthetic-{runId}-registered", registeredUser.Id));
        var missing = await CreateService(context, evaluation)
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest(null, 999_999_999));
        var inactive = await CreateService(context, evaluation)
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest(null, inactiveUser.Id));
        var officer = await CreateService(context, evaluation)
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest(null, officerUser.Id));
        var admin = await CreateService(context, evaluation)
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest(null, adminUser.Id));

        Assert.True(valid.Succeeded, valid.Message);
        var persisted = await context.Recommendations.AsNoTracking().SingleAsync(item => item.Id == valid.Value!.RecommendationId);
        Assert.Equal(registeredUser.Id, persisted.UserId);
        Assert.Equal(RecommendationOrchestrationErrorCodes.InvalidUserContext, missing.ErrorCode);
        Assert.Equal(RecommendationOrchestrationErrorCodes.InvalidUserContext, inactive.ErrorCode);
        Assert.Equal(RecommendationOrchestrationErrorCodes.InvalidUserContext, officer.ErrorCode);
        Assert.Equal(RecommendationOrchestrationErrorCodes.InvalidUserContext, admin.ErrorCode);
    }

    [Fact]
    public async Task SqlServerServiceIntegration_RollsBackWhenChildPersistenceFails()
    {
        if (!IsIntegrationTestEnabled())
        {
            return;
        }

        var runId = RequireRunId();
        await using var context = await CreateVerifiedContextAsync();
        var evaluation = CreateMixedEvaluation(runId, Guid.NewGuid());
        var beforeRecommendations = await context.Recommendations.CountAsync();
        var beforeChildren = await context.RecommendationCrops.CountAsync();

        var result = await CreateService(
                context,
                evaluation,
                new DuplicateRankRecommendationRankingService())
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest($"Synthetic-{runId}-duplicate-rank"));

        Assert.False(result.Succeeded);
        Assert.Equal(RecommendationOrchestrationErrorCodes.PersistenceFailed, result.ErrorCode);
        Assert.Equal(beforeRecommendations, await context.Recommendations.CountAsync());
        Assert.Equal(beforeChildren, await context.RecommendationCrops.CountAsync());
    }

    [Fact]
    public async Task SqlServerServiceIntegration_PropagatesCancellationAndProtectsUnrelatedChanges()
    {
        if (!IsIntegrationTestEnabled())
        {
            return;
        }

        var runId = RequireRunId();
        await using var context = await CreateVerifiedContextAsync();
        var evaluation = CreateAllInsufficientEvaluation(runId, Guid.NewGuid());
        var beforeRecommendations = await context.Recommendations.CountAsync();

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateService(context, evaluation)
                .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest(null), cancellation.Token));
        Assert.Equal(beforeRecommendations, await context.Recommendations.CountAsync());

        context.Users.Add(new User
        {
            Name = $"Synthetic Pending {runId}",
            Email = $"pending-{runId}@example.test",
            PasswordHash = "synthetic-test-hash",
            RoleId = 1,
            IsActive = true,
            CreatedAt = DateTimeOffset.Parse("2026-10-09T00:00:00Z")
        });

        var result = await CreateService(context, evaluation)
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest(null));

        Assert.False(result.Succeeded);
        Assert.Equal(RecommendationOrchestrationErrorCodes.PersistenceFailed, result.ErrorCode);
        Assert.Equal(beforeRecommendations, await context.Recommendations.CountAsync());
        Assert.False(await context.Users.AsNoTracking().AnyAsync(user => user.Email == $"pending-{runId}@example.test"));
    }

    private static async Task<AnuradhapuraAiDbContext> CreateVerifiedContextAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("SQL Server service test connection was not provided.");
        }

        var options = new DbContextOptionsBuilder<AnuradhapuraAiDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        var context = new AnuradhapuraAiDbContext(options);

        var databaseName = await context.Database.SqlQueryRaw<string>("SELECT DB_NAME() AS Value").SingleAsync();
        if (!string.Equals(databaseName, ExpectedDatabase, StringComparison.Ordinal) ||
            string.Equals(databaseName, ProtectedDatabase, StringComparison.Ordinal))
        {
            await context.DisposeAsync();
            throw new InvalidOperationException("SQL Server service tests refused unsafe database target.");
        }

        var migrations = await context.Database
            .SqlQueryRaw<string>("SELECT MigrationId AS Value FROM dbo.__EFMigrationsHistory ORDER BY MigrationId")
            .ToListAsync();
        Assert.Equal(
            [
                "20261003084453_InitialDatabaseFoundation",
                "20261007105741_AddCropRequirementScoringMetadata",
                "20261009034224_AddPhase8RecommendationProvenanceAndEvidence"
            ],
            migrations);

        return context;
    }

    private static bool IsIntegrationTestEnabled() =>
        string.Equals(Environment.GetEnvironmentVariable(EnabledVariable), "true", StringComparison.OrdinalIgnoreCase);

    private static RecommendationPersistenceService CreateService(
        AnuradhapuraAiDbContext context,
        RecommendationEvaluationResponse evaluation,
        IRecommendationRankingService? rankingService = null) =>
        new(
            context,
            new StubRecommendationOrchestrationService(
                RecommendationOrchestrationResult<RecommendationEvaluationResponse>.Success(evaluation)),
            rankingService ?? new RecommendationRankingService(),
            new FixedTimeProvider(DateTimeOffset.Parse("2026-10-09T00:00:00Z")));

    private static async Task<User> AddSyntheticUserAsync(
        AnuradhapuraAiDbContext context,
        string runId,
        string roleName,
        bool isActive)
    {
        var role = await context.UserRoles.SingleAsync(item => item.Name == roleName);
        var user = new User
        {
            Name = $"Synthetic {roleName} {runId}",
            Email = $"{roleName.Replace(" ", "-", StringComparison.OrdinalIgnoreCase).ToLowerInvariant()}-{Guid.NewGuid():N}-{runId}@example.test",
            PasswordHash = "synthetic-test-hash",
            RoleId = role.Id,
            IsActive = isActive,
            CreatedAt = DateTimeOffset.Parse("2026-10-09T00:00:00Z")
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return user;
    }

    private static RecommendationEvaluationResponse CreateMixedEvaluation(string runId, Guid forecastRunId) =>
        CreateEvaluationResponse(runId, forecastRunId,
        [
            Crop(ApprovedCropNames.Paddy, 92m, ApprovedRecommendationEvaluationStatuses.Partial, 2, 50m),
            Crop(ApprovedCropNames.Maize, 85m, ApprovedRecommendationEvaluationStatuses.Partial, 1, 25m),
            Crop(ApprovedCropNames.GreenGram, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence),
            Crop(ApprovedCropNames.Cowpea, 72m, ApprovedRecommendationEvaluationStatuses.Partial, 3, 75m),
            Crop(ApprovedCropNames.Groundnut, 61m, ApprovedRecommendationEvaluationStatuses.Complete, 4, 100m),
            Crop(ApprovedCropNames.Chilli, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence)
        ]);

    private static RecommendationEvaluationResponse CreateAllInsufficientEvaluation(string runId, Guid forecastRunId) =>
        CreateEvaluationResponse(
            runId,
            forecastRunId,
            ApprovedCropNames.All
                .Select(cropName => Crop(cropName, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence))
                .ToList());

    private static RecommendationEvaluationResponse CreateEvaluationResponse(
        string runId,
        Guid forecastRunId,
        IReadOnlyList<CropRecommendationEvaluation> crops) =>
        new(
            new SelectedForecastRun(
                forecastRunId,
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 2),
                new DateOnly(2026, 1, 8),
                $"v1-synthetic-{runId}",
                DateTimeOffset.Parse("2026-10-09T00:00:00Z"),
                Enumerable.Range(1, 7)
                    .Select(day => new SuitabilityForecastDay(new DateOnly(2026, 1, 1).AddDays(day), 1m, 26m, 70m))
                    .ToList()),
            crops)
        {
            AppliedConfiguration = CreateAppliedConfiguration(runId)
        };

    private static CropRecommendationEvaluation Crop(
        string cropName,
        decimal? overallScore,
        string evaluationStatus,
        int? evaluatedFactorCount = null,
        decimal evaluatedWeight = 25m)
    {
        var isEvaluable = overallScore.HasValue;
        var coverageFactorCount = evaluatedFactorCount ?? (isEvaluable ? 1 : 0);
        var evaluableFactors = SuitabilityFactors.All
            .Take(isEvaluable ? coverageFactorCount : 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var factors = SuitabilityFactors.All
            .Select(factor => new SuitabilityFactorResult(
                factor,
                isEvaluable && evaluableFactors.Contains(factor),
                isEvaluable && evaluableFactors.Contains(factor) ? overallScore : null,
                isEvaluable && evaluableFactors.Contains(factor) && factor != SuitabilityFactors.Soil ? 26m : null,
                25m,
                isEvaluable && evaluableFactors.Contains(factor) ? 1m / coverageFactorCount : null,
                isEvaluable && evaluableFactors.Contains(factor)
                    ? "Synthetic SQL Server service factor was evaluated."
                    : "Synthetic SQL Server service factor was unavailable."))
            .ToList();

        return new CropRecommendationEvaluation(
            cropName,
            evaluationStatus,
            factors.Single(factor => factor.Factor == SuitabilityFactors.Rainfall).Score,
            factors.Single(factor => factor.Factor == SuitabilityFactors.Temperature).Score,
            factors.Single(factor => factor.Factor == SuitabilityFactors.Humidity).Score,
            factors.Single(factor => factor.Factor == SuitabilityFactors.Soil).Score,
            overallScore,
            overallScore.HasValue ? ApprovedSuitabilityCategories.Suitable : null,
            factors.Where(factor => !factor.IsEvaluable).Select(factor => factor.Factor).ToList(),
            factors,
            ClimateRisks: [],
            $"{cropName} synthetic SQL Server service explanation.",
            new EvidenceCoverage(coverageFactorCount, SuitabilityFactors.All.Length, isEvaluable ? evaluatedWeight : 0m));
    }

    private static AppliedRecommendationConfiguration CreateAppliedConfiguration(string runId) =>
        new(
            new SuitabilityFactorWeights(25m, 25m, 25m, 25m),
            new SuitabilityCategoryThresholds(80m, 60m, 40m),
            ApprovedCropNames.All
                .Select(cropName => new AppliedCropConfiguration(
                    cropName,
                    RainfallRequirement: null,
                    TemperatureRequirement: new AppliedRangeRequirement(
                        21m,
                        36m,
                        18m,
                        40m,
                        "deg C",
                        ApprovedTimeBases.Daily,
                        true),
                    HumidityRequirement: null,
                    SoilCompatibilities:
                    [
                        new AppliedSoilCompatibility($"Synthetic loam {runId}", 75m)
                    ]))
                .ToList());

    private static void AssertSnapshotSchema(string snapshotJson)
    {
        using var document = JsonDocument.Parse(snapshotJson);
        Assert.Equal(EvidenceSchemaVersion, document.RootElement.GetProperty("schemaVersion").GetString());
    }

    private static string RequireRunId() =>
        Environment.GetEnvironmentVariable("ANURADHAPURA_PHASE8B2_STEP3B_RUN_ID")
        ?? throw new InvalidOperationException("Step 3B test run identifier was not provided.");

    private sealed class StubRecommendationOrchestrationService(
        RecommendationOrchestrationResult<RecommendationEvaluationResponse> result) : IRecommendationOrchestrationService
    {
        public Task<RecommendationOrchestrationResult<RecommendationEvaluationResponse>> EvaluateLatestForecastAsync(
            RecommendationEvaluationRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(result);
        }
    }

    private sealed class DuplicateRankRecommendationRankingService : IRecommendationRankingService
    {
        public RankedRecommendationEvaluationResponse Rank(RecommendationEvaluationResponse evaluation) =>
            new(
                evaluation.Forecast,
                evaluation.Crops.Select(crop => new RankedCropRecommendationEvaluation(
                        crop.CropName,
                        crop.EvaluationStatus,
                        crop.RainfallScore,
                        crop.TemperatureScore,
                        crop.HumidityScore,
                        crop.SoilScore,
                        crop.OverallScore,
                        crop.SuitabilityCategory,
                        crop.OverallScore.HasValue ? 1 : null,
                        crop.UnavailableFactors,
                        crop.Factors,
                        crop.ClimateRisks,
                        crop.Explanation,
                        crop.EvidenceCoverage))
                    .ToList())
            {
                AppliedConfiguration = evaluation.AppliedConfiguration
            };
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
