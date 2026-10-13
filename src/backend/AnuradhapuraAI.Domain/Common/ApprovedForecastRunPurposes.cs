namespace AnuradhapuraAI.Domain.Common;

public static class ApprovedForecastRunPurposes
{
    public const string Operational = "Operational";
    public const string HistoricalEvaluation = "HistoricalEvaluation";
    public const string LegacyUnknown = "LegacyUnknown";

    public static readonly string[] All =
    [
        Operational,
        HistoricalEvaluation,
        LegacyUnknown
    ];
}
