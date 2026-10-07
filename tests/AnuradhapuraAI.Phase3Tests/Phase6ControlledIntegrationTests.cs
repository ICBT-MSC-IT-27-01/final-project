using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class Phase6ControlledIntegrationTests(Phase3WebApplicationFactory factory) : IClassFixture<Phase3WebApplicationFactory>
{
    [Fact]
    public async Task ForecastEndpoint_CallsRunningPythonService_AndPersistsSevenForecastRecords()
    {
        var baseUrl = Environment.GetEnvironmentVariable("PHASE6_FORECASTING_SERVICE_BASE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        using var testFactory = factory.CreateIsolated(services =>
        {
            services.Configure<WeatherModelFeatureOptions>(options => options.EnableWeatherModelIntegration = true);
            services.Configure<ForecastingServiceOptions>(options => options.BaseUrl = baseUrl);
        });
        var client = testFactory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/forecasts", HistoricalWindowRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ForecastResponse>();
        Assert.NotNull(body);
        Assert.Equal("v1", body!.ModelVersion);
        Assert.Equal(new DateOnly(2025, 12, 1), body.ForecastDate);
        Assert.Equal(7, body.Forecasts.Count);
        Assert.Equal(new DateOnly(2025, 12, 2), body.Forecasts[0].TargetDate);
        Assert.Equal(new DateOnly(2025, 12, 8), body.Forecasts[^1].TargetDate);

        using var scope = testFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AnuradhapuraAiDbContext>();
        Assert.Equal(7, dbContext.ForecastRecords.Count());
    }

    private static CreateForecastRequest HistoricalWindowRequest()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var datasetPath = Path.Combine(repoRoot, "docs", "anuradhapura_weather_2010_2025_era5.csv");
        var rows = File.ReadLines(datasetPath)
            .Skip(1)
            .Select(line => line.Split(','))
            .Where(columns => columns.Length == 4)
            .Select(columns => new ForecastObservationRequest(
                DateOnly.Parse(columns[0], CultureInfo.InvariantCulture),
                Temperature: decimal.Parse(columns[2], CultureInfo.InvariantCulture),
                Rainfall: decimal.Parse(columns[1], CultureInfo.InvariantCulture),
                Humidity: decimal.Parse(columns[3], CultureInfo.InvariantCulture)))
            .Where(row => row.Date >= new DateOnly(2025, 11, 2) && row.Date <= new DateOnly(2025, 12, 1))
            .ToList();

        Assert.Equal(30, rows.Count);
        return new CreateForecastRequest(rows);
    }
}
