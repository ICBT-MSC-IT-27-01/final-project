namespace AnuradhapuraAI.Application.Authentication;

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    int UserId,
    string Name,
    string Email,
    string Role);
