using System.Data.Common;
using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Application.Recommendations;
using AnuradhapuraAI.Application.Suitability;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Persistence;
using AnuradhapuraAI.Infrastructure.Recommendations;
using AnuradhapuraAI.Infrastructure.Suitability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class Phase8B1RecommendationOrchestrationTests
{
    [Fact]
    public async Task EvaluateLatestForecast_SelectsLatestCompleteValidRun()
    {
        await using var dbContext = CreateDbContext();
        await SeedApprovedCropsAsync(dbContext);
        var olderRun = Guid.Parse("00000000-0000-0000-0000-000000000101");
        var newerRun = Guid.Parse("00000000-0000-0000-0000-000000000202");
        SeedForecastRun(dbContext, olderRun, new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        SeedForecastRun(dbContext, newerRun, new DateOnly(2026, 1, 8), DateTimeOffset.Parse("2026-01-02T00:00:00Z"));
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).EvaluateLatestForecastAsync(new RecommendationEvaluationRequest(null));

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(newerRun, result.Value!.Forecast.ForecastRunId);
        Assert.Equal(new DateOnly(2026, 1, 9), result.Value.Forecast.TargetStartDate);
        Assert.Equal(new DateOnly(2026, 1, 15), result.Value.Forecast.TargetEndDate);
    }

    [Fact]
    public async Task EvaluateLatestForecast_SkipsIncompleteDuplicateAndNonConsecutiveRuns()
    {
        await using var dbContext = CreateDbContext();
        await SeedApprovedCropsAsync(dbContext);
        SeedForecastRun(dbContext, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-04T00:00:00Z"), count: 6);
        SeedForecastRun(dbContext, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-03T00:00:00Z"), duplicateTargetDate: true);
        SeedForecastRun(dbContext, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-02T00:00:00Z"), targetDateGap: true);
        var validRun = Guid.NewGuid();
        SeedForecastRun(dbContext, validRun, new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).EvaluateLatestForecastAsync(new RecommendationEvaluationRequest(null));

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(validRun, result.Value!.Forecast.ForecastRunId);
    }

    [Fact]
    public async Task EvaluateLatestForecast_DoesNotMixPartialRuns()
    {
        await using var dbContext = CreateDbContext();
        await SeedApprovedCropsAsync(dbContext);
        SeedForecastRun(dbContext, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-01T00:00:00Z"), count: 4);
        SeedForecastRun(dbContext, Guid.NewGuid(), new DateOnly(2026, 1, 4), DateTimeOffset.Parse("2026-01-02T00:00:00Z"), count: 3);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).EvaluateLatestForecastAsync(new RecommendationEvaluationRequest(null));

        Assert.False(result.Succeeded);
        Assert.Equal(RecommendationOrchestrationErrorCodes.ForecastUnavailable, result.ErrorCode);
    }

    [Fact]
    public async Task EvaluateLatestForecast_SkipsInvalidWeatherAndInconsistentMetadata()
    {
        await using var dbContext = CreateDbContext();
        await SeedApprovedCropsAsync(dbContext);
        SeedForecastRun(dbContext, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-04T00:00:00Z"), invalidHumidity: true);
        SeedForecastRun(dbContext, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-03T00:00:00Z"), inconsistentModelVersion: true);
        var validRun = Guid.NewGuid();
        SeedForecastRun(dbContext, validRun, new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-02T00:00:00Z"));
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).EvaluateLatestForecastAsync(new RecommendationEvaluationRequest(null));

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(validRun, result.Value!.Forecast.ForecastRunId);
    }

    [Fact]
    public async Task EvaluateLatestForecast_UsesDeterministicTieOrderingWithoutTreatingGuidAsRecency()
    {
        await using var dbContext = CreateDbContext();
        await SeedApprovedCropsAsync(dbContext);
        var sameCreatedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var firstTie = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var secondTie = Guid.Parse("00000000-0000-0000-0000-000000000002");
        SeedForecastRun(dbContext, secondTie, new DateOnly(2026, 1, 1), sameCreatedAt);
        SeedForecastRun(dbContext, firstTie, new DateOnly(2026, 2, 1), sameCreatedAt);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).EvaluateLatestForecastAsync(new RecommendationEvaluationRequest(null));

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(firstTie, result.Value!.Forecast.ForecastRunId);
    }

    [Fact]
    public async Task EvaluateLatestForecast_ReturnsForecastUnavailableWhenNoValidRunExists()
    {
        await using var dbContext = CreateDbContext();
        await SeedApprovedCropsAsync(dbContext);

        var result = await CreateService(dbContext).EvaluateLatestForecastAsync(new RecommendationEvaluationRequest(null));

        Assert.False(result.Succeeded);
        Assert.Equal(RecommendationOrchestrationErrorCodes.ForecastUnavailable, result.ErrorCode);
    }

    [Fact]
    public async Task EvaluateLatestForecast_DatabaseReadFailureReturnsControlledResult()
    {
        await using var dbContext = CreateDbContext();
        await SeedApprovedCropsAsync(dbContext);
        SeedForecastRun(dbContext, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        await dbContext.SaveChangesAsync();

        var result = await CreateService(
                dbContext,
                configurationProvider: new ThrowingSuitabilityConfigurationProvider(new TestDbException()))
            .EvaluateLatestForecastAsync(new RecommendationEvaluationRequest(null));

        Assert.False(result.Succeeded);
        Assert.Equal(RecommendationOrchestrationErrorCodes.DatabaseReadFailed, result.ErrorCode);
        Assert.DoesNotContain("connection", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(nameof(TestDbException), result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluateLatestForecast_DatabaseTimeoutReturnsControlledResult()
    {
        await using var dbContext = CreateDbContext();
        await SeedApprovedCropsAsync(dbContext);
        SeedForecastRun(dbContext, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        await dbContext.SaveChangesAsync();

        var result = await CreateService(
                dbContext,
                configurationProvider: new ThrowingSuitabilityConfigurationProvider(new TimeoutException("Synthetic database timeout.")))
            .EvaluateLatestForecastAsync(new RecommendationEvaluationRequest(null));

        Assert.False(result.Succeeded);
        Assert.Equal(RecommendationOrchestrationErrorCodes.DatabaseReadFailed, result.ErrorCode);
        Assert.Equal("Forecast or suitability configuration could not be read.", result.Message);
    }

    [Fact]
    public async Task EvaluateLatestForecast_CancellationIsNotConvertedToDatabaseFailure()
    {
        await using var dbContext = CreateDbContext();
        await SeedApprovedCropsAsync(dbContext);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateService(dbContext).EvaluateLatestForecastAsync(
                new RecommendationEvaluationRequest(null),
                cancellation.Token));
    }

    [Fact]
    public async Task EvaluateLatestForecast_ReturnsSixInsufficientEvidenceCropsWithoutPersistence()
    {
        await using var dbContext = CreateDbContext();
        await SeedApprovedCropsAsync(dbContext);
        SeedForecastRun(dbContext, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).EvaluateLatestForecastAsync(new RecommendationEvaluationRequest(null));

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(ApprovedCropNames.All, result.Value!.Crops.Select(crop => crop.CropName));
        Assert.All(result.Value.Crops, crop =>
        {
            Assert.Equal(ApprovedRecommendationEvaluationStatuses.InsufficientEvidence, crop.EvaluationStatus);
            Assert.Null(crop.OverallScore);
            Assert.Null(crop.SuitabilityCategory);
            Assert.Equal(SuitabilityFactors.All, crop.UnavailableFactors);
            Assert.Equal(0, crop.EvidenceCoverage.EvaluatedFactorCount);
            Assert.Equal(4, crop.EvidenceCoverage.TotalFactorCount);
        });
        Assert.Empty(dbContext.Recommendations);
        Assert.Empty(dbContext.RecommendationCrops);
    }

    [Fact]
    public async Task EvaluateLatestForecast_AllFourFactorsEvaluableReturnsCompleteEvaluation()
    {
        await using var dbContext = CreateDbContext();
        await SeedApprovedCropsAsync(dbContext);
        SeedForecastRun(dbContext, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        await SeedSameTemperatureRequirementAsync(dbContext);
        await SeedSameHumidityRequirementAsync(dbContext);
        await SeedSameSevenDayRainfallRequirementAsync(dbContext);
        await SeedSameSoilCompatibilityAsync(dbContext, "Synthetic loam", 75m);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).EvaluateLatestForecastAsync(new RecommendationEvaluationRequest("Synthetic loam"));

        Assert.True(result.Succeeded, result.Message);
        Assert.All(result.Value!.Crops, crop =>
        {
            Assert.Equal(ApprovedRecommendationEvaluationStatuses.Complete, crop.EvaluationStatus);
            Assert.True(crop.RainfallScore.HasValue);
            Assert.True(crop.TemperatureScore.HasValue);
            Assert.True(crop.HumidityScore.HasValue);
            Assert.True(crop.SoilScore.HasValue);
            Assert.True(crop.OverallScore.HasValue);
            Assert.NotNull(crop.SuitabilityCategory);
            Assert.Empty(crop.UnavailableFactors);
            Assert.Equal(4, crop.EvidenceCoverage.EvaluatedFactorCount);
            Assert.Equal(100m, crop.EvidenceCoverage.EvaluatedConfiguredWeightTotal);
        });
    }

    [Fact]
    public async Task EvaluateLatestForecast_UsesOptionalSoilTypeAndReportsPartialEvidence()
    {
        await using var dbContext = CreateDbContext();
        await SeedApprovedCropsAsync(dbContext);
        SeedForecastRun(dbContext, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        await SeedSameTemperatureRequirementAsync(dbContext);
        await SeedSameSoilCompatibilityAsync(dbContext, "Synthetic loam", 75m);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).EvaluateLatestForecastAsync(new RecommendationEvaluationRequest("Synthetic loam"));

        Assert.True(result.Succeeded, result.Message);
        Assert.All(result.Value!.Crops, crop =>
        {
            Assert.Equal(ApprovedRecommendationEvaluationStatuses.Partial, crop.EvaluationStatus);
            Assert.True(crop.TemperatureScore.HasValue);
            Assert.Equal(75m, crop.SoilScore);
            Assert.Null(crop.RainfallScore);
            Assert.Null(crop.HumidityScore);
            Assert.Equal(2, crop.EvidenceCoverage.EvaluatedFactorCount);
            Assert.Equal(50m, crop.EvidenceCoverage.EvaluatedConfiguredWeightTotal);
            Assert.Contains(SuitabilityFactors.Rainfall, crop.UnavailableFactors);
            Assert.Contains(SuitabilityFactors.Humidity, crop.UnavailableFactors);
        });
    }

    [Fact]
    public async Task EvaluateLatestForecast_MissingSoilCompatibilityKeepsSoilUnavailable()
    {
        await using var dbContext = CreateDbContext();
        await SeedApprovedCropsAsync(dbContext);
        SeedForecastRun(dbContext, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        await SeedSameTemperatureRequirementAsync(dbContext);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).EvaluateLatestForecastAsync(new RecommendationEvaluationRequest("Unknown soil"));

        Assert.True(result.Succeeded, result.Message);
        Assert.All(result.Value!.Crops, crop =>
        {
            Assert.Equal(ApprovedRecommendationEvaluationStatuses.Partial, crop.EvaluationStatus);
            Assert.Null(crop.SoilScore);
            Assert.Contains(SuitabilityFactors.Soil, crop.UnavailableFactors);
            Assert.Equal(1, crop.EvidenceCoverage.EvaluatedFactorCount);
        });
    }

    [Fact]
    public async Task EvaluateLatestForecast_ExcludesIncompleteAndAnnualRainfallRequirements()
    {
        await using var dbContext = CreateDbContext();
        await SeedApprovedCropsAsync(dbContext);
        SeedForecastRun(dbContext, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        await SeedSameAnnualRainfallRequirementAsync(dbContext);
        await SeedIncompleteHumidityRequirementAsync(dbContext);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).EvaluateLatestForecastAsync(new RecommendationEvaluationRequest(null));

        Assert.True(result.Succeeded, result.Message);
        Assert.All(result.Value!.Crops, crop =>
        {
            Assert.Equal(ApprovedRecommendationEvaluationStatuses.InsufficientEvidence, crop.EvaluationStatus);
            Assert.Null(crop.RainfallScore);
            Assert.Null(crop.HumidityScore);
            Assert.Contains(SuitabilityFactors.Rainfall, crop.UnavailableFactors);
            Assert.Contains(SuitabilityFactors.Humidity, crop.UnavailableFactors);
        });
    }

    [Fact]
    public async Task EvaluateLatestForecast_ReturnsConfigurationErrorForCorruptCropCatalog()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Crops.Add(new Crop { Id = 1, Name = ApprovedCropNames.Paddy, IsActive = true });
        SeedForecastRun(dbContext, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).EvaluateLatestForecastAsync(new RecommendationEvaluationRequest(null));

        Assert.False(result.Succeeded);
        Assert.Equal(RecommendationOrchestrationErrorCodes.InvalidConfiguration, result.ErrorCode);
    }

    [Fact]
    public async Task EvaluateLatestForecast_ClimateRiskFlagDisabledReturnsNoRisks()
    {
        await using var dbContext = CreateDbContext();
        await SeedApprovedCropsAsync(dbContext);
        SeedForecastRun(dbContext, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        await SeedSameTemperatureRequirementAsync(dbContext);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext, enableClimateRisks: false)
            .EvaluateLatestForecastAsync(new RecommendationEvaluationRequest(null));

        Assert.True(result.Succeeded, result.Message);
        Assert.All(result.Value!.Crops, crop => Assert.Empty(crop.ClimateRisks));
    }

    [Fact]
    public async Task EvaluateLatestForecast_SkipsInconsistentForecastDateAndCreatedAtRuns()
    {
        await using var dbContext = CreateDbContext();
        await SeedApprovedCropsAsync(dbContext);
        SeedForecastRun(dbContext, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-04T00:00:00Z"), inconsistentForecastDate: true);
        SeedForecastRun(dbContext, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-03T00:00:00Z"), inconsistentCreatedAt: true);
        var validRun = Guid.NewGuid();
        SeedForecastRun(dbContext, validRun, new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-02T00:00:00Z"));
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).EvaluateLatestForecastAsync(new RecommendationEvaluationRequest(null));

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(validRun, result.Value!.Forecast.ForecastRunId);
    }

    [Fact]
    public async Task EvaluateLatestForecast_SkipsNegativeRainfallRun()
    {
        await using var dbContext = CreateDbContext();
        await SeedApprovedCropsAsync(dbContext);
        SeedForecastRun(dbContext, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-02T00:00:00Z"), negativeRainfall: true);
        var validRun = Guid.NewGuid();
        SeedForecastRun(dbContext, validRun, new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).EvaluateLatestForecastAsync(new RecommendationEvaluationRequest(null));

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(validRun, result.Value!.Forecast.ForecastRunId);
    }

    private static RecommendationOrchestrationService CreateService(
        AnuradhapuraAiDbContext dbContext,
        bool enableClimateRisks = false,
        ISuitabilityConfigurationProvider? configurationProvider = null) =>
        new(
            dbContext,
            configurationProvider ?? new SuitabilityConfigurationProvider(dbContext),
            new CropSuitabilityEngine(),
            Options.Create(new WeatherModelFeatureOptions
            {
                EnableClimateRiskIndicators = enableClimateRisks
            }));

    private static AnuradhapuraAiDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AnuradhapuraAiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AnuradhapuraAiDbContext(options);
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

    private static void SeedForecastRun(
        AnuradhapuraAiDbContext dbContext,
        Guid runId,
        DateOnly forecastDate,
        DateTimeOffset createdAt,
        int count = 7,
        bool duplicateTargetDate = false,
        bool targetDateGap = false,
        bool invalidHumidity = false,
        bool inconsistentModelVersion = false,
        bool inconsistentForecastDate = false,
        bool inconsistentCreatedAt = false,
        bool negativeRainfall = false)
    {
        for (var index = 0; index < count; index++)
        {
            var targetOffset = targetDateGap && index == 6 ? 8 : index + 1;
            var targetDate = duplicateTargetDate && index == 6
                ? forecastDate.AddDays(1)
                : forecastDate.AddDays(targetOffset);

            dbContext.ForecastRecords.Add(new ForecastRecord
            {
                ForecastRunId = runId,
                ForecastDate = inconsistentForecastDate && index == 0 ? forecastDate.AddDays(-1) : forecastDate,
                TargetDate = targetDate,
                Rainfall = negativeRainfall && index == 0 ? -1m : 1m,
                Temperature = 26m,
                Humidity = invalidHumidity && index == 0 ? 101m : 70m,
                ModelVersion = inconsistentModelVersion && index == 0 ? "v0" : "v1",
                CreatedAt = inconsistentCreatedAt && index == 0 ? createdAt.AddMinutes(1) : createdAt
            });
        }
    }

    private static async Task SeedSameTemperatureRequirementAsync(AnuradhapuraAiDbContext dbContext)
    {
        foreach (var crop in await dbContext.Crops.ToListAsync())
        {
            dbContext.CropEnvironmentalRequirements.Add(new CropEnvironmentalRequirement
            {
                CropId = crop.Id,
                VariableType = ApprovedVariableTypes.Temperature,
                MinimumValue = 24m,
                MaximumValue = 30m,
                AcceptableMinimumValue = 20m,
                AcceptableMaximumValue = 34m,
                Unit = "synthetic-test-unit",
                TimeBasis = ApprovedTimeBases.SevenDay,
                IsCompatibleWithSevenDayForecast = true,
                IsActive = true
            });
        }
    }

    private static async Task SeedSameSoilCompatibilityAsync(
        AnuradhapuraAiDbContext dbContext,
        string soilType,
        decimal compatibilityScore)
    {
        foreach (var crop in await dbContext.Crops.ToListAsync())
        {
            dbContext.SoilCompatibilities.Add(new SoilCompatibility
            {
                CropId = crop.Id,
                SoilType = soilType,
                CompatibilityScore = compatibilityScore,
                IsActive = true
            });
        }
    }

    private static async Task SeedSameSevenDayRainfallRequirementAsync(AnuradhapuraAiDbContext dbContext)
    {
        foreach (var crop in await dbContext.Crops.ToListAsync())
        {
            dbContext.CropEnvironmentalRequirements.Add(new CropEnvironmentalRequirement
            {
                CropId = crop.Id,
                VariableType = ApprovedVariableTypes.Rainfall,
                MinimumValue = 5m,
                MaximumValue = 10m,
                AcceptableMinimumValue = 0m,
                AcceptableMaximumValue = 20m,
                Unit = "synthetic-test-unit",
                TimeBasis = ApprovedTimeBases.SevenDay,
                IsCompatibleWithSevenDayForecast = true,
                IsActive = true
            });
        }
    }

    private static async Task SeedSameHumidityRequirementAsync(AnuradhapuraAiDbContext dbContext)
    {
        foreach (var crop in await dbContext.Crops.ToListAsync())
        {
            dbContext.CropEnvironmentalRequirements.Add(new CropEnvironmentalRequirement
            {
                CropId = crop.Id,
                VariableType = ApprovedVariableTypes.Humidity,
                MinimumValue = 60m,
                MaximumValue = 80m,
                AcceptableMinimumValue = 40m,
                AcceptableMaximumValue = 90m,
                Unit = "synthetic-test-unit",
                TimeBasis = ApprovedTimeBases.SevenDay,
                IsCompatibleWithSevenDayForecast = true,
                IsActive = true
            });
        }
    }

    private static async Task SeedSameAnnualRainfallRequirementAsync(AnuradhapuraAiDbContext dbContext)
    {
        foreach (var crop in await dbContext.Crops.ToListAsync())
        {
            dbContext.CropEnvironmentalRequirements.Add(new CropEnvironmentalRequirement
            {
                CropId = crop.Id,
                VariableType = ApprovedVariableTypes.Rainfall,
                MinimumValue = 600m,
                MaximumValue = 1200m,
                AcceptableMinimumValue = 400m,
                AcceptableMaximumValue = 1800m,
                Unit = "synthetic-test-unit",
                TimeBasis = ApprovedTimeBases.Annual,
                IsCompatibleWithSevenDayForecast = false,
                IsActive = true
            });
        }
    }

    private static async Task SeedIncompleteHumidityRequirementAsync(AnuradhapuraAiDbContext dbContext)
    {
        foreach (var crop in await dbContext.Crops.ToListAsync())
        {
            dbContext.CropEnvironmentalRequirements.Add(new CropEnvironmentalRequirement
            {
                CropId = crop.Id,
                VariableType = ApprovedVariableTypes.Humidity,
                MinimumValue = 60m,
                MaximumValue = 80m,
                AcceptableMinimumValue = 40m,
                AcceptableMaximumValue = null,
                Unit = "synthetic-test-unit",
                TimeBasis = ApprovedTimeBases.SevenDay,
                IsCompatibleWithSevenDayForecast = true,
                IsActive = true
            });
        }
    }

    private sealed class ThrowingSuitabilityConfigurationProvider(Exception exception) : ISuitabilityConfigurationProvider
    {
        public Task<SuitabilityConfigurationSnapshot> GetActiveConfigurationAsync(CancellationToken cancellationToken = default) =>
            throw exception;
    }

    private sealed class TestDbException : DbException
    {
        public override string Message => "Synthetic database read failure.";
    }
}
