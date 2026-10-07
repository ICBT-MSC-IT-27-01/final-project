using System.Text.Json;
using AnuradhapuraAI.Application.Authentication;
using AnuradhapuraAI.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class AuthenticationServiceTests
{
    [Fact]
    public async Task Register_ValidUser_CreatesRegisteredUser()
    {
        await using var host = await AuthenticationServiceTestHost.CreateAsync();

        var result = await host.AuthenticationService.RegisterAsync(new RegisterRequest
        {
            Name = "Normal User",
            Email = "Normal.User@example.com",
            Password = "SecurePass123"
        });
        var user = await host.DbContext.Users.Include(candidate => candidate.Role).SingleAsync();

        Assert.True(result.Succeeded);
        Assert.Equal("normal.user@example.com", user.Email);
        Assert.Equal(ApprovedRoleNames.RegisteredUser, user.Role?.Name);
        Assert.True(user.IsActive);
    }

    [Theory]
    [InlineData("Administrator", 3)]
    [InlineData("Agricultural Officer", 2)]
    public async Task Register_ClientSuppliedPrivilegedRole_IsIgnored(string role, int roleId)
    {
        await using var host = await AuthenticationServiceTestHost.CreateAsync();
        var request = JsonSerializer.Deserialize<RegisterRequest>(
            $$"""
            {
              "name": "Role Attempt",
              "email": "{{roleId}}.attempt@example.com",
              "password": "SecurePass123",
              "role": "{{role}}",
              "roleId": {{roleId}},
              "isAdmin": true,
              "isOfficer": true
            }
            """,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var result = await host.AuthenticationService.RegisterAsync(request);
        var user = await host.DbContext.Users.Include(candidate => candidate.Role).SingleAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(ApprovedRoleNames.RegisteredUser, user.Role?.Name);
    }

    [Fact]
    public async Task Register_DuplicateEmail_IsRejected()
    {
        await using var host = await AuthenticationServiceTestHost.CreateAsync();
        var request = new RegisterRequest
        {
            Name = "Duplicate User",
            Email = "duplicate@example.com",
            Password = "SecurePass123"
        };

        var firstResult = await host.AuthenticationService.RegisterAsync(request);
        var secondResult = await host.AuthenticationService.RegisterAsync(request);

        Assert.True(firstResult.Succeeded);
        Assert.False(secondResult.Succeeded);
        Assert.Equal(AuthenticationErrorCodes.DuplicateEmail, secondResult.ErrorCode);
    }

    [Fact]
    public async Task Register_Password_IsHashed()
    {
        await using var host = await AuthenticationServiceTestHost.CreateAsync();

        await host.AuthenticationService.RegisterAsync(new RegisterRequest
        {
            Name = "Password User",
            Email = "password@example.com",
            Password = "SecurePass123"
        });
        var user = await host.DbContext.Users.SingleAsync();

        Assert.NotEqual("SecurePass123", user.PasswordHash);
        Assert.NotEmpty(user.PasswordHash);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        await using var host = await AuthenticationServiceTestHost.CreateAsync();
        await host.AuthenticationService.RegisterAsync(new RegisterRequest
        {
            Name = "Login User",
            Email = "login@example.com",
            Password = "SecurePass123"
        });

        var result = await host.AuthenticationService.LoginAsync(new LoginRequest
        {
            Email = "login@example.com",
            Password = "SecurePass123"
        });

        Assert.True(result.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(result.Value?.AccessToken));
        Assert.Equal(ApprovedRoleNames.RegisteredUser, result.Value?.Role);
    }

    [Fact]
    public async Task Login_InvalidCredentials_IsRejected()
    {
        await using var host = await AuthenticationServiceTestHost.CreateAsync();

        var result = await host.AuthenticationService.LoginAsync(new LoginRequest
        {
            Email = "missing@example.com",
            Password = "WrongPassword"
        });

        Assert.False(result.Succeeded);
        Assert.Equal(AuthenticationErrorCodes.InvalidCredentials, result.ErrorCode);
    }

    [Fact]
    public async Task Login_InactiveUser_IsRejected()
    {
        await using var host = await AuthenticationServiceTestHost.CreateAsync();
        await host.SeedUserAsync(
            "Inactive User",
            "inactive@example.com",
            "SecurePass123",
            ApprovedRoleNames.RegisteredUser,
            isActive: false);

        var result = await host.AuthenticationService.LoginAsync(new LoginRequest
        {
            Email = "inactive@example.com",
            Password = "SecurePass123"
        });

        Assert.False(result.Succeeded);
        Assert.Equal(AuthenticationErrorCodes.InactiveUser, result.ErrorCode);
    }
}
