using AnuradhapuraAI.Application.Suitability;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnuradhapuraAI.Infrastructure.Suitability;

public sealed class SuitabilityConfigurationProvider(AnuradhapuraAiDbContext dbContext) : ISuitabilityConfigurationProvider
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
            .Select(crop => ToCropProfile(crop!))
            .ToList();

        return new SuitabilityConfigurationSnapshot(
            profiles,
            ToWeights(configurations),
            ToCategoryThresholds(configurations));
    }

    private static CropSuitabilityProfile ToCropProfile(Crop crop) =>
        new(
            crop.Name,
            ToRangeRequirement(crop.EnvironmentalRequirements.SingleOrDefault(requirement => requirement.VariableType == ApprovedVariableTypes.Rainfall)),
            ToRangeRequirement(crop.EnvironmentalRequirements.SingleOrDefault(requirement => requirement.VariableType == ApprovedVariableTypes.Temperature)),
            ToRangeRequirement(crop.EnvironmentalRequirements.SingleOrDefault(requirement => requirement.VariableType == ApprovedVariableTypes.Humidity)),
            crop.SoilCompatibilities
                .Select(compatibility => new SuitabilitySoilCompatibility(
                    compatibility.SoilType,
                    compatibility.CompatibilityScore))
                .ToList());

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
                25m),
            Temperature: GetConfigurationValue(
                configurations,
                ApprovedSuitabilityConfiguration.Types.FactorWeight,
                ApprovedSuitabilityConfiguration.Keys.Temperature,
                25m),
            Humidity: GetConfigurationValue(
                configurations,
                ApprovedSuitabilityConfiguration.Types.FactorWeight,
                ApprovedSuitabilityConfiguration.Keys.Humidity,
                25m),
            Soil: GetConfigurationValue(
                configurations,
                ApprovedSuitabilityConfiguration.Types.FactorWeight,
                ApprovedSuitabilityConfiguration.Keys.SoilCompatibility,
                25m));

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
                60m),
            ModeratelySuitableMinimum: GetConfigurationValue(
                configurations,
                ApprovedSuitabilityConfiguration.Types.CategoryThreshold,
                ApprovedSuitabilityConfiguration.Keys.ModeratelySuitable,
                40m));

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
