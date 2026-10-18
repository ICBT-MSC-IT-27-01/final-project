using AnuradhapuraAI.Application.Suitability;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Persistence;
using AnuradhapuraAI.Infrastructure.Agriculture;
using Microsoft.EntityFrameworkCore;

namespace AnuradhapuraAI.Infrastructure.Suitability;

public sealed class SuitabilityConfigurationProvider(
    AnuradhapuraAiDbContext dbContext,
    CropEvidenceCatalog? cropEvidenceCatalog = null) : ISuitabilityConfigurationProvider
{
    public async Task<SuitabilityConfigurationSnapshot> GetActiveConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var crops = await dbContext.Crops
            .AsNoTracking()
            .Where(crop => crop.IsActive && ApprovedCropNames.All.Contains(crop.Name))
            .Include(crop => crop.EnvironmentalRequirements.Where(requirement => requirement.IsActive))
            .Include(crop => crop.SoilCompatibilities.Where(compatibility => compatibility.IsActive))
            .ToListAsync(cancellationToken);

        var configurations = await dbContext.SuitabilityConfigurations
            .AsNoTracking()
            .Where(configuration => configuration.IsActive)
            .ToListAsync(cancellationToken);

        var profiles = ApprovedCropNames.All
            .Select(cropName => crops.SingleOrDefault(crop => crop.Name == cropName))
            .Where(crop => crop is not null)
            .Select(crop => ToCropProfile(crop!, cropEvidenceCatalog))
            .ToList();

        return new SuitabilityConfigurationSnapshot(
            profiles,
            ToWeights(configurations),
            ToCategoryThresholds(configurations));
    }

    private static CropSuitabilityProfile ToCropProfile(Crop crop, CropEvidenceCatalog? cropEvidenceCatalog)
    {
        var databaseRequirements = crop.EnvironmentalRequirements
            .GroupBy(requirement => requirement.VariableType)
            .ToDictionary(group => group.Key, group => ToRangeRequirement(group.Single()));

        if (cropEvidenceCatalog is null ||
            !cropEvidenceCatalog.TryGetCrop(crop.Name, out var evidence))
        {
            return CreateProfile(crop, databaseRequirements, null);
        }

        var evidenceRequirements = evidence.Requirements
            .ToDictionary(requirement => requirement.Factor, requirement => requirement.ToSuitabilityRequirement());
        var profile = CreateProfile(crop, databaseRequirements, evidenceRequirements);
        var weatherRequirements = new[]
        {
            profile.RainfallRequirement,
            profile.TemperatureRequirement,
            profile.HumidityRequirement
        };

        return profile with
        {
            SupportsFullSuitabilityAssessment = weatherRequirements.All(requirement =>
                requirement is not null && requirement.SupportsFullSuitabilityAssessment),
            AssessmentBasis = evidence.AssessmentBasis,
            EvidenceSources =
            [
                evidence.CalendarEvidence.ToSuitabilitySource(),
                .. evidenceRequirements.Values.SelectMany(requirement => requirement.Sources)
            ],
            ScientificLimitations = evidence.ScientificLimitations
        };
    }

    private static CropSuitabilityProfile CreateProfile(
        Crop crop,
        IReadOnlyDictionary<string, SuitabilityRangeRequirement?> databaseRequirements,
        IReadOnlyDictionary<string, SuitabilityRangeRequirement>? evidenceRequirements)
    {
        var rainfall = GetRequirement(ApprovedVariableTypes.Rainfall, databaseRequirements, evidenceRequirements);
        var temperature = GetRequirement(ApprovedVariableTypes.Temperature, databaseRequirements, evidenceRequirements);
        var humidity = GetRequirement(ApprovedVariableTypes.Humidity, databaseRequirements, evidenceRequirements);

        return new CropSuitabilityProfile(
            crop.Name,
            rainfall,
            temperature,
            humidity,
            crop.SoilCompatibilities
                .Select(compatibility => new SuitabilitySoilCompatibility(
                    compatibility.SoilType,
                    compatibility.CompatibilityScore))
                .ToList())
        {
            SupportsFullSuitabilityAssessment = new[] { rainfall, temperature, humidity }
                .All(requirement => requirement is not null && requirement.SupportsFullSuitabilityAssessment)
        };
    }

    private static SuitabilityRangeRequirement? GetRequirement(
        string variableType,
        IReadOnlyDictionary<string, SuitabilityRangeRequirement?> databaseRequirements,
        IReadOnlyDictionary<string, SuitabilityRangeRequirement>? evidenceRequirements) =>
        databaseRequirements.GetValueOrDefault(variableType) ??
        evidenceRequirements?.GetValueOrDefault(variableType);

    private static SuitabilityRangeRequirement? ToRangeRequirement(CropEnvironmentalRequirement? requirement)
    {
        if (requirement is null ||
            !requirement.AcceptableMinimumValue.HasValue ||
            !requirement.AcceptableMaximumValue.HasValue ||
            string.IsNullOrWhiteSpace(requirement.TimeBasis))
        {
            return null;
        }

        return new SuitabilityRangeRequirement(
            OptimalMinimum: requirement.MinimumValue,
            OptimalMaximum: requirement.MaximumValue,
            AcceptableMinimum: requirement.AcceptableMinimumValue.Value,
            AcceptableMaximum: requirement.AcceptableMaximumValue.Value,
            Unit: requirement.Unit,
            TimeBasis: requirement.TimeBasis,
            IsCompatibleWithSevenDayForecast: requirement.IsCompatibleWithSevenDayForecast);
    }

    private static SuitabilityFactorWeights ToWeights(IReadOnlyList<SuitabilityConfiguration> configurations) =>
        new(
            Rainfall: GetConfigurationValue(
                configurations,
                ApprovedSuitabilityConfiguration.Types.FactorWeight,
                ApprovedSuitabilityConfiguration.Keys.Rainfall,
                40m),
            Temperature: GetConfigurationValue(
                configurations,
                ApprovedSuitabilityConfiguration.Types.FactorWeight,
                ApprovedSuitabilityConfiguration.Keys.Temperature,
                15m),
            Humidity: GetConfigurationValue(
                configurations,
                ApprovedSuitabilityConfiguration.Types.FactorWeight,
                ApprovedSuitabilityConfiguration.Keys.Humidity,
                10m),
            Soil: GetConfigurationValue(
                configurations,
                ApprovedSuitabilityConfiguration.Types.FactorWeight,
                ApprovedSuitabilityConfiguration.Keys.SoilCompatibility,
                35m));

    private static SuitabilityCategoryThresholds ToCategoryThresholds(IReadOnlyList<SuitabilityConfiguration> configurations) =>
        new(
            HighlySuitableMinimum: GetConfigurationValue(
                configurations,
                ApprovedSuitabilityConfiguration.Types.CategoryThreshold,
                ApprovedSuitabilityConfiguration.Keys.HighlySuitable,
                80m),
            SuitableMinimum: GetConfigurationValue(
                configurations,
                ApprovedSuitabilityConfiguration.Types.CategoryThreshold,
                ApprovedSuitabilityConfiguration.Keys.Suitable,
                50m),
            ModeratelySuitableMinimum: GetConfigurationValue(
                configurations,
                ApprovedSuitabilityConfiguration.Types.CategoryThreshold,
                ApprovedSuitabilityConfiguration.Keys.ModeratelySuitable,
                25m));

    private static decimal GetConfigurationValue(
        IReadOnlyList<SuitabilityConfiguration> configurations,
        string type,
        string key,
        decimal fallback)
    {
        return configurations
            .SingleOrDefault(configuration => configuration.ConfigurationType == type && configuration.ConfigurationKey == key)
            ?.Value ?? fallback;
    }
}
