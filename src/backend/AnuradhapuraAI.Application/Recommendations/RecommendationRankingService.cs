using AnuradhapuraAI.Domain.Common;

namespace AnuradhapuraAI.Application.Recommendations;

public sealed class RecommendationRankingService : IRecommendationRankingService
{
    public RankedRecommendationEvaluationResponse Rank(RecommendationEvaluationResponse evaluation)
    {
        var rankByCrop = evaluation.Crops
            .Where(crop =>
                crop.OverallScore.HasValue &&
                !string.Equals(
                    crop.EvaluationStatus,
                    ApprovedRecommendationEvaluationStatuses.InsufficientEvidence,
                    StringComparison.OrdinalIgnoreCase))
            .GroupBy(crop => new { crop.AssessmentState, crop.AssessmentBasis })
            .SelectMany(RankComparableGroup)
            .ToDictionary(item => item.CropName, item => (int?)item.Rank, StringComparer.OrdinalIgnoreCase);

        var rankedCrops = evaluation.Crops
            .OrderBy(crop => ApprovedCropOrder(crop.CropName))
            .Select(crop => new RankedCropRecommendationEvaluation(
                crop.CropName,
                crop.EvaluationStatus,
                crop.RainfallScore,
                crop.TemperatureScore,
                crop.HumidityScore,
                crop.SoilScore,
                crop.OverallScore,
                crop.SuitabilityCategory,
                rankByCrop.GetValueOrDefault(crop.CropName),
                crop.UnavailableFactors,
                crop.Factors,
                crop.ClimateRisks,
                crop.Explanation,
                crop.EvidenceCoverage)
            {
                AssessmentState = crop.AssessmentState,
                AssessmentBasis = crop.AssessmentBasis,
                EvidenceSources = crop.EvidenceSources,
                ScientificLimitations = crop.ScientificLimitations
            })
            .ToList();

        return new RankedRecommendationEvaluationResponse(evaluation.Forecast, rankedCrops)
        {
            AppliedConfiguration = evaluation.AppliedConfiguration
        };
    }

    private static int ApprovedCropOrder(string cropName) =>
        Array.FindIndex(ApprovedCropNames.All, approvedCropName =>
            string.Equals(approvedCropName, cropName, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<(string CropName, int Rank)> RankComparableGroup(
        IEnumerable<CropRecommendationEvaluation> group)
    {
        var ordered = group
            .OrderByDescending(crop => crop.OverallScore!.Value)
            .ThenByDescending(crop => crop.EvidenceCoverage.EvaluatedFactorCount)
            .ThenByDescending(crop => crop.EvidenceCoverage.EvaluatedConfiguredWeightTotal)
            .ThenBy(crop => ApprovedCropOrder(crop.CropName))
            .ToList();

        if (ordered.All(crop =>
                string.Equals(
                    crop.AssessmentState,
                    RecommendationAssessmentStates.PartiallyAssessed,
                    StringComparison.OrdinalIgnoreCase)) &&
            ordered.Select(crop => crop.OverallScore!.Value).Distinct().Count() <= 1)
        {
            return [];
        }

        return ordered.Select((crop, index) => (crop.CropName, index + 1));
    }
}
