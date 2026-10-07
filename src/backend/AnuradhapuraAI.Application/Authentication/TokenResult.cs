namespace AnuradhapuraAI.Application.Authentication;

public sealed record TokenResult(
    string AccessToken,
    DateTimeOffset ExpiresAt);
