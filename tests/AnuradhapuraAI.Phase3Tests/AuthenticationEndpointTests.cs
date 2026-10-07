using System.Net;
using AnuradhapuraAI.Domain.Common;
using Xunit;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class AuthenticationEndpointTests : IClassFixture<Phase3WebApplicationFactory>
{
    private readonly Phase3WebApplicationFactory factory;

    public AuthenticationEndpointTests(Phase3WebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Me_UnauthenticatedRequest_ReturnsUnauthorized()
    {
        using var testFactory = factory.CreateIsolated();
        var client = testFactory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminCheck_RegisteredUser_ReturnsForbidden()
    {
        using var testFactory = factory.CreateIsolated();
        var client = testFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.EmailHeader, "registered@example.com");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, ApprovedRoleNames.RegisteredUser);

        var response = await client.GetAsync("/api/auth/admin-check");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminCheck_Administrator_ReturnsOk()
    {
        using var testFactory = factory.CreateIsolated();
        var client = testFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.EmailHeader, "admin@example.com");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, ApprovedRoleNames.Administrator);

        var response = await client.GetAsync("/api/auth/admin-check");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
