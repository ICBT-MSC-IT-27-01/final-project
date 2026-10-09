using AnuradhapuraAI.Application.Suitability;

namespace AnuradhapuraAI.Application.Recommendations;

public sealed record RecommendationEvaluationRequest(string? SoilType);

public sealed record RecommendationEvaluationResponse(
    SelectedForecastRun Forecast,
    IReadOnlyList<CropRecommendationEvaluation> Crops);

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

public static class RecommendationOrchestrationErrorCodes
{
    public const string ForecastUnavailable = "ForecastUnavailable";
    public const string InvalidForecastData = "InvalidForecastData";
    public const string InvalidConfiguration = "InvalidConfiguration";
    public const string DatabaseReadFailed = "DatabaseReadFailed";
}
