using System.Text.Json;
using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Application.Recommendations;
using AnuradhapuraAI.Application.Suitability;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Persistence;
using AnuradhapuraAI.Infrastructure.Recommendations;
using AnuradhapuraAI.Infrastructure.Suitability;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class Phase8B2RecommendationPersistenceTests
{
    [Fact]
    public void Rank_OrdersByOverallScoreEvidenceCoverageAndApprovedCropOrder()
    {
        var service = new RecommendationRankingService();
        var response = CreateEvaluationResponse([
            Crop(ApprovedCropNames.Paddy, 80m, evaluatedFactorCount: 2, evaluatedWeight: 50m),
            Crop(ApprovedCropNames.Maize, 95m, evaluatedFactorCount: 1, evaluatedWeight: 25m),
            Crop(ApprovedCropNames.GreenGram, 80m, evaluatedFactorCount: 3, evaluatedWeight: 75m),
            Crop(ApprovedCropNames.Cowpea, 80m, evaluatedFactorCount: 3, evaluatedWeight: 75m),
            Crop(ApprovedCropNames.Groundnut, 70m, evaluatedFactorCount: 4, evaluatedWeight: 100m),
            Crop(ApprovedCropNames.Chilli, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence)
        ]);

        var ranked = service.Rank(response);

        Assert.Equal(1, ranked.Crops.Single(crop => crop.CropName == ApprovedCropNames.Maize).Rank);
        Assert.Equal(2, ranked.Crops.Single(crop => crop.CropName == ApprovedCropNames.GreenGram).Rank);
        Assert.Equal(3, ranked.Crops.Single(crop => crop.CropName == ApprovedCropNames.Cowpea).Rank);
        Assert.Equal(4, ranked.Crops.Single(crop => crop.CropName == ApprovedCropNames.Paddy).Rank);
        Assert.Equal(5, ranked.Crops.Single(crop => crop.CropName == ApprovedCropNames.Groundnut).Rank);
        Assert.Null(ranked.Crops.Single(crop => crop.CropName == ApprovedCropNames.Chilli).Rank);
    }

    [Fact]
    public void Rank_PartialEvaluationsRemainRankableAndInsufficientEvidenceIsUnranked()
    {
        var service = new RecommendationRankingService();
        var response = CreateEvaluationResponse([
            Crop(ApprovedCropNames.Paddy, 90m, ApprovedRecommendationEvaluationStatuses.Partial, evaluatedFactorCount: 1, evaluatedWeight: 25m),
            Crop(ApprovedCropNames.Maize, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence),
            Crop(ApprovedCropNames.GreenGram, 70m, ApprovedRecommendationEvaluationStatuses.Complete, evaluatedFactorCount: 4, evaluatedWeight: 100m),
            Crop(ApprovedCropNames.Cowpea, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence),
            Crop(ApprovedCropNames.Groundnut, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence),
            Crop(ApprovedCropNames.Chilli, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence)
        ]);

        var first = service.Rank(response);
        var second = service.Rank(response);

        Assert.Equal(1, first.Crops.Single(crop => crop.CropName == ApprovedCropNames.Paddy).Rank);
        Assert.Equal(2, first.Crops.Single(crop => crop.CropName == ApprovedCropNames.GreenGram).Rank);
        Assert.All(first.Crops.Where(crop => crop.EvaluationStatus == ApprovedRecommendationEvaluationStatuses.InsufficientEvidence), crop => Assert.Null(crop.Rank));
        Assert.Equal(first.Crops.Select(crop => crop.Rank), second.Crops.Select(crop => crop.Rank));
    }

    [Fact]
    public void Rank_AllInsufficientEvidenceCropsReceiveNoRanks()
    {
        var ranked = new RecommendationRankingService().Rank(CreateEvaluationResponse(ApprovedCropNames.All
            .Select(cropName => Crop(cropName, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence))
            .ToList()));

        Assert.All(ranked.Crops, crop =>
        {
            Assert.Null(crop.Rank);
            Assert.Null(crop.OverallScore);
            Assert.Null(crop.SuitabilityCategory);
        });
    }

    [Fact]
    public async Task CreateRecommendation_PersistsPublicRecommendationWithSixChildrenSnapshotsAndRanks()
    {
        await using var database = await CreateSqliteDatabaseAsync();
        await SeedApprovedCropsAsync(database.Context);
        var evaluation = CreateEvaluationResponse([
            Crop(ApprovedCropNames.Paddy, 88m, ApprovedRecommendationEvaluationStatuses.Partial, 2, 50m),
            Crop(ApprovedCropNames.Maize, 92m, ApprovedRecommendationEvaluationStatuses.Partial, 1, 25m),
            Crop(ApprovedCropNames.GreenGram, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence),
            Crop(ApprovedCropNames.Cowpea, 88m, ApprovedRecommendationEvaluationStatuses.Partial, 3, 75m),
            Crop(ApprovedCropNames.Groundnut, 60m, ApprovedRecommendationEvaluationStatuses.Complete, 4, 100m),
            Crop(ApprovedCropNames.Chilli, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence)
        ]);

        var result = await CreatePersistenceService(database.Context, evaluation)
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest("Synthetic loam"));

        Assert.True(result.Succeeded, result.Message);
        var recommendation = await database.Context.Recommendations
            .Include(item => item.RecommendationCrops)
            .SingleAsync();
        Assert.Null(recommendation.UserId);
        Assert.Equal(evaluation.Forecast.ForecastRunId, recommendation.ForecastRunId);
        Assert.Equal("Synthetic loam", recommendation.SoilType);
        Assert.Equal(6, recommendation.RecommendationCrops.Count);
        Assert.Equal(6, recommendation.RecommendationCrops.Select(crop => crop.CropId).Distinct().Count());
        Assert.Equal([1, 2, 3, 4], recommendation.RecommendationCrops.Where(crop => crop.Rank.HasValue).Select(crop => crop.Rank!.Value).Order().ToArray());
        Assert.Contains(recommendation.RecommendationCrops, crop => crop.Rank is null && crop.OverallScore is null && crop.SuitabilityCategory is null);
        AssertSnapshotHasSchemaVersion(recommendation.EvidenceSnapshotJson);
        Assert.All(recommendation.RecommendationCrops, crop => AssertSnapshotHasSchemaVersion(crop.EvidenceSnapshotJson));
    }

    [Fact]
    public async Task CreateRecommendation_PersistsValidRegisteredUserAssociation()
    {
        await using var database = await CreateSqliteDatabaseAsync();
        await SeedApprovedCropsAsync(database.Context);
        var user = await SeedRegisteredUserAsync(database.Context);

        var result = await CreatePersistenceService(database.Context, CreateAllInsufficientEvaluation())
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest(null, user.Id));

        Assert.True(result.Succeeded, result.Message);
        var recommendation = await database.Context.Recommendations.SingleAsync();
        Assert.Equal(user.Id, recommendation.UserId);
    }

    [Fact]
    public async Task CreateRecommendation_RejectsUntrustedOrNonRegisteredUserContext()
    {
        await using var database = await CreateSqliteDatabaseAsync();
        await SeedApprovedCropsAsync(database.Context);
        var officer = await SeedUserAsync(database.Context, ApprovedRoleNames.AgriculturalOfficer);

        var missingResult = await CreatePersistenceService(database.Context, CreateAllInsufficientEvaluation())
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest(null, 999));
        var officerResult = await CreatePersistenceService(database.Context, CreateAllInsufficientEvaluation())
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest(null, officer.Id));

        Assert.False(missingResult.Succeeded);
        Assert.Equal(RecommendationOrchestrationErrorCodes.InvalidUserContext, missingResult.ErrorCode);
        Assert.False(officerResult.Succeeded);
        Assert.Equal(RecommendationOrchestrationErrorCodes.InvalidUserContext, officerResult.ErrorCode);
        Assert.Empty(database.Context.Recommendations);
    }

    [Fact]
    public async Task CreateRecommendation_RejectsDuplicateCropEvaluationBeforePersistence()
    {
        await using var database = await CreateSqliteDatabaseAsync();
        await SeedApprovedCropsAsync(database.Context);
        var duplicateCropEvaluation = CreateEvaluationResponse([
            Crop(ApprovedCropNames.Paddy, 80m),
            Crop(ApprovedCropNames.Paddy, 79m),
            Crop(ApprovedCropNames.GreenGram, 78m),
            Crop(ApprovedCropNames.Cowpea, 77m),
            Crop(ApprovedCropNames.Groundnut, 76m),
            Crop(ApprovedCropNames.Chilli, 75m)
        ]);

        var result = await CreatePersistenceService(database.Context, duplicateCropEvaluation)
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest(null));

        Assert.False(result.Succeeded);
        Assert.Equal(RecommendationOrchestrationErrorCodes.InvalidConfiguration, result.ErrorCode);
        Assert.Empty(database.Context.Recommendations);
        Assert.Empty(database.Context.RecommendationCrops);
    }

    [Fact]
    public async Task CreateRecommendation_RejectsInsufficientEvidenceWithNonNullOverallScore()
    {
        await using var database = await CreateSqliteDatabaseAsync();
        await SeedApprovedCropsAsync(database.Context);
        var invalidEvaluation = CreateEvaluationResponse([
            Crop(ApprovedCropNames.Paddy, 10m, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence, evaluatedFactorCount: 0, evaluatedWeight: 0m),
            Crop(ApprovedCropNames.Maize, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence),
            Crop(ApprovedCropNames.GreenGram, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence),
            Crop(ApprovedCropNames.Cowpea, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence),
            Crop(ApprovedCropNames.Groundnut, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence),
            Crop(ApprovedCropNames.Chilli, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence)
        ]);

        var result = await CreatePersistenceService(database.Context, invalidEvaluation)
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest(null));

        Assert.False(result.Succeeded);
        Assert.Equal(RecommendationOrchestrationErrorCodes.InvalidConfiguration, result.ErrorCode);
        Assert.Empty(database.Context.Recommendations);
    }

    [Fact]
    public async Task CreateRecommendation_RejectsPartialStatusWithZeroEvaluableFactors()
    {
        await using var database = await CreateSqliteDatabaseAsync();
        await SeedApprovedCropsAsync(database.Context);
        var invalidEvaluation = CreateEvaluationResponse([
            Crop(ApprovedCropNames.Paddy, null, ApprovedRecommendationEvaluationStatuses.Partial, evaluatedFactorCount: 0, evaluatedWeight: 0m),
            Crop(ApprovedCropNames.Maize, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence),
            Crop(ApprovedCropNames.GreenGram, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence),
            Crop(ApprovedCropNames.Cowpea, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence),
            Crop(ApprovedCropNames.Groundnut, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence),
            Crop(ApprovedCropNames.Chilli, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence)
        ]);

        var result = await CreatePersistenceService(database.Context, invalidEvaluation)
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest(null));

        Assert.False(result.Succeeded);
        Assert.Equal(RecommendationOrchestrationErrorCodes.InvalidConfiguration, result.ErrorCode);
    }

    [Fact]
    public async Task CreateRecommendation_RejectsCompleteStatusWithMissingFactorEvidence()
    {
        await using var database = await CreateSqliteDatabaseAsync();
        await SeedApprovedCropsAsync(database.Context);
        var invalidEvaluation = CreateEvaluationResponse([
            Crop(ApprovedCropNames.Paddy, 82m, ApprovedRecommendationEvaluationStatuses.Complete, evaluatedFactorCount: 1, evaluatedWeight: 25m),
            Crop(ApprovedCropNames.Maize, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence),
            Crop(ApprovedCropNames.GreenGram, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence),
            Crop(ApprovedCropNames.Cowpea, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence),
            Crop(ApprovedCropNames.Groundnut, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence),
            Crop(ApprovedCropNames.Chilli, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence)
        ]);

        var result = await CreatePersistenceService(database.Context, invalidEvaluation)
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest(null));

        Assert.False(result.Succeeded);
        Assert.Equal(RecommendationOrchestrationErrorCodes.InvalidConfiguration, result.ErrorCode);
    }

    [Fact]
    public async Task CreateRecommendation_RejectsEvaluableFactorWithMissingOverallScore()
    {
        await using var database = await CreateSqliteDatabaseAsync();
        await SeedApprovedCropsAsync(database.Context);
        var invalidEvaluation = CreateEvaluationResponse([
            CropWithTemperatureScoreButMissingOverall(ApprovedCropNames.Paddy),
            Crop(ApprovedCropNames.Maize, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence),
            Crop(ApprovedCropNames.GreenGram, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence),
            Crop(ApprovedCropNames.Cowpea, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence),
            Crop(ApprovedCropNames.Groundnut, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence),
            Crop(ApprovedCropNames.Chilli, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence)
        ]);

        var result = await CreatePersistenceService(database.Context, invalidEvaluation)
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest(null));

        Assert.False(result.Succeeded);
        Assert.Equal(RecommendationOrchestrationErrorCodes.InvalidConfiguration, result.ErrorCode);
    }

    [Fact]
    public async Task CreateRecommendation_AllInsufficientEvidenceResultsPersistSafelyWithNullScoresRanksAndCategories()
    {
        await using var database = await CreateSqliteDatabaseAsync();
        await SeedApprovedCropsAsync(database.Context);

        var result = await CreatePersistenceService(database.Context, CreateAllInsufficientEvaluation())
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest(null));

        Assert.True(result.Succeeded, result.Message);
        var crops = await database.Context.RecommendationCrops.ToListAsync();
        Assert.Equal(6, crops.Count);
        Assert.All(crops, crop =>
        {
            Assert.Equal(ApprovedRecommendationEvaluationStatuses.InsufficientEvidence, crop.EvaluationStatus);
            Assert.Null(crop.RainfallScore);
            Assert.Null(crop.TemperatureScore);
            Assert.Null(crop.HumidityScore);
            Assert.Null(crop.SoilScore);
            Assert.Null(crop.OverallScore);
            Assert.Null(crop.SuitabilityCategory);
            Assert.Null(crop.Rank);
        });
    }

    [Fact]
    public async Task CreateRecommendation_SnapshotsCaptureUnavailableEvidenceForecastProvenanceAndSevenDaySemantics()
    {
        await using var database = await CreateSqliteDatabaseAsync();
        await SeedApprovedCropsAsync(database.Context);

        var result = await CreatePersistenceService(database.Context, CreateAllInsufficientEvaluation())
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest(null));

        Assert.True(result.Succeeded, result.Message);
        var recommendation = await database.Context.Recommendations.Include(item => item.RecommendationCrops).SingleAsync();
        using var parentJson = JsonDocument.Parse(recommendation.EvidenceSnapshotJson);
        Assert.Equal("phase8b2-evidence-v1", parentJson.RootElement.GetProperty("schemaVersion").GetString());
        Assert.Equal(80m, parentJson.RootElement.GetProperty("appliedConfiguration").GetProperty("categoryThresholds").GetProperty("highlySuitableMinimum").GetDecimal());
        Assert.Equal(25m, parentJson.RootElement.GetProperty("appliedConfiguration").GetProperty("weights").GetProperty("temperature").GetDecimal());
        Assert.True(parentJson.RootElement.GetProperty("forecast").TryGetProperty("forecastRunId", out _));
        Assert.Equal(7, parentJson.RootElement.GetProperty("forecast").GetProperty("days").GetArrayLength());

        using var cropJson = JsonDocument.Parse(recommendation.RecommendationCrops.First().EvidenceSnapshotJson);
        Assert.Equal("phase8b2-evidence-v1", cropJson.RootElement.GetProperty("schemaVersion").GetString());
        Assert.Equal(4, cropJson.RootElement.GetProperty("unavailableFactors").GetArrayLength());
        Assert.Equal("SevenDay", cropJson.RootElement.GetProperty("forecast").GetProperty("rainfallTimeBasis").GetString());
        Assert.Equal(21m, cropJson.RootElement.GetProperty("appliedRequirements").GetProperty("temperature").GetProperty("optimalMinimum").GetDecimal());
        Assert.Equal(36m, cropJson.RootElement.GetProperty("appliedRequirements").GetProperty("temperature").GetProperty("optimalMaximum").GetDecimal());
        Assert.Equal(18m, cropJson.RootElement.GetProperty("appliedRequirements").GetProperty("temperature").GetProperty("acceptableMinimum").GetDecimal());
        Assert.Equal(40m, cropJson.RootElement.GetProperty("appliedRequirements").GetProperty("temperature").GetProperty("acceptableMaximum").GetDecimal());
        Assert.Equal("Daily", cropJson.RootElement.GetProperty("appliedRequirements").GetProperty("temperature").GetProperty("timeBasis").GetString());
        Assert.True(cropJson.RootElement.GetProperty("appliedRequirements").GetProperty("temperature").GetProperty("isCompatibleWithSevenDayForecast").GetBoolean());
        Assert.Equal(JsonValueKind.Null, cropJson.RootElement.GetProperty("appliedRequirements").GetProperty("rainfall").ValueKind);
        Assert.Equal(recommendation.RecommendationCrops.First().EvaluationStatus, cropJson.RootElement.GetProperty("evaluationStatus").GetString());
        Assert.Equal(JsonValueKind.Null, cropJson.RootElement.GetProperty("rank").ValueKind);
        Assert.Contains("not a scientific confidence percentage", cropJson.RootElement.GetProperty("evidenceCoverage").GetProperty("note").GetString());
    }

    [Fact]
    public async Task CreateRecommendation_UsesPhase8B1LatestValidForecastSelectionWhenUsingRealOrchestration()
    {
        await using var database = await CreateSqliteDatabaseAsync();
        await SeedApprovedCropsAsync(database.Context);
        var olderRun = Guid.Parse("00000000-0000-0000-0000-000000000101");
        var newerRun = Guid.Parse("00000000-0000-0000-0000-000000000202");
        SeedForecastRun(database.Context, olderRun, new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        SeedForecastRun(database.Context, newerRun, new DateOnly(2026, 1, 8), DateTimeOffset.Parse("2026-01-02T00:00:00Z"));
        await database.Context.SaveChangesAsync();

        var result = await CreateRealPersistenceService(database.Context)
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest(null));

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(newerRun, result.Value!.ForecastRunId);
        Assert.Equal(newerRun, (await database.Context.Recommendations.SingleAsync()).ForecastRunId);
    }

    [Fact]
    public async Task CreateRecommendation_ReturnsForecastUnavailableWithoutPersistence()
    {
        await using var database = await CreateSqliteDatabaseAsync();
        await SeedApprovedCropsAsync(database.Context);

        var result = await CreateRealPersistenceService(database.Context)
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest(null));

        Assert.False(result.Succeeded);
        Assert.Equal(RecommendationOrchestrationErrorCodes.ForecastUnavailable, result.ErrorCode);
        Assert.Empty(database.Context.Recommendations);
        Assert.Empty(database.Context.RecommendationCrops);
    }

    [Fact]
    public async Task CreateRecommendation_ChildInsertionFailureRollsBackParentAndReturnsControlledResult()
    {
        await using var database = await CreateSqliteDatabaseAsync();
        await SeedApprovedCropsAsync(database.Context);
        await database.Context.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER "TR_RecommendationCrop_SyntheticFailure"
            BEFORE INSERT ON "RecommendationCrop"
            BEGIN
                SELECT RAISE(ABORT, 'synthetic child insert failure');
            END;
            """);

        var result = await CreatePersistenceService(database.Context, CreateAllInsufficientEvaluation())
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest(null));

        Assert.False(result.Succeeded);
        Assert.Equal(RecommendationOrchestrationErrorCodes.PersistenceFailed, result.ErrorCode);
        database.Context.ChangeTracker.Clear();
        Assert.Empty(await database.Context.Recommendations.ToListAsync());
        Assert.Empty(await database.Context.RecommendationCrops.ToListAsync());
    }

    [Fact]
    public async Task CreateRecommendation_CancellationDoesNotCreatePartialRecords()
    {
        await using var database = await CreateSqliteDatabaseAsync();
        await SeedApprovedCropsAsync(database.Context);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreatePersistenceService(database.Context, CreateAllInsufficientEvaluation())
                .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest(null), cancellation.Token));

        Assert.Empty(database.Context.Recommendations);
        Assert.Empty(database.Context.RecommendationCrops);
    }

    [Fact]
    public async Task CreateRecommendation_DoesNotCommitUnrelatedTrackedChanges()
    {
        await using var database = await CreateSqliteDatabaseAsync();
        await SeedApprovedCropsAsync(database.Context);
        database.Context.Crops.Add(new Crop { Name = "Unapproved Test Crop", IsActive = true });

        var result = await CreatePersistenceService(database.Context, CreateAllInsufficientEvaluation())
            .CreateRecommendationAsync(new CreateRecommendationPersistenceRequest(null));

        Assert.False(result.Succeeded);
        Assert.Equal(RecommendationOrchestrationErrorCodes.PersistenceFailed, result.ErrorCode);
        Assert.Empty(database.Context.Recommendations);
        Assert.DoesNotContain(await database.Context.Crops.AsNoTracking().ToListAsync(), crop => crop.Name == "Unapproved Test Crop");
    }

    private static RecommendationPersistenceService CreatePersistenceService(
        AnuradhapuraAiDbContext dbContext,
        RecommendationEvaluationResponse evaluation) =>
        new(
            dbContext,
            new StubRecommendationOrchestrationService(
                RecommendationOrchestrationResult<RecommendationEvaluationResponse>.Success(evaluation)),
            new RecommendationRankingService(),
            new FixedTimeProvider(DateTimeOffset.Parse("2026-10-09T00:00:00Z")));

    private static RecommendationPersistenceService CreateRealPersistenceService(AnuradhapuraAiDbContext dbContext) =>
        new(
            dbContext,
            new RecommendationOrchestrationService(
                dbContext,
                new SuitabilityConfigurationProvider(dbContext),
                new CropSuitabilityEngine(),
                Options.Create(new WeatherModelFeatureOptions())),
            new RecommendationRankingService(),
            new FixedTimeProvider(DateTimeOffset.Parse("2026-10-09T00:00:00Z")));

    private static RecommendationEvaluationResponse CreateAllInsufficientEvaluation() =>
        CreateEvaluationResponse(ApprovedCropNames.All
            .Select(cropName => Crop(cropName, null, ApprovedRecommendationEvaluationStatuses.InsufficientEvidence))
            .ToList());

    private static RecommendationEvaluationResponse CreateEvaluationResponse(IReadOnlyList<CropRecommendationEvaluation> crops) =>
        new RecommendationEvaluationResponse(
            new SelectedForecastRun(
                Guid.Parse("00000000-0000-0000-0000-000000008001"),
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 2),
                new DateOnly(2026, 1, 8),
                "v1",
                DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                Enumerable.Range(1, 7)
                    .Select(day => new SuitabilityForecastDay(new DateOnly(2026, 1, 1).AddDays(day), 1m, 26m, 70m))
                    .ToList()),
            crops)
        {
            AppliedConfiguration = CreateAppliedConfiguration()
        };

    private static CropRecommendationEvaluation Crop(
        string cropName,
        decimal? overallScore,
        string evaluationStatus = ApprovedRecommendationEvaluationStatuses.Partial,
        int? evaluatedFactorCount = null,
        decimal evaluatedWeight = 25m)
    {
        var isEvaluable = overallScore.HasValue;
        var coverageFactorCount = evaluatedFactorCount ?? (isEvaluable ? 1 : 0);
        var evaluableFactors = SuitabilityFactors.All
            .Take(isEvaluable ? coverageFactorCount : 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var factors = SuitabilityFactors.All.Select(factor => new SuitabilityFactorResult(
                factor,
                isEvaluable && evaluableFactors.Contains(factor),
                isEvaluable && evaluableFactors.Contains(factor) ? overallScore : null,
                isEvaluable && evaluableFactors.Contains(factor) && factor != SuitabilityFactors.Soil ? 26m : null,
                25m,
                isEvaluable && evaluableFactors.Contains(factor) ? 1m / coverageFactorCount : null,
                isEvaluable && evaluableFactors.Contains(factor)
                    ? "Synthetic test factor was evaluated."
                    : "Synthetic test factor was unavailable."))
            .ToList();

        return new CropRecommendationEvaluation(
            cropName,
            evaluationStatus,
            RainfallScore: factors.Single(factor => factor.Factor == SuitabilityFactors.Rainfall).Score,
            TemperatureScore: factors.Single(factor => factor.Factor == SuitabilityFactors.Temperature).Score,
            HumidityScore: factors.Single(factor => factor.Factor == SuitabilityFactors.Humidity).Score,
            SoilScore: factors.Single(factor => factor.Factor == SuitabilityFactors.Soil).Score,
            overallScore,
            overallScore.HasValue ? ApprovedSuitabilityCategories.Suitable : null,
            factors.Where(factor => !factor.IsEvaluable).Select(factor => factor.Factor).ToList(),
            factors,
            ClimateRisks: [],
            $"{cropName} synthetic explanation.",
            new EvidenceCoverage(coverageFactorCount, SuitabilityFactors.All.Length, isEvaluable ? evaluatedWeight : 0m));
    }

    private static CropRecommendationEvaluation CropWithTemperatureScoreButMissingOverall(string cropName)
    {
        var factors = SuitabilityFactors.All.Select(factor => new SuitabilityFactorResult(
                factor,
                factor == SuitabilityFactors.Temperature,
                factor == SuitabilityFactors.Temperature ? 70m : null,
                factor == SuitabilityFactors.Temperature ? 26m : null,
                25m,
                factor == SuitabilityFactors.Temperature ? 1m : null,
                factor == SuitabilityFactors.Temperature
                    ? "Synthetic test factor was evaluated."
                    : "Synthetic test factor was unavailable."))
            .ToList();

        return new CropRecommendationEvaluation(
            cropName,
            ApprovedRecommendationEvaluationStatuses.Partial,
            RainfallScore: null,
            TemperatureScore: 70m,
            HumidityScore: null,
            SoilScore: null,
            OverallScore: null,
            SuitabilityCategory: null,
            factors.Where(factor => !factor.IsEvaluable).Select(factor => factor.Factor).ToList(),
            factors,
            ClimateRisks: [],
            $"{cropName} synthetic invalid explanation.",
            new EvidenceCoverage(1, SuitabilityFactors.All.Length, 25m));
    }

    private static AppliedRecommendationConfiguration CreateAppliedConfiguration() =>
        new(
            new SuitabilityFactorWeights(25m, 25m, 25m, 25m),
            new SuitabilityCategoryThresholds(80m, 60m, 40m),
            ApprovedCropNames.All
                .Select(cropName => new AppliedCropConfiguration(
                    cropName,
                    RainfallRequirement: null,
                    TemperatureRequirement: new AppliedRangeRequirement(
                        OptimalMinimum: 21m,
                        OptimalMaximum: 36m,
                        AcceptableMinimum: 18m,
                        AcceptableMaximum: 40m,
                        Unit: "deg C",
                        TimeBasis: ApprovedTimeBases.Daily,
                        IsCompatibleWithSevenDayForecast: true),
                    HumidityRequirement: null,
                    SoilCompatibilities:
                    [
                        new AppliedSoilCompatibility("Synthetic loam", 75m)
                    ]))
                .ToList());

    private static async Task<TestDatabase> CreateSqliteDatabaseAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AnuradhapuraAiDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new AnuradhapuraAiDbContext(options);
        await CreateSqliteSchemaAsync(context);
        await SeedApprovedRolesAsync(context);
        return new TestDatabase(connection, context);
    }

    private static async Task CreateSqliteSchemaAsync(AnuradhapuraAiDbContext dbContext)
    {
        var sql = """
            PRAGMA foreign_keys = ON;

            CREATE TABLE "UserRole" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_UserRole" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL
            );
            CREATE UNIQUE INDEX "IX_UserRole_Name" ON "UserRole" ("Name");

            CREATE TABLE "Crop" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Crop" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL,
                "IsActive" INTEGER NOT NULL
            );
            CREATE UNIQUE INDEX "IX_Crop_Name" ON "Crop" ("Name");

            CREATE TABLE "User" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_User" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL,
                "Email" TEXT NOT NULL,
                "PasswordHash" TEXT NOT NULL,
                "RoleId" INTEGER NOT NULL,
                "IsActive" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                CONSTRAINT "FK_User_UserRole_RoleId" FOREIGN KEY ("RoleId") REFERENCES "UserRole" ("Id") ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX "IX_User_Email" ON "User" ("Email");

            CREATE TABLE "CropEnvironmentalRequirement" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_CropEnvironmentalRequirement" PRIMARY KEY AUTOINCREMENT,
                "CropId" INTEGER NOT NULL,
                "VariableType" TEXT NOT NULL,
                "MinimumValue" REAL NOT NULL,
                "MaximumValue" REAL NOT NULL,
                "AcceptableMinimumValue" REAL NULL,
                "AcceptableMaximumValue" REAL NULL,
                "Unit" TEXT NOT NULL,
                "TimeBasis" TEXT NULL,
                "IsCompatibleWithSevenDayForecast" INTEGER NOT NULL,
                "IsActive" INTEGER NOT NULL,
                CONSTRAINT "FK_CropEnvironmentalRequirement_Crop_CropId" FOREIGN KEY ("CropId") REFERENCES "Crop" ("Id") ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX "IX_CropEnvironmentalRequirement_CropId_VariableType" ON "CropEnvironmentalRequirement" ("CropId", "VariableType");

            CREATE TABLE "SoilCompatibility" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_SoilCompatibility" PRIMARY KEY AUTOINCREMENT,
                "CropId" INTEGER NOT NULL,
                "SoilType" TEXT NOT NULL,
                "CompatibilityScore" REAL NOT NULL,
                "IsActive" INTEGER NOT NULL,
                CONSTRAINT "FK_SoilCompatibility_Crop_CropId" FOREIGN KEY ("CropId") REFERENCES "Crop" ("Id") ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX "IX_SoilCompatibility_CropId_SoilType" ON "SoilCompatibility" ("CropId", "SoilType");

            CREATE TABLE "SuitabilityConfiguration" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_SuitabilityConfiguration" PRIMARY KEY AUTOINCREMENT,
                "ConfigurationType" TEXT NOT NULL,
                "ConfigurationKey" TEXT NOT NULL,
                "Value" REAL NOT NULL,
                "IsActive" INTEGER NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                "UpdatedByUserId" INTEGER NULL,
                CONSTRAINT "FK_SuitabilityConfiguration_User_UpdatedByUserId" FOREIGN KEY ("UpdatedByUserId") REFERENCES "User" ("Id") ON DELETE SET NULL
            );
            CREATE UNIQUE INDEX "IX_SuitabilityConfiguration_ConfigurationType_ConfigurationKey" ON "SuitabilityConfiguration" ("ConfigurationType", "ConfigurationKey");

            CREATE TABLE "ForecastRecord" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ForecastRecord" PRIMARY KEY AUTOINCREMENT,
                "ForecastRunId" TEXT NOT NULL,
                "ForecastDate" TEXT NOT NULL,
                "TargetDate" TEXT NOT NULL,
                "Rainfall" REAL NOT NULL,
                "Temperature" REAL NOT NULL,
                "Humidity" REAL NOT NULL,
                "ModelVersion" TEXT NULL,
                "CreatedAt" TEXT NOT NULL
            );
            CREATE INDEX "IX_ForecastRecord_ForecastDate_TargetDate" ON "ForecastRecord" ("ForecastDate", "TargetDate");
            CREATE UNIQUE INDEX "IX_ForecastRecord_ForecastRunId_TargetDate" ON "ForecastRecord" ("ForecastRunId", "TargetDate");

            CREATE TABLE "Recommendation" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Recommendation" PRIMARY KEY AUTOINCREMENT,
                "UserId" INTEGER NULL,
                "ForecastRunId" TEXT NOT NULL,
                "SoilType" TEXT NULL,
                "EvidenceSnapshotJson" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                CONSTRAINT "FK_Recommendation_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE SET NULL
            );
            CREATE INDEX "IX_Recommendation_ForecastRunId" ON "Recommendation" ("ForecastRunId");

            CREATE TABLE "RecommendationCrop" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_RecommendationCrop" PRIMARY KEY AUTOINCREMENT,
                "RecommendationId" INTEGER NOT NULL,
                "CropId" INTEGER NOT NULL,
                "RainfallScore" REAL NULL,
                "TemperatureScore" REAL NULL,
                "HumidityScore" REAL NULL,
                "SoilScore" REAL NULL,
                "OverallScore" REAL NULL,
                "SuitabilityCategory" TEXT NULL,
                "Explanation" TEXT NOT NULL,
                "Rank" INTEGER NULL,
                "EvaluationStatus" TEXT NOT NULL,
                "EvidenceSnapshotJson" TEXT NOT NULL,
                CONSTRAINT "CK_RecommendationCrop_Rank_Positive" CHECK ("Rank" IS NULL OR "Rank" >= 1),
                CONSTRAINT "CK_RecommendationCrop_Scores_Range" CHECK (("RainfallScore" IS NULL OR ("RainfallScore" >= 0 AND "RainfallScore" <= 100)) AND ("TemperatureScore" IS NULL OR ("TemperatureScore" >= 0 AND "TemperatureScore" <= 100)) AND ("HumidityScore" IS NULL OR ("HumidityScore" >= 0 AND "HumidityScore" <= 100)) AND ("SoilScore" IS NULL OR ("SoilScore" >= 0 AND "SoilScore" <= 100)) AND ("OverallScore" IS NULL OR ("OverallScore" >= 0 AND "OverallScore" <= 100))),
                CONSTRAINT "FK_RecommendationCrop_Crop_CropId" FOREIGN KEY ("CropId") REFERENCES "Crop" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_RecommendationCrop_Recommendation_RecommendationId" FOREIGN KEY ("RecommendationId") REFERENCES "Recommendation" ("Id") ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX "IX_RecommendationCrop_RecommendationId_CropId" ON "RecommendationCrop" ("RecommendationId", "CropId");
            CREATE UNIQUE INDEX "IX_RecommendationCrop_RecommendationId_Rank" ON "RecommendationCrop" ("RecommendationId", "Rank") WHERE "Rank" IS NOT NULL;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task SeedApprovedRolesAsync(AnuradhapuraAiDbContext dbContext)
    {
        dbContext.UserRoles.AddRange(
            new UserRole { Id = 1, Name = ApprovedRoleNames.RegisteredUser },
            new UserRole { Id = 2, Name = ApprovedRoleNames.AgriculturalOfficer },
            new UserRole { Id = 3, Name = ApprovedRoleNames.Administrator });
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedApprovedCropsAsync(AnuradhapuraAiDbContext dbContext)
    {
        dbContext.Crops.AddRange(ApprovedCropNames.All.Select((name, index) => new Crop
        {
            Id = index + 1,
            Name = name,
            IsActive = true
        }));
        await dbContext.SaveChangesAsync();
    }

    private static async Task<User> SeedRegisteredUserAsync(AnuradhapuraAiDbContext dbContext) =>
        await SeedUserAsync(dbContext, ApprovedRoleNames.RegisteredUser);

    private static async Task<User> SeedUserAsync(AnuradhapuraAiDbContext dbContext, string roleName)
    {
        var role = await dbContext.UserRoles.SingleAsync(item => item.Name == roleName);
        var user = new User
        {
            Name = $"{roleName} Test User",
            Email = $"{Guid.NewGuid():N}@example.test",
            PasswordHash = "synthetic-test-hash",
            RoleId = role.Id,
            IsActive = true,
            CreatedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z")
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    private static void SeedForecastRun(
        AnuradhapuraAiDbContext dbContext,
        Guid runId,
        DateOnly forecastDate,
        DateTimeOffset createdAt)
    {
        for (var index = 0; index < 7; index++)
        {
            dbContext.ForecastRecords.Add(new ForecastRecord
            {
                ForecastRunId = runId,
                ForecastDate = forecastDate,
                TargetDate = forecastDate.AddDays(index + 1),
                Rainfall = 1m,
                Temperature = 26m,
                Humidity = 70m,
                ModelVersion = "v1",
                CreatedAt = createdAt
            });
        }
    }

    private static void AssertSnapshotHasSchemaVersion(string snapshotJson)
    {
        using var document = JsonDocument.Parse(snapshotJson);
        Assert.Equal("phase8b2-evidence-v1", document.RootElement.GetProperty("schemaVersion").GetString());
    }

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

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class TestDatabase(
        SqliteConnection connection,
        AnuradhapuraAiDbContext context) : IAsyncDisposable
    {
        public AnuradhapuraAiDbContext Context { get; } = context;

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
