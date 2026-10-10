namespace AnuradhapuraAI.Application.Validations;

public interface IRecommendationValidationService
{
    Task<RecommendationValidationResult<IReadOnlyList<RecommendationReviewSummaryResponse>>> ListPendingReviewsAsync(
        CancellationToken cancellationToken = default);

    Task<RecommendationValidationResult<RecommendationReviewResponse>> GetReviewAsync(
        int recommendationId,
        CancellationToken cancellationToken = default);

    Task<RecommendationValidationResult<RecommendationValidationResponse>> SubmitValidationAsync(
        int recommendationId,
        RecommendationValidationSubmission submission,
        CancellationToken cancellationToken = default);
}
