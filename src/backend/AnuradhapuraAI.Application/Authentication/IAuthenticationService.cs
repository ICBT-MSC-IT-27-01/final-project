namespace AnuradhapuraAI.Application.Authentication;

public interface IAuthenticationService
{
    Task<AuthenticationResult<RegisterResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthenticationResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}
