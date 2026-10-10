using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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
    public async Task GetLatestForecast_ReturnsLatestCompleteRun_ForAnonymousUsers()
    {
        using var testFactory = factory.CreateIsolated();
        await SeedForecastRunAsync(testFactory, Guid.Parse("00000000-0000-0000-0000-000000000101"), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var latestRunId = Guid.Parse("00000000-0000-0000-0000-000000000202");
        await SeedForecastRunAsync(testFactory, latestRunId, new DateOnly(2026, 1, 8), DateTimeOffset.Parse("2026-01-02T00:00:00Z"));
        var client = testFactory.CreateClient();

        var response = await client.GetAsync("/api/forecasts/latest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LatestForecastResponse>();
        Assert.NotNull(body);
        Assert.Equal(latestRunId, body!.ForecastRunId);
        Assert.Equal("Anuradhapura", body.District);
        Assert.Equal("v1", body.ModelVersion);
        Assert.Equal(new DateOnly(2026, 1, 9), body.ForecastPeriodStart);
        Assert.Equal(new DateOnly(2026, 1, 15), body.ForecastPeriodEnd);
        Assert.Equal(7, body.DailyForecasts.Count);
        Assert.Equal(body.DailyForecasts.OrderBy(day => day.TargetDate).Select(day => day.TargetDate), body.DailyForecasts.Select(day => day.TargetDate));
        Assert.Contains(body.Limitations, limitation => limitation.Contains("AI-predicted", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetLatestForecast_ReturnsNotFound_WhenNoForecastRecordsExist()
    {
        using var testFactory = factory.CreateIsolated();
        var client = testFactory.CreateClient();

        var response = await client.GetAsync("/api/forecasts/latest");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiMessage>();
        Assert.Equal("No complete forecast is available.", body!.Message);
    }

    [Fact]
    public async Task GetLatestForecast_ReturnsNotFound_WhenNoCompleteRunExists()
    {
        using var testFactory = factory.CreateIsolated();
        await SeedForecastRunAsync(testFactory, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-01T00:00:00Z"), count: 6);
        var client = testFactory.CreateClient();

        var response = await client.GetAsync("/api/forecasts/latest");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiMessage>();
        Assert.Equal("No complete forecast is available.", body!.Message);
    }

    [Fact]
    public async Task GetLatestForecast_SkipsInvalidNewestRunAndReturnsOlderValidRun()
    {
        using var testFactory = factory.CreateIsolated();
        var validRunId = Guid.Parse("00000000-0000-0000-0000-000000000303");
        await SeedForecastRunAsync(testFactory, validRunId, new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        await SeedForecastRunAsync(testFactory, Guid.NewGuid(), new DateOnly(2026, 2, 1), DateTimeOffset.Parse("2026-01-02T00:00:00Z"), targetDateGap: true);
        var client = testFactory.CreateClient();

        var response = await client.GetAsync("/api/forecasts/latest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LatestForecastResponse>();
        Assert.Equal(validRunId, body!.ForecastRunId);
    }

    [Fact]
    public async Task GetLatestForecast_RejectsDuplicateNonConsecutiveAndInconsistentMetadataRuns()
    {
        using var testFactory = factory.CreateIsolated();
        await SeedForecastRunAsync(testFactory, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-04T00:00:00Z"), duplicateTargetDate: true);
        await SeedForecastRunAsync(testFactory, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-03T00:00:00Z"), targetDateGap: true);
        await SeedForecastRunAsync(testFactory, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-02T00:00:00Z"), inconsistentForecastDate: true);
        await SeedForecastRunAsync(testFactory, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-01T00:00:00Z"), inconsistentModelVersion: true);
        var client = testFactory.CreateClient();

        var response = await client.GetAsync("/api/forecasts/latest");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("rainfall")]
    [InlineData("temperature")]
    [InlineData("humidity")]
    public async Task GetLatestForecast_RejectsInvalidWeatherValues(string invalidField)
    {
        using var testFactory = factory.CreateIsolated();
        await SeedForecastRunAsync(
            testFactory,
            Guid.NewGuid(),
            new DateOnly(2026, 1, 1),
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            invalidField: invalidField);
        var client = testFactory.CreateClient();

        var response = await client.GetAsync("/api/forecasts/latest");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLatestForecast_AllowsCreatedAtVariationsAndUsesLatestRunTimestamp()
    {
        using var testFactory = factory.CreateIsolated();
        var runId = Guid.Parse("00000000-0000-0000-0000-000000000404");
        var createdAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        await SeedForecastRunAsync(testFactory, runId, new DateOnly(2026, 1, 1), createdAt, createdAtVariation: true);
        var client = testFactory.CreateClient();

        var response = await client.GetAsync("/api/forecasts/latest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LatestForecastResponse>();
        Assert.Equal(runId, body!.ForecastRunId);
        Assert.Equal(createdAt.AddMinutes(6), body.CreatedAt);
    }

    [Fact]
    public async Task GetLatestForecast_UsesDeterministicTieOrdering()
    {
        using var testFactory = factory.CreateIsolated();
        var sameCreatedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var firstTie = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var secondTie = Guid.Parse("00000000-0000-0000-0000-000000000002");
        await SeedForecastRunAsync(testFactory, secondTie, new DateOnly(2026, 1, 1), sameCreatedAt);
        await SeedForecastRunAsync(testFactory, firstTie, new DateOnly(2026, 2, 1), sameCreatedAt);
        var client = testFactory.CreateClient();

        var response = await client.GetAsync("/api/forecasts/latest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LatestForecastResponse>();
        Assert.Equal(firstTie, body!.ForecastRunId);
    }

    [Fact]
    public async Task GetLatestForecast_DoesNotCallForecastGenerationClientOrPersistData()
    {
        var fakeClient = new FakeForecastingClient(ForecastResult<ForecastResponse>.Success(SampleForecastResponse()));
        using var testFactory = factory.CreateIsolated(services =>
        {
            services.RemoveAll<IWeatherForecastingClient>();
            services.AddSingleton<IWeatherForecastingClient>(fakeClient);
        });
        await SeedForecastRunAsync(testFactory, Guid.NewGuid(), new DateOnly(2026, 1, 1), DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var client = testFactory.CreateClient();

        var response = await client.GetAsync("/api/forecasts/latest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(fakeClient.WasCalled);
        using var scope = testFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AnuradhapuraAiDbContext>();
        Assert.Equal(7, await dbContext.ForecastRecords.CountAsync());
    }

    [Fact]
    public async Task GetLatestForecast_MapsDatabaseFailureToSafeServiceUnavailable()
    {
        using var testFactory = factory.CreateIsolated(services =>
        {
            services.RemoveAll<IForecastService>();
            services.AddScoped<IForecastService>(_ => new FakeForecastService(
                ForecastResult<LatestForecastResponse>.Failure(ForecastErrorCodes.DatabaseReadFailed, "Forecast data is temporarily unavailable.")));
        });
        var client = testFactory.CreateClient();

        var response = await client.GetAsync("/api/forecasts/latest");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiMessage>();
        Assert.Equal("Forecast data is temporarily unavailable.", body!.Message);
    }

    [Fact]
    public async Task GetLatestForecast_MapsUnexpectedFailureToSafeInternalServerError()
    {
        using var testFactory = factory.CreateIsolated(services =>
        {
            services.RemoveAll<IForecastService>();
            services.AddScoped<IForecastService>(_ => new FakeForecastService(
                ForecastResult<LatestForecastResponse>.Failure(ForecastErrorCodes.UnexpectedFailure, "Forecast request failed.")));
        });
        var client = testFactory.CreateClient();

        var response = await client.GetAsync("/api/forecasts/latest");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiMessage>();
        Assert.Equal("Forecast request failed.", body!.Message);
    }

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
        var forecastRunIds = dbContext.ForecastRecords.Select(record => record.ForecastRunId).Distinct().ToList();
        Assert.Single(forecastRunIds);
        Assert.NotEqual(Guid.Empty, forecastRunIds[0]);
        Assert.Empty(dbContext.Recommendations);
    }

    [Fact]
    public async Task CreateForecast_UsesDifferentForecastRunId_ForDifferentExecutions()
    {
        var fakeClient = new FakeForecastingClient(ForecastResult<ForecastResponse>.Success(SampleForecastResponse()));
        using var testFactory = factory.CreateIsolated(services =>
        {
            EnableWeatherModelIntegration(services);
            services.RemoveAll<IWeatherForecastingClient>();
            services.AddSingleton<IWeatherForecastingClient>(fakeClient);
        });
        var client = testFactory.CreateClient();

        var firstResponse = await client.PostAsJsonAsync("/api/forecasts", SampleRequest());
        var secondResponse = await client.PostAsJsonAsync("/api/forecasts", SampleRequest());

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        using var scope = testFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AnuradhapuraAiDbContext>();
        var forecastRunIds = dbContext.ForecastRecords
            .Select(record => record.ForecastRunId)
            .Distinct()
            .ToList();
        Assert.Equal(2, forecastRunIds.Count);
        Assert.DoesNotContain(Guid.Empty, forecastRunIds);
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

    private static async Task SeedForecastRunAsync(
        Phase3WebApplicationFactory testFactory,
        Guid runId,
        DateOnly forecastDate,
        DateTimeOffset createdAt,
        int count = 7,
        bool duplicateTargetDate = false,
        bool targetDateGap = false,
        bool inconsistentModelVersion = false,
        bool inconsistentForecastDate = false,
        bool createdAtVariation = false,
        string? invalidField = null)
    {
        using var scope = testFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AnuradhapuraAiDbContext>();
        for (var index = 0; index < count; index++)
        {
            var targetOffset = targetDateGap && index == 6 ? 8 : index + 1;
            var targetDate = duplicateTargetDate && index == 6
                ? forecastDate.AddDays(1)
                : forecastDate.AddDays(targetOffset);

            dbContext.ForecastRecords.Add(new ForecastRecord
            {
                ForecastRunId = runId,
                ForecastDate = inconsistentForecastDate && index == 0 ? forecastDate.AddDays(-1) : forecastDate,
                TargetDate = targetDate,
                Rainfall = invalidField == "rainfall" && index == 0 ? -1m : 1m + index,
                Temperature = invalidField == "temperature" && index == 0 ? decimal.MaxValue : 26m + index,
                Humidity = invalidField == "humidity" && index == 0 ? 101m : 70m + index,
                ModelVersion = inconsistentModelVersion && index == 0 ? "v0" : "v1",
                CreatedAt = createdAtVariation ? createdAt.AddMinutes(index) : createdAt
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private sealed record ApiMessage(string? Message);

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

    private sealed class FakeForecastService(ForecastResult<LatestForecastResponse> latestForecastResult) : IForecastService
    {
        public Task<ForecastResult<ForecastResponse>> GenerateForecastAsync(
            CreateForecastRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ForecastResult<ForecastResponse>.Failure(
                ForecastErrorCodes.ServiceUnavailable,
                "Forecasting service is unavailable."));

        public Task<ForecastResult<LatestForecastResponse>> GetLatestForecastAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(latestForecastResult);
    }
}
