namespace AnuradhapuraAI.Domain.Common;

public static class ApprovedValidationStatuses
{
    public const string PendingValidation = "Pending Validation";
    public const string Validated = "Validated";
    public const string NeedsReview = "Needs Review";

    public static readonly string[] All =
    [
        PendingValidation,
        Validated,
        NeedsReview
    ];
}
