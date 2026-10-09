namespace AnuradhapuraAI.Application.Recommendations;

public sealed class RecommendationOrchestrationResult<T>
{
    private RecommendationOrchestrationResult(bool succeeded, T? value, string? errorCode, string? message)
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

    public static RecommendationOrchestrationResult<T> Success(T value) =>
        new(true, value, null, null);

    public static RecommendationOrchestrationResult<T> Failure(string errorCode, string message) =>
        new(false, default, errorCode, message);
}
