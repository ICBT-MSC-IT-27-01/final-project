namespace AnuradhapuraAI.Application.Authentication;

public sealed record CurrentUserResponse(
    int Id,
    string Email,
    string Role);
