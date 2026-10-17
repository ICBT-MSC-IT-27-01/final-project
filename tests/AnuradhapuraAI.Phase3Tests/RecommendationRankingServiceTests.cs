using AnuradhapuraAI.Application.Recommendations;
using AnuradhapuraAI.Application.Suitability;
using AnuradhapuraAI.Domain.Common;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class RecommendationRankingServiceTests
{
    [Fact]
    public void Rank_LeavesEquivalentPartialIndicatorsUnranked()
    {
        var evaluation = new RecommendationEvaluationResponse(
            new SelectedForecastRun(Guid.NewGuid(), new DateOnly(2026, 10, 17), new DateOnly(2026, 10, 18), new DateOnly(2026, 10, 24), null, DateTimeOffset.UtcNow, []),
            ApprovedCropNames.All.Select(CreateEquivalentIndicator).ToList());

        var ranked = new RecommendationRankingService().Rank(evaluation);

        Assert.All(ranked.Crops, crop => Assert.Null(crop.Rank));
    }

    private static CropRecommendationEvaluation CreateEquivalentIndicator(string cropName) =>
        new(
            cropName,
            ApprovedRecommendationEvaluationStatuses.Partial,
            null,
            100m,
            null,
            null,
            100m,
            null,
            [SuitabilityFactors.Rainfall, SuitabilityFactors.Humidity, SuitabilityFactors.Soil],
            [],
            [],
            "Temperature-only indicator.",
            new EvidenceCoverage(1, 4, 25m))
        {
            AssessmentState = RecommendationAssessmentStates.PartiallyAssessed,
            AssessmentBasis = "Seven-day mean air-temperature compatibility indicator only"
        };
}
