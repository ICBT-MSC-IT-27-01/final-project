namespace AnuradhapuraAI.Domain.Common;

public static class ApprovedRecommendationEvaluationStatuses
{
    public const string Complete = "Complete";
    public const string Partial = "Partial";
    public const string InsufficientEvidence = "InsufficientEvidence";

    public static readonly string[] All =
    [
        Complete,
        Partial,
        InsufficientEvidence
    ];
}
