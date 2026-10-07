namespace AnuradhapuraAI.Application.Authentication;

public sealed record RegisterResponse(
    int UserId,
    string Name,
    string Email,
    string Role,
    DateTimeOffset CreatedAt);
