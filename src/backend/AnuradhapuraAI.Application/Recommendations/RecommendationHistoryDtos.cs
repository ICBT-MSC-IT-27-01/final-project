namespace AnuradhapuraAI.Application.Recommendations;

public sealed record RecommendationHistoryQuery(
    int TrustedRegisteredUserId,
    int Page,
    int PageSize);

public sealed record RecommendationHistoryPageResponse(
    IReadOnlyList<RecommendationHistorySummaryResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record RecommendationHistorySummaryResponse(
    int RecommendationId,
    Guid ForecastRunId,
    string? SoilType,
    DateTimeOffset CreatedAt,
    DateOnly? ForecastDate,
    DateOnly? TargetStartDate,
    DateOnly? TargetEndDate,
    string? ModelVersion,
    int CropResultCount,
    string? TopCropName,
    decimal? TopOverallScore,
    string? TopSuitabilityCategory);

public sealed record RecommendationHistoryDetailResponse(
    int RecommendationId,
    Guid ForecastRunId,
    string? SoilType,
    DateTimeOffset CreatedAt,
    RecommendationHistoryForecastResponse Forecast,
    string EvidenceSnapshotJson,
    IReadOnlyList<RecommendationHistoryCropResponse> Crops,
    IReadOnlyList<string> Limitations);

public sealed record RecommendationHistoryForecastResponse(
    Guid ForecastRunId,
    DateOnly? ForecastDate,
    DateOnly? TargetStartDate,
    DateOnly? TargetEndDate,
    string? ModelVersion,
    DateTimeOffset? CreatedAt,
    IReadOnlyList<RecommendationHistoryForecastDayResponse> Days);

public sealed record RecommendationHistoryForecastDayResponse(
    DateOnly? TargetDate,
    decimal? Rainfall,
    decimal? Temperature,
    decimal? Humidity);

public sealed record RecommendationHistoryCropResponse(
    int CropId,
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
    IReadOnlyList<RecommendationHistoryFactorResponse> Factors,
    IReadOnlyList<string> ClimateRisks,
    string Explanation,
    string EvidenceSnapshotJson);

public sealed record RecommendationHistoryFactorResponse(
    string Factor,
    bool IsEvaluable,
    decimal? Score,
    decimal? AggregatedValue,
    decimal? ConfiguredWeight,
    decimal? EffectiveWeight,
    string? Explanation);

public sealed class RecommendationHistoryResult<T>
{
    private RecommendationHistoryResult(bool succeeded, T? value, string? errorCode, string? message)
    {
        Succeeded = succeeded;
        Value = value;
        ErrorCode = errorCode;
        Message = message;
    }

    public bool Succeeded { get; }

    public T? Value { get; }

    public string? ErrorCode { get; }

    public string? Message { get; }

    public static RecommendationHistoryResult<T> Success(T value) => new(true, value, null, null);

    public static RecommendationHistoryResult<T> Failure(string errorCode, string message) =>
        new(false, default, errorCode, message);
}

public static class RecommendationHistoryErrorCodes
{
    public const string FeatureDisabled = "FeatureDisabled";
    public const string InvalidUserContext = "InvalidUserContext";
    public const string InvalidPaging = "InvalidPaging";
    public const string RecommendationNotFound = "RecommendationNotFound";
    public const string DatabaseReadFailed = "DatabaseReadFailed";
}
