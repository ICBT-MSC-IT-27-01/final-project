namespace AnuradhapuraAI.Application.Recommendations;

public interface IRecommendationRankingService
{
    RankedRecommendationEvaluationResponse Rank(RecommendationEvaluationResponse evaluation);
}
