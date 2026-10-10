namespace AnuradhapuraAI.Application.Validations;

public sealed class SubmitRecommendationValidationRequest
{
    public string Status { get; set; } = string.Empty;

    public string? Comment { get; set; }
}

public sealed record RecommendationValidationSubmission(
    string Status,
    string? Comment,
    int TrustedOfficerUserId);

public sealed record RecommendationValidationResponse(
    int Id,
    int RecommendationId,
    int OfficerUserId,
    string Status,
    string? Comment,
    DateTimeOffset CreatedAt);

public sealed record RecommendationReviewSummaryResponse(
    int RecommendationId,
    Guid ForecastRunId,
    int? UserId,
    string? SoilType,
    DateTimeOffset CreatedAt,
    string? LatestValidationStatus,
    DateTimeOffset? LatestValidationCreatedAt);

public sealed record RecommendationReviewResponse(
    int RecommendationId,
    Guid ForecastRunId,
    int? UserId,
    string? SoilType,
    DateTimeOffset CreatedAt,
    string EvidenceSnapshotJson,
    IReadOnlyList<RecommendationReviewCropResponse> Crops,
    IReadOnlyList<RecommendationValidationResponse> ValidationHistory);

public sealed record RecommendationReviewCropResponse(
    int CropId,
    string CropName,
    decimal? RainfallScore,
    decimal? TemperatureScore,
    decimal? HumidityScore,
    decimal? SoilScore,
    decimal? OverallScore,
    string? SuitabilityCategory,
    int? Rank,
    string EvaluationStatus,
    string Explanation,
    string EvidenceSnapshotJson);
