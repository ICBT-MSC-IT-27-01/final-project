using AnuradhapuraAI.Application.Authentication;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AnuradhapuraAI.Infrastructure.Authentication;

public sealed class AuthenticationService(
    AnuradhapuraAiDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService,
    TimeProvider timeProvider) : IAuthenticationService
{
    public async Task<AuthenticationResult<RegisterResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        var emailExists = await dbContext.Users
            .AnyAsync(user => user.Email == email, cancellationToken);

        if (emailExists)
        {
            return AuthenticationResult<RegisterResponse>.Failure(AuthenticationErrorCodes.DuplicateEmail);
        }

        var registeredUserRole = await dbContext.UserRoles
            .SingleOrDefaultAsync(role => role.Name == ApprovedRoleNames.RegisteredUser, cancellationToken);

        if (registeredUserRole is null)
        {
            return AuthenticationResult<RegisterResponse>.Failure(AuthenticationErrorCodes.RegisteredUserRoleMissing);
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            RoleId = registeredUserRole.Id,
            IsActive = true,
            CreatedAt = timeProvider.GetUtcNow()
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new RegisterResponse(
            user.Id,
            user.Name,
            user.Email,
            ApprovedRoleNames.RegisteredUser,
            user.CreatedAt);

        return AuthenticationResult<RegisterResponse>.Success(response);
    }

    public async Task<AuthenticationResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        var user = await dbContext.Users
            .Include(candidate => candidate.Role)
            .SingleOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);

        if (user is null)
        {
            return AuthenticationResult<LoginResponse>.Failure(AuthenticationErrorCodes.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return AuthenticationResult<LoginResponse>.Failure(AuthenticationErrorCodes.InactiveUser);
        }

        var passwordResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return AuthenticationResult<LoginResponse>.Failure(AuthenticationErrorCodes.InvalidCredentials);
        }

        var roleName = user.Role?.Name;
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return AuthenticationResult<LoginResponse>.Failure(AuthenticationErrorCodes.InvalidCredentials);
        }

        var token = tokenService.CreateToken(user, roleName);
        var response = new LoginResponse(
            token.AccessToken,
            token.ExpiresAt,
            user.Id,
            user.Name,
            user.Email,
            roleName);

        return AuthenticationResult<LoginResponse>.Success(response);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
