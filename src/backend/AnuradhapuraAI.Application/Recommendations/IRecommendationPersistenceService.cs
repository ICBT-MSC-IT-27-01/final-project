namespace AnuradhapuraAI.Application.Recommendations;

public interface IRecommendationPersistenceService
{
    Task<RecommendationOrchestrationResult<PersistedRecommendationResponse>> CreateRecommendationAsync(
        CreateRecommendationPersistenceRequest request,
        CancellationToken cancellationToken = default);
}
