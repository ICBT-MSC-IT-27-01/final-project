namespace AnuradhapuraAI.Application.Forecasting;

public sealed class ForecastResult<T>
{
    private ForecastResult(T? value, string? errorCode, string? message)
    {
        Value = value;
        ErrorCode = errorCode;
        Message = message;
    }

    public bool Succeeded => ErrorCode is null;

    public T? Value { get; }

    public string? ErrorCode { get; }

    public string? Message { get; }

    public static ForecastResult<T> Success(T value) => new(value, null, null);

    public static ForecastResult<T> Failure(string errorCode, string message) => new(default, errorCode, message);
}
