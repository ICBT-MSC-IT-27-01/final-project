namespace AnuradhapuraAI.Application.Recommendations;

public interface IRecommendationPersistenceService
{
    Task<RecommendationOrchestrationResult<PersistedRecommendationResponse>> CreateRecommendationAsync(
        CreateRecommendationPersistenceRequest request,
        CancellationToken cancellationToken = default);
}

public interface IRecommendationHistoryService
{
    Task<RecommendationHistoryResult<RecommendationHistoryPageResponse>> ListHistoryAsync(
        RecommendationHistoryQuery query,
        CancellationToken cancellationToken = default);

    Task<RecommendationHistoryResult<RecommendationHistoryDetailResponse>> GetDetailAsync(
        int recommendationId,
        int trustedRegisteredUserId,
        CancellationToken cancellationToken = default);
}
