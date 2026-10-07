namespace AnuradhapuraAI.Application.Suitability;

public interface ISuitabilityConfigurationProvider
{
    Task<SuitabilityConfigurationSnapshot> GetActiveConfigurationAsync(CancellationToken cancellationToken = default);
}

public sealed record SuitabilityConfigurationSnapshot(
    IReadOnlyList<CropSuitabilityProfile> CropProfiles,
    SuitabilityFactorWeights Weights,
    SuitabilityCategoryThresholds CategoryThresholds);
