using System.Net;
using AnuradhapuraAI.Application.Forecasting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class UnhandledExceptionEndpointTests
{
    [Fact]
    public async Task UnexpectedFailure_ReturnsSafeJsonWithoutRequestCredentials()
    {
        using var factory = new Phase3WebApplicationFactory().CreateIsolated(services =>
        {
            services.RemoveAll<IForecastService>();
            services.AddScoped<IForecastService, ThrowingForecastService>();
        });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer test-secret-marker");

        using var response = await client.GetAsync("/api/forecasts/latest");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("An unexpected server error occurred.", body);
        Assert.DoesNotContain("test-secret-marker", body);
        Assert.DoesNotContain("ThrowingForecastService", body);
    }

    private sealed class ThrowingForecastService : IForecastService
    {
        public Task<ForecastResult<ForecastResponse>> GenerateForecastAsync(
            CreateForecastRequest request,
            CancellationToken cancellationToken = default) => throw new InvalidOperationException("Unexpected test failure.");

        public Task<ForecastResult<LatestForecastResponse>> GetLatestForecastAsync(
            CancellationToken cancellationToken = default) => throw new InvalidOperationException("Unexpected test failure.");
    }
}
