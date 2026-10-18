using System.Net;
using AnuradhapuraAI.Api.Controllers;
using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

    [Theory]
    [InlineData("Registered User")]
    [InlineData("Agricultural Officer")]
    public async Task OperationalForecastTrigger_NonAdministrator_IsForbidden(string role)
    {
        using var testFactory = factory.CreateIsolated();
        var client = testFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.EmailHeader, "user@example.com");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, role);

        var response = await client.PostAsync("/api/admin/forecasts/operational", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OperationalForecastTrigger_AdministratorReceivesFeatureDisabledResponse_WhenIntegrationFlagOff()
    {
        using var testFactory = factory.CreateIsolated(services =>
        {
            services.PostConfigure<WeatherModelFeatureOptions>(options =>
                options.EnableWeatherModelIntegration = false);
        });
        var client = testFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.EmailHeader, "admin@example.com");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, ApprovedRoleNames.Administrator);

        var response = await client.PostAsync("/api/admin/forecasts/operational", content: null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
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
