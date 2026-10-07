namespace AnuradhapuraAI.Domain.Common;

public static class ApprovedTimeBases
{
    public const string Daily = "Daily";
    public const string SevenDay = "SevenDay";
    public const string GrowingPeriod = "GrowingPeriod";
    public const string Seasonal = "Seasonal";
    public const string Annual = "Annual";

    public static readonly string[] All =
    [
        Daily,
        SevenDay,
        GrowingPeriod,
        Seasonal,
        Annual
    ];
}
