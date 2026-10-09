namespace AnuradhapuraAI.Application.Recommendations;

public interface IRecommendationOrchestrationService
{
    Task<RecommendationOrchestrationResult<RecommendationEvaluationResponse>> EvaluateLatestForecastAsync(
        RecommendationEvaluationRequest request,
        CancellationToken cancellationToken = default);
}
