using AnuradhapuraAI.Application.Suitability;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Persistence;
using AnuradhapuraAI.Infrastructure.Suitability;
using Microsoft.EntityFrameworkCore;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class SuitabilityConfigurationProviderTests
{
    [Fact]
    public async Task GetActiveConfiguration_MapsRequirementFieldsToEngineContract()
    {
        await using var dbContext = CreateDbContext();
        await SeedApprovedCropsAsync(dbContext);
        dbContext.CropEnvironmentalRequirements.Add(new CropEnvironmentalRequirement
        {
            CropId = 1,
            VariableType = ApprovedVariableTypes.Temperature,
            MinimumValue = 20m,
            MaximumValue = 30m,
            AcceptableMinimumValue = 10m,
            AcceptableMaximumValue = 40m,
            Unit = "synthetic-test-unit",
            TimeBasis = ApprovedTimeBases.SevenDay,
            IsCompatibleWithSevenDayForecast = true,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var provider = new SuitabilityConfigurationProvider(dbContext);

        var snapshot = await provider.GetActiveConfigurationAsync();

        var paddy = snapshot.CropProfiles.Single(profile => profile.CropName == ApprovedCropNames.Paddy);
        Assert.NotNull(paddy.TemperatureRequirement);
        Assert.Equal(20m, paddy.TemperatureRequirement!.OptimalMinimum);
        Assert.Equal(30m, paddy.TemperatureRequirement.OptimalMaximum);
        Assert.Equal(10m, paddy.TemperatureRequirement.AcceptableMinimum);
        Assert.Equal(40m, paddy.TemperatureRequirement.AcceptableMaximum);
        Assert.Equal(ApprovedTimeBases.SevenDay, paddy.TemperatureRequirement.TimeBasis);
        Assert.True(paddy.TemperatureRequirement.IsCompatibleWithSevenDayForecast);
    }

    [Fact]
    public async Task GetActiveConfiguration_DoesNotMapIncompleteAcceptableRangeAsEvaluable()
    {
        await using var dbContext = CreateDbContext();
        await SeedApprovedCropsAsync(dbContext);
        dbContext.CropEnvironmentalRequirements.Add(new CropEnvironmentalRequirement
        {
            CropId = 1,
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
        await dbContext.SaveChangesAsync();

        var provider = new SuitabilityConfigurationProvider(dbContext);

        var snapshot = await provider.GetActiveConfigurationAsync();

        var paddy = snapshot.CropProfiles.Single(profile => profile.CropName == ApprovedCropNames.Paddy);
        Assert.Null(paddy.HumidityRequirement);
    }

    [Fact]
    public async Task GetActiveConfiguration_AnnualRainfallWithCompatibilityFalseIsNotScoredByEngine()
    {
        await using var dbContext = CreateDbContext();
        await SeedApprovedCropsAsync(dbContext);
        await SeedSameRequirementForAllCropsAsync(
            dbContext,
            ApprovedVariableTypes.Rainfall,
            ApprovedTimeBases.Annual,
            compatibleWithSevenDayForecast: false);

        var provider = new SuitabilityConfigurationProvider(dbContext);
        var engine = new CropSuitabilityEngine();
        var snapshot = await provider.GetActiveConfigurationAsync();

        var result = engine.Evaluate(new SuitabilityEvaluationRequest(
            Forecast: SevenDayForecast(),
            SoilType: null,
            CropProfiles: snapshot.CropProfiles,
            Weights: snapshot.Weights,
            CategoryThresholds: snapshot.CategoryThresholds,
            EnableClimateRiskIndicators: false));

        Assert.False(result.Succeeded);
        Assert.Equal(SuitabilityErrorCodes.InsufficientEvidence, result.ErrorCode);
    }

    [Fact]
    public async Task GetActiveConfiguration_ExplicitCompatibleSevenDayRequirementCanBeEvaluated()
    {
        await using var dbContext = CreateDbContext();
        await SeedApprovedCropsAsync(dbContext);
        await SeedSameRequirementForAllCropsAsync(
            dbContext,
            ApprovedVariableTypes.Rainfall,
            ApprovedTimeBases.SevenDay,
            compatibleWithSevenDayForecast: true);

        var provider = new SuitabilityConfigurationProvider(dbContext);
        var engine = new CropSuitabilityEngine();
        var snapshot = await provider.GetActiveConfigurationAsync();

        var result = engine.Evaluate(new SuitabilityEvaluationRequest(
            Forecast: SevenDayForecast(),
            SoilType: null,
            CropProfiles: snapshot.CropProfiles,
            Weights: snapshot.Weights,
            CategoryThresholds: snapshot.CategoryThresholds,
            EnableClimateRiskIndicators: false));

        Assert.True(result.Succeeded, result.Message);
        Assert.All(result.Value!.Crops, crop =>
        {
            Assert.True(crop.Rainfall.IsEvaluable);
            Assert.Equal(7m, crop.Rainfall.AggregatedValue);
        });
    }

    private static AnuradhapuraAiDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AnuradhapuraAiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AnuradhapuraAiDbContext(options);
    }

    private static async Task SeedApprovedCropsAsync(AnuradhapuraAiDbContext dbContext)
    {
        dbContext.Crops.AddRange(ApprovedCropNames.All.Select((crop, index) => new Crop
        {
            Id = index + 1,
            Name = crop,
            IsActive = true
        }));

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedSameRequirementForAllCropsAsync(
        AnuradhapuraAiDbContext dbContext,
        string variableType,
        string timeBasis,
        bool compatibleWithSevenDayForecast)
    {
        foreach (var crop in await dbContext.Crops.ToListAsync())
        {
            dbContext.CropEnvironmentalRequirements.Add(new CropEnvironmentalRequirement
            {
                CropId = crop.Id,
                VariableType = variableType,
                MinimumValue = 5m,
                MaximumValue = 8m,
                AcceptableMinimumValue = 0m,
                AcceptableMaximumValue = 15m,
                Unit = "synthetic-test-unit",
                TimeBasis = timeBasis,
                IsCompatibleWithSevenDayForecast = compatibleWithSevenDayForecast,
                IsActive = true
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static IReadOnlyList<SuitabilityForecastDay> SevenDayForecast()
    {
        var startDate = new DateOnly(2025, 1, 1);
        return Enumerable.Range(0, 7)
            .Select(offset => new SuitabilityForecastDay(
                startDate.AddDays(offset),
                Rainfall: 1m,
                Temperature: 26m,
                Humidity: 70m))
            .ToList();
    }
}
