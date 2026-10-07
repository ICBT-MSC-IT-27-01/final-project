namespace AnuradhapuraAI.Domain.Common;

public static class ApprovedSuitabilityCategories
{
    public const string HighlySuitable = "Highly Suitable";
    public const string Suitable = "Suitable";
    public const string ModeratelySuitable = "Moderately Suitable";
    public const string Unsuitable = "Unsuitable";

    public static readonly string[] All =
    [
        HighlySuitable,
        Suitable,
        ModeratelySuitable,
        Unsuitable
    ];
}
