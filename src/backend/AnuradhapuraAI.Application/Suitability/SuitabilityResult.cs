namespace AnuradhapuraAI.Application.Suitability;

public sealed class SuitabilityResult<T>
{
    private SuitabilityResult(T? value, string? errorCode, string? message)
    {
        Value = value;
        ErrorCode = errorCode;
        Message = message;
    }

    public bool Succeeded => ErrorCode is null;

    public T? Value { get; }

    public string? ErrorCode { get; }

    public string? Message { get; }

    public static SuitabilityResult<T> Success(T value) => new(value, null, null);

    public static SuitabilityResult<T> Failure(string errorCode, string message) => new(default, errorCode, message);
}
