namespace AnuradhapuraAI.Application.Suitability;

public static class RangeSuitabilityScorer
{
    public static decimal Score(
        decimal value,
        decimal acceptableMinimum,
        decimal optimalMinimum,
        decimal optimalMaximum,
        decimal acceptableMaximum)
    {
        if (acceptableMinimum > optimalMinimum ||
            optimalMinimum > optimalMaximum ||
            optimalMaximum > acceptableMaximum)
        {
            throw new ArgumentException("Suitability range boundaries must satisfy acceptableMin <= optimalMin <= optimalMax <= acceptableMax.");
        }

        if (value < acceptableMinimum || value > acceptableMaximum)
        {
            return 0m;
        }

        if (value >= optimalMinimum && value <= optimalMaximum)
        {
            return 100m;
        }

        if (value < optimalMinimum)
        {
            return ScoreTransition(value, acceptableMinimum, optimalMinimum, lowerTransition: true);
        }

        return ScoreTransition(value, optimalMaximum, acceptableMaximum, lowerTransition: false);
    }

    private static decimal ScoreTransition(
        decimal value,
        decimal edge,
        decimal optimalBoundary,
        bool lowerTransition)
    {
        var denominator = optimalBoundary - edge;
        if (denominator == 0m)
        {
            return 0m;
        }

        var rawScore = lowerTransition
            ? 100m * (value - edge) / denominator
            : 100m * (optimalBoundary - value) / denominator;

        return Math.Clamp(rawScore, 0m, 100m);
    }
}
