using AnuradhapuraAI.Domain.Common;

namespace AnuradhapuraAI.Application.Suitability;

public sealed record SuitabilityForecastDay(
    DateOnly TargetDate,
    decimal Rainfall,
    decimal Temperature,
    decimal Humidity);

public sealed record SuitabilityRangeRequirement(
    decimal OptimalMinimum,
    decimal OptimalMaximum,
    decimal AcceptableMinimum,
    decimal AcceptableMaximum,
    string Unit,
    string TimeBasis,
    bool IsCompatibleWithSevenDayForecast);

public sealed record SuitabilitySoilCompatibility(
    string SoilType,
    decimal CompatibilityScore);

public sealed record CropSuitabilityProfile(
    string CropName,
    SuitabilityRangeRequirement? RainfallRequirement,
    SuitabilityRangeRequirement? TemperatureRequirement,
    SuitabilityRangeRequirement? HumidityRequirement,
    IReadOnlyList<SuitabilitySoilCompatibility> SoilCompatibilities);

public sealed record SuitabilityFactorWeights(
    decimal Rainfall = 25m,
    decimal Temperature = 25m,
    decimal Humidity = 25m,
    decimal Soil = 25m);

public sealed record SuitabilityCategoryThresholds(
    decimal HighlySuitableMinimum = 80m,
    decimal SuitableMinimum = 60m,
    decimal ModeratelySuitableMinimum = 40m);

public sealed record ClimateRiskThresholds(
    decimal? LowRainfallMaximum = null,
    decimal? HeavyRainfallMinimum = null,
    decimal? HighTemperatureMinimum = null);

public sealed record SuitabilityEvaluationRequest(
    IReadOnlyList<SuitabilityForecastDay> Forecast,
    string? SoilType,
    IReadOnlyList<CropSuitabilityProfile> CropProfiles,
    SuitabilityFactorWeights Weights,
    SuitabilityCategoryThresholds CategoryThresholds,
    bool EnableClimateRiskIndicators,
    ClimateRiskThresholds? ClimateRiskThresholds = null);

public sealed record SuitabilityEvaluationResponse(
    IReadOnlyList<CropSuitabilityResult> Crops);

public sealed record CropSuitabilityResult(
    string CropName,
    decimal? OverallScore,
    string? SuitabilityCategory,
    IReadOnlyList<SuitabilityFactorResult> Factors,
    IReadOnlyList<string> ClimateRisks,
    string Explanation,
    int EvaluatedFactorCount)
{
    public SuitabilityFactorResult Rainfall =>
        Factors.Single(factor => factor.Factor == SuitabilityFactors.Rainfall);

    public SuitabilityFactorResult Temperature =>
        Factors.Single(factor => factor.Factor == SuitabilityFactors.Temperature);

    public SuitabilityFactorResult Humidity =>
        Factors.Single(factor => factor.Factor == SuitabilityFactors.Humidity);

    public SuitabilityFactorResult Soil =>
        Factors.Single(factor => factor.Factor == SuitabilityFactors.Soil);
}

public sealed record SuitabilityFactorResult(
    string Factor,
    bool IsEvaluable,
    decimal? Score,
    decimal? AggregatedValue,
    decimal ConfiguredWeight,
    decimal? EffectiveWeight,
    string Explanation);

public static class SuitabilityFactors
{
    public const string Rainfall = ApprovedVariableTypes.Rainfall;
    public const string Temperature = ApprovedVariableTypes.Temperature;
    public const string Humidity = ApprovedVariableTypes.Humidity;
    public const string Soil = "Soil";

    public static readonly string[] All =
    [
        Rainfall,
        Temperature,
        Humidity,
        Soil
    ];
}

public static class ApprovedClimateRisks
{
    public const string LowRainfall = "Low rainfall";
    public const string HeavyRainfall = "Heavy rainfall";
    public const string HighTemperature = "High temperature";

    public static readonly string[] All =
    [
        LowRainfall,
        HeavyRainfall,
        HighTemperature
    ];
}
