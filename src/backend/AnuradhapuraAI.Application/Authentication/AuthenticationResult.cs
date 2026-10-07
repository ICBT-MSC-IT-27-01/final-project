namespace AnuradhapuraAI.Application.Authentication;

public sealed record AuthenticationResult<T>(
    bool Succeeded,
    T? Value,
    string? ErrorCode)
{
    public static AuthenticationResult<T> Success(T value)
    {
        return new AuthenticationResult<T>(true, value, null);
    }

    public static AuthenticationResult<T> Failure(string errorCode)
    {
        return new AuthenticationResult<T>(false, default, errorCode);
    }
}
