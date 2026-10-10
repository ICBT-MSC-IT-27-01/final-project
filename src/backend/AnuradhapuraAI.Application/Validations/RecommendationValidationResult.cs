namespace AnuradhapuraAI.Application.Validations;

public sealed class RecommendationValidationResult<T>
{
    private RecommendationValidationResult(bool succeeded, T? value, string? errorCode, string? message)
    {
        Succeeded = succeeded;
        Value = value;
        ErrorCode = errorCode;
        Message = message;
    }

    public bool Succeeded { get; }

    public T? Value { get; }

    public string? ErrorCode { get; }

    public string? Message { get; }

    public static RecommendationValidationResult<T> Success(T value) =>
        new(true, value, null, null);

    public static RecommendationValidationResult<T> Failure(string errorCode, string message) =>
        new(false, default, errorCode, message);
}

public static class RecommendationValidationErrorCodes
{
    public const string FeatureDisabled = "FeatureDisabled";
    public const string RecommendationNotFound = "RecommendationNotFound";
    public const string InvalidReviewer = "InvalidReviewer";
    public const string InvalidStatus = "InvalidStatus";
    public const string InvalidComment = "InvalidComment";
    public const string DatabaseReadFailed = "DatabaseReadFailed";
    public const string PersistenceFailed = "PersistenceFailed";
}
