using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class ForecastEndpointTests(Phase3WebApplicationFactory factory) : IClassFixture<Phase3WebApplicationFactory>
{
    [Fact]
    public async Task CreateForecast_ReturnsForecastAndPersistsRecords()
    {
        var fakeClient = new FakeForecastingClient(ForecastResult<ForecastResponse>.Success(SampleForecastResponse()));
        using var testFactory = factory.CreateIsolated(services =>
        {
            EnableWeatherModelIntegration(services);
            services.RemoveAll<IWeatherForecastingClient>();
            services.AddSingleton<IWeatherForecastingClient>(fakeClient);
        });
        var client = testFactory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/forecasts", SampleRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ForecastResponse>();
        Assert.NotNull(body);
        Assert.Equal("v1", body!.ModelVersion);
        Assert.Equal(7, body.Forecasts.Count);
        Assert.True(fakeClient.WasCalled);

        using var scope = testFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AnuradhapuraAiDbContext>();
        Assert.Equal(7, dbContext.ForecastRecords.Count());
        Assert.Empty(dbContext.Recommendations);
    }

    [Fact]
    public async Task CreateForecast_ReturnsServiceUnavailable_WhenFeatureFlagDisabled()
    {
        var fakeClient = new FakeForecastingClient(ForecastResult<ForecastResponse>.Success(SampleForecastResponse()));
        using var testFactory = factory.CreateIsolated(services =>
        {
            services.RemoveAll<IWeatherForecastingClient>();
            services.AddSingleton<IWeatherForecastingClient>(fakeClient);
        });
        var client = testFactory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/forecasts", SampleRequest());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.False(fakeClient.WasCalled);
    }

    [Fact]
    public async Task CreateForecast_ReturnsBadRequest_ForInvalidObservationCount()
    {
        using var testFactory = factory.CreateIsolated(services =>
        {
            EnableWeatherModelIntegration(services);
            services.RemoveAll<IWeatherForecastingClient>();
            services.AddSingleton<IWeatherForecastingClient>(new FakeForecastingClient(ForecastResult<ForecastResponse>.Success(SampleForecastResponse())));
        });
        var client = testFactory.CreateClient();
        var request = new CreateForecastRequest(SampleRequest().Observations.Take(29).ToList());

        var response = await client.PostAsJsonAsync("/api/forecasts", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateForecast_ReturnsBadRequest_ForMissingWeatherVariable()
    {
        using var testFactory = factory.CreateIsolated(services =>
        {
            EnableWeatherModelIntegration(services);
            services.RemoveAll<IWeatherForecastingClient>();
            services.AddSingleton<IWeatherForecastingClient>(new FakeForecastingClient(ForecastResult<ForecastResponse>.Success(SampleForecastResponse())));
        });
        var client = testFactory.CreateClient();
        var observations = string.Join(
            ",",
            Enumerable.Range(0, 30).Select(index =>
            {
                var date = new DateOnly(2025, 1, 1).AddDays(index).ToString("yyyy-MM-dd");
                return index == 0
                    ? $"{{\"date\":\"{date}\",\"temperature\":25,\"rainfall\":1}}"
                    : $"{{\"date\":\"{date}\",\"temperature\":25,\"rainfall\":1,\"humidity\":80}}";
            }));
        using var content = new StringContent($$"""{"observations":[{{observations}}]}""", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/forecasts", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateForecast_ReturnsBadGateway_ForMalformedDownstreamResponse()
    {
        using var testFactory = factory.CreateIsolated(services =>
        {
            EnableWeatherModelIntegration(services);
            services.RemoveAll<IWeatherForecastingClient>();
            services.AddSingleton<IWeatherForecastingClient>(new FakeForecastingClient(
                ForecastResult<ForecastResponse>.Failure(ForecastErrorCodes.DownstreamError, "Malformed response.")));
        });
        var client = testFactory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/forecasts", SampleRequest());

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task CreateForecast_ReturnsServiceUnavailable_WhenForecastingServiceUnavailable()
    {
        using var testFactory = factory.CreateIsolated(services =>
        {
            EnableWeatherModelIntegration(services);
            services.RemoveAll<IWeatherForecastingClient>();
            services.AddSingleton<IWeatherForecastingClient>(new FakeForecastingClient(
                ForecastResult<ForecastResponse>.Failure(ForecastErrorCodes.ServiceUnavailable, "Unavailable.")));
        });
        var client = testFactory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/forecasts", SampleRequest());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private static void EnableWeatherModelIntegration(IServiceCollection services)
    {
        services.Configure<WeatherModelFeatureOptions>(options => options.EnableWeatherModelIntegration = true);
    }

    private static CreateForecastRequest SampleRequest()
    {
        var start = new DateOnly(2025, 1, 1);
        var observations = Enumerable.Range(0, 30)
            .Select(index => new ForecastObservationRequest(
                start.AddDays(index),
                Temperature: 25 + index,
                Rainfall: index,
                Humidity: 70 + index))
            .ToList();
        return new CreateForecastRequest(observations);
    }

    private static ForecastResponse SampleForecastResponse()
    {
        var forecastDate = new DateOnly(2025, 1, 30);
        var forecasts = Enumerable.Range(1, 7)
            .Select(index => new ForecastDayResponse(
                forecastDate,
                forecastDate.AddDays(index),
                Temperature: 26 + index,
                Rainfall: index,
                Humidity: 80 + index,
                ModelVersion: "v1"))
            .ToList();
        return new ForecastResponse("v1", forecastDate, forecasts);
    }

    private sealed class FakeForecastingClient(ForecastResult<ForecastResponse> result) : IWeatherForecastingClient
    {
        public bool WasCalled { get; private set; }

        public Task<ForecastResult<ForecastResponse>> GenerateForecastAsync(
            CreateForecastRequest request,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(result);
        }
    }
}
