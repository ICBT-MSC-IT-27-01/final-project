using System.Net;
using AnuradhapuraAI.Api.Controllers;
using AnuradhapuraAI.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class Phase4AdminAuthorizationTests : IClassFixture<Phase3WebApplicationFactory>
{
    private readonly Phase3WebApplicationFactory factory;

    public Phase4AdminAuthorizationTests(Phase3WebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task AdminRoute_UnauthenticatedRequest_IsRejected()
    {
        using var testFactory = factory.CreateIsolated();
        var client = testFactory.CreateClient();

        var response = await client.GetAsync("/api/admin/crops");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Registered User")]
    [InlineData("Agricultural Officer")]
    public async Task AdminRoute_NonAdministrator_IsForbidden(string role)
    {
        using var testFactory = factory.CreateIsolated();
        var client = testFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.EmailHeader, "user@example.com");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, role);

        var response = await client.GetAsync("/api/admin/crops");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public void AdminController_RequiresAdministratorOnlyPolicy()
    {
        var authorize = typeof(AdminController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal("AdministratorOnly", authorize.Policy);
    }
}
