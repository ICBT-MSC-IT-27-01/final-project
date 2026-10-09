using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AnuradhapuraAI.Infrastructure.Forecasting;

public sealed class ForecastService(
    IWeatherForecastingClient forecastingClient,
    AnuradhapuraAiDbContext dbContext,
    IOptions<WeatherModelFeatureOptions> featureOptions,
    TimeProvider timeProvider) : IForecastService
{
    public async Task<ForecastResult<ForecastResponse>> GenerateForecastAsync(
        CreateForecastRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!featureOptions.Value.EnableWeatherModelIntegration)
        {
            return ForecastResult<ForecastResponse>.Failure(
                ForecastErrorCodes.IntegrationDisabled,
                "Weather model integration is disabled.");
        }

        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return ForecastResult<ForecastResponse>.Failure(ForecastErrorCodes.InvalidInput, validationError);
        }

        var forecastResult = await forecastingClient.GenerateForecastAsync(request, cancellationToken);
        if (!forecastResult.Succeeded || forecastResult.Value is null)
        {
            return forecastResult;
        }

        var response = forecastResult.Value;
        if (response.Forecasts.Count != 7)
        {
            return ForecastResult<ForecastResponse>.Failure(
                ForecastErrorCodes.DownstreamError,
                "Forecasting service returned an invalid forecast horizon.");
        }

        try
        {
            var createdAt = timeProvider.GetUtcNow();
            var forecastRunId = Guid.NewGuid();
            foreach (var forecast in response.Forecasts)
            {
                dbContext.ForecastRecords.Add(new ForecastRecord
                {
                    ForecastRunId = forecastRunId,
                    ForecastDate = forecast.ForecastDate,
                    TargetDate = forecast.TargetDate,
                    Temperature = forecast.Temperature,
                    Rainfall = forecast.Rainfall,
                    Humidity = forecast.Humidity,
                    ModelVersion = forecast.ModelVersion,
                    CreatedAt = createdAt
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return ForecastResult<ForecastResponse>.Failure(
                ForecastErrorCodes.PersistenceFailed,
                "Forecast records could not be saved.");
        }

        return forecastResult;
    }

    private static string? ValidateRequest(CreateForecastRequest request)
    {
        if (request.Observations.Count != 30)
        {
            return "Exactly 30 daily observations are required.";
        }

        var previousDate = request.Observations[0].Date;
        for (var index = 0; index < request.Observations.Count; index++)
        {
            var observation = request.Observations[index];
            if (index > 0 && observation.Date != previousDate.AddDays(1))
            {
                return "Observations must be chronological daily values without gaps.";
            }

            if (observation.Temperature is null || observation.Rainfall is null || observation.Humidity is null)
            {
                return "Temperature, rainfall, and humidity are required for every observation.";
            }

            previousDate = observation.Date;
        }

        return null;
    }
}
