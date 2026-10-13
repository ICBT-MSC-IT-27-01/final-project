using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Persistence;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace AnuradhapuraAI.Infrastructure.Forecasting;

public sealed class ForecastService(
    IWeatherForecastingClient forecastingClient,
    AnuradhapuraAiDbContext dbContext,
    IOptions<WeatherModelFeatureOptions> featureOptions,
    TimeProvider timeProvider) : IForecastService
{
    private const int ForecastHorizonDays = 7;

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
        var runMetadata = ValidateForecastResponse(response);
        if (runMetadata is null)
        {
            return ForecastResult<ForecastResponse>.Failure(
                ForecastErrorCodes.DownstreamError,
                "Forecasting service returned an invalid forecast run.");
        }

        try
        {
            var createdAt = timeProvider.GetUtcNow();
            var forecastRunId = Guid.NewGuid();

            dbContext.ForecastRuns.Add(new ForecastRun
            {
                ForecastRunId = forecastRunId,
                RunPurpose = ApprovedForecastRunPurposes.LegacyUnknown,
                CreatedAt = createdAt,
                ForecastOriginDate = runMetadata.ForecastOriginDate,
                TargetStartDate = runMetadata.TargetStartDate,
                TargetEndDate = runMetadata.TargetEndDate,
                ModelVersion = runMetadata.ModelVersion
            });

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

    public async Task<ForecastResult<LatestForecastResponse>> GetLatestForecastAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var runs = await dbContext.ForecastRuns
                .AsNoTracking()
                .Include(run => run.ForecastRecords)
                .Where(run => run.RunPurpose == ApprovedForecastRunPurposes.Operational)
                .ToListAsync(cancellationToken);

            var selectedRun = runs
                .Select(OperationalForecastRunSelector.ToLatestForecastOrNull)
                .Where(run => run is not null)
                .OrderByDescending(run => run!.CreatedAt)
                .ThenBy(run => run!.ForecastRunId.ToString("D"), StringComparer.Ordinal)
                .FirstOrDefault();

            return selectedRun is null
                ? ForecastResult<LatestForecastResponse>.Failure(
                    ForecastErrorCodes.ForecastUnavailable,
                    "No complete forecast is available.")
                : ForecastResult<LatestForecastResponse>.Success(selectedRun);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsDatabaseReadFailure(ex))
        {
            return ForecastResult<LatestForecastResponse>.Failure(
                ForecastErrorCodes.DatabaseReadFailed,
                "Forecast data is temporarily unavailable.");
        }
        catch
        {
            return ForecastResult<LatestForecastResponse>.Failure(
                ForecastErrorCodes.UnexpectedFailure,
                "Forecast request failed.");
        }
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

    private static ForecastRunMetadata? ValidateForecastResponse(ForecastResponse response)
    {
        if (response.Forecasts.Count != ForecastHorizonDays)
        {
            return null;
        }

        var records = response.Forecasts
            .OrderBy(forecast => forecast.TargetDate)
            .ToList();

        if (records.Select(forecast => forecast.ForecastDate).Distinct().Count() != 1 ||
            records.Select(forecast => forecast.TargetDate).Distinct().Count() != ForecastHorizonDays ||
            records.Select(forecast => forecast.ModelVersion).Distinct(StringComparer.Ordinal).Count() != 1 ||
            !string.Equals(records[0].ModelVersion, response.ModelVersion, StringComparison.Ordinal) ||
            records.Any(forecast =>
                forecast.Rainfall < 0m ||
                forecast.Humidity < 0m ||
                forecast.Humidity > 100m ||
                !IsFinite(forecast.Rainfall) ||
                !IsFinite(forecast.Temperature) ||
                !IsFinite(forecast.Humidity)))
        {
            return null;
        }

        for (var index = 1; index < records.Count; index++)
        {
            if (records[index].TargetDate != records[index - 1].TargetDate.AddDays(1))
            {
                return null;
            }
        }

        return new ForecastRunMetadata(
            records[0].ForecastDate,
            records[0].TargetDate,
            records[^1].TargetDate,
            response.ModelVersion);
    }

    private static bool IsFinite(decimal value) =>
        value != decimal.MinValue && value != decimal.MaxValue;

    private static bool IsDatabaseReadFailure(Exception exception) =>
        exception is DbException or TimeoutException or RetryLimitExceededException ||
        exception.InnerException is not null && IsDatabaseReadFailure(exception.InnerException);

    private sealed record ForecastRunMetadata(
        DateOnly ForecastOriginDate,
        DateOnly TargetStartDate,
        DateOnly TargetEndDate,
        string ModelVersion);
}
