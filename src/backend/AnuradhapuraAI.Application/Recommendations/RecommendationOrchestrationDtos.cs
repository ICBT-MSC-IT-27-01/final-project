using AnuradhapuraAI.Application.Suitability;

namespace AnuradhapuraAI.Application.Recommendations;

public sealed record RecommendationEvaluationRequest(string? SoilType);

public sealed record RecommendationEvaluationResponse(
    SelectedForecastRun Forecast,
    IReadOnlyList<CropRecommendationEvaluation> Crops)
{
    public AppliedRecommendationConfiguration? AppliedConfiguration { get; init; }
}

public sealed record SelectedForecastRun(
    Guid ForecastRunId,
    DateOnly ForecastDate,
    DateOnly TargetStartDate,
    DateOnly TargetEndDate,
    string? ModelVersion,
    DateTimeOffset CreatedAt,
    IReadOnlyList<SuitabilityForecastDay> ForecastDays);

public sealed record CropRecommendationEvaluation(
    string CropName,
    string EvaluationStatus,
    decimal? RainfallScore,
    decimal? TemperatureScore,
    decimal? HumidityScore,
    decimal? SoilScore,
    decimal? OverallScore,
    string? SuitabilityCategory,
    IReadOnlyList<string> UnavailableFactors,
    IReadOnlyList<SuitabilityFactorResult> Factors,
    IReadOnlyList<string> ClimateRisks,
    string Explanation,
    EvidenceCoverage EvidenceCoverage);

public sealed record EvidenceCoverage(
    int EvaluatedFactorCount,
    int TotalFactorCount,
    decimal EvaluatedConfiguredWeightTotal);

public sealed record AppliedRecommendationConfiguration(
    SuitabilityFactorWeights Weights,
    SuitabilityCategoryThresholds CategoryThresholds,
    IReadOnlyList<AppliedCropConfiguration> Crops);

public sealed record AppliedCropConfiguration(
    string CropName,
    AppliedRangeRequirement? RainfallRequirement,
    AppliedRangeRequirement? TemperatureRequirement,
    AppliedRangeRequirement? HumidityRequirement,
    IReadOnlyList<AppliedSoilCompatibility> SoilCompatibilities);

public sealed record AppliedRangeRequirement(
    decimal OptimalMinimum,
    decimal OptimalMaximum,
    decimal AcceptableMinimum,
    decimal AcceptableMaximum,
    string Unit,
    string TimeBasis,
    bool IsCompatibleWithSevenDayForecast);

public sealed record AppliedSoilCompatibility(
    string SoilType,
    decimal CompatibilityScore);

public sealed record RankedRecommendationEvaluationResponse(
    SelectedForecastRun Forecast,
    IReadOnlyList<RankedCropRecommendationEvaluation> Crops)
{
    public AppliedRecommendationConfiguration? AppliedConfiguration { get; init; }
}

public sealed record RankedCropRecommendationEvaluation(
    string CropName,
    string EvaluationStatus,
    decimal? RainfallScore,
    decimal? TemperatureScore,
    decimal? HumidityScore,
    decimal? SoilScore,
    decimal? OverallScore,
    string? SuitabilityCategory,
    int? Rank,
    IReadOnlyList<string> UnavailableFactors,
    IReadOnlyList<SuitabilityFactorResult> Factors,
    IReadOnlyList<string> ClimateRisks,
    string Explanation,
    EvidenceCoverage EvidenceCoverage);

public sealed record CreateRecommendationPersistenceRequest(
    string? SoilType,
    int? TrustedRegisteredUserId = null);

public sealed record PersistedRecommendationResponse(
    int RecommendationId,
    Guid ForecastRunId,
    int? UserId,
    string? SoilType,
    DateTimeOffset CreatedAt,
    IReadOnlyList<RankedCropRecommendationEvaluation> Crops);

public static class RecommendationOrchestrationErrorCodes
{
    public const string ForecastUnavailable = "ForecastUnavailable";
    public const string InvalidForecastData = "InvalidForecastData";
    public const string InvalidConfiguration = "InvalidConfiguration";
    public const string DatabaseReadFailed = "DatabaseReadFailed";
    public const string InvalidUserContext = "InvalidUserContext";
    public const string SnapshotSerializationFailed = "SnapshotSerializationFailed";
    public const string PersistenceFailed = "PersistenceFailed";
}
