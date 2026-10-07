using AnuradhapuraAI.Application.Authentication;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Authentication;
using AnuradhapuraAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class AuthenticationServiceTestHost : IAsyncDisposable
{
    private AuthenticationServiceTestHost(
        AnuradhapuraAiDbContext dbContext,
        IAuthenticationService authenticationService,
        IPasswordHasher<User> passwordHasher)
    {
        DbContext = dbContext;
        AuthenticationService = authenticationService;
        this.passwordHasher = passwordHasher;
    }

    private readonly IPasswordHasher<User> passwordHasher;

    public AnuradhapuraAiDbContext DbContext { get; }

    public IAuthenticationService AuthenticationService { get; }

    public static async Task<AuthenticationServiceTestHost> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<AnuradhapuraAiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var dbContext = new AnuradhapuraAiDbContext(options);
        var passwordHasher = new PasswordHasher<User>();
        var jwtSettings = Options.Create(new JwtSettings
        {
            Issuer = "Phase3Tests",
            Audience = "Phase3Tests",
            SigningKey = "phase-3-tests-signing-key-at-least-32-chars",
            ExpirationMinutes = 60
        });
        var tokenService = new JwtTokenService(jwtSettings, TimeProvider.System);
        var authenticationService = new AuthenticationService(
            dbContext,
            passwordHasher,
            tokenService,
            TimeProvider.System);

        var host = new AuthenticationServiceTestHost(dbContext, authenticationService, passwordHasher);
        await host.SeedRolesAsync();

        return host;
    }

    public async Task SeedUserAsync(
        string name,
        string email,
        string password,
        string roleName,
        bool isActive)
    {
        var role = await DbContext.UserRoles.SingleAsync(candidate => candidate.Name == roleName);
        var user = new User
        {
            Name = name,
            Email = email,
            RoleId = role.Id,
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
    }

    private async Task SeedRolesAsync()
    {
        DbContext.UserRoles.AddRange(
            new UserRole { Id = 1, Name = ApprovedRoleNames.RegisteredUser },
            new UserRole { Id = 2, Name = ApprovedRoleNames.AgriculturalOfficer },
            new UserRole { Id = 3, Name = ApprovedRoleNames.Administrator });

        await DbContext.SaveChangesAsync();
    }
}
