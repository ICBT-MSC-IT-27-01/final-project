using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace AnuradhapuraAI.Infrastructure.Forecasting;

public sealed class ProviderForecastOrchestrationService(
    IWeatherObservationSource weatherObservationSource,
    IFreshWeatherObservationSource freshWeatherObservationSource,
    IWeatherForecastingClient forecastingClient,
    AnuradhapuraAiDbContext dbContext,
    IOptions<WeatherModelFeatureOptions> featureOptions,
    TimeProvider timeProvider) : IProviderForecastOrchestrationService
{
    private const int RequiredObservationDays = 30;
    private const int ForecastHorizonDays = 7;
    private const decimal ApprovedLatitude = 8.312229m;
    private const decimal ApprovedLongitude = 80.413055m;
    private const string ProviderName = "Open-Meteo";
    private const string ApprovedProduct = "era5";
    private const string ApprovedOperationalSource = "OpenMeteoForecastPastDays";
    private const string ApprovedOperationalProduct = OpenMeteoForecastPastDaysWeatherSource.ApprovedProduct;
    private const string ApprovedTimezone = "Asia/Colombo";
    private const decimal MinimumPlausibleTemperature = -20m;
    private const decimal MaximumPlausibleTemperature = 60m;

    public async Task<ProviderForecastPreparationResult> PrepareHistoricalForecastAsync(
        ProviderForecastPreparationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!featureOptions.Value.EnableWeatherModelIntegration)
        {
            return ProviderForecastPreparationResult.Failure(
                ProviderForecastPreparationErrorCodes.FeatureDisabled,
                "Weather model integration is disabled.");
        }

        var currentDateResult = TryGetCurrentDate();
        if (!currentDateResult.Succeeded)
        {
            return ProviderForecastPreparationResult.Failure(
                ProviderForecastPreparationErrorCodes.InvalidObservationWindow,
                currentDateResult.Message);
        }

        var windowError = ValidateWindow(request, currentDateResult.CurrentDate);
        if (windowError is not null)
        {
            return ProviderForecastPreparationResult.Failure(
                ProviderForecastPreparationErrorCodes.InvalidObservationWindow,
                windowError);
        }

        var sourceRequest = new WeatherObservationSourceRequest(
            ApprovedLatitude,
            ApprovedLongitude,
            request.ObservationWindowStart,
            request.ObservationWindowEnd,
            ApprovedProduct,
            ApprovedTimezone);

        var sourceResult = await weatherObservationSource.GetDailyObservationsAsync(sourceRequest, cancellationToken);
        if (!sourceResult.Succeeded || sourceResult.Value is null)
        {
            return ProviderForecastPreparationResult.Failure(
                ToPreparationErrorCode(sourceResult.ErrorCode),
                SafeProviderMessage(sourceResult.ErrorCode));
        }

        var observations = sourceResult.Value.Observations;
        var dataError = ValidateObservations(request, observations, currentDateResult.CurrentDate);
        if (dataError is not null)
        {
            return ProviderForecastPreparationResult.Failure(
                ProviderForecastPreparationErrorCodes.InvalidProviderData,
                dataError);
        }

        var forecastRequest = new CreateForecastRequest(observations
            .Select(observation => new ForecastObservationRequest(
                observation.Date,
                observation.Temperature,
                observation.Rainfall,
                observation.Humidity))
            .ToList());

        var forecastOriginDate = request.ObservationWindowEnd;
        var targetDates = Enumerable.Range(1, ForecastHorizonDays)
            .Select(forecastOriginDate.AddDays)
            .ToList();

        var provenance = new ProviderForecastProvenance(
            ProviderName,
            ApprovedProduct,
            ApprovedLatitude,
            ApprovedLongitude,
            sourceResult.Value.Metadata.ReturnedLatitude,
            sourceResult.Value.Metadata.ReturnedLongitude,
            ApprovedTimezone,
            request.ObservationWindowStart,
            request.ObservationWindowEnd,
            forecastOriginDate,
            targetDates[0],
            targetDates[^1],
            ProviderForecastRunPurpose.HistoricalEvaluation,
            sourceResult.Value.Metadata.RetrievedAt);

        return ProviderForecastPreparationResult.Success(new PreparedProviderForecast(
            forecastRequest,
            request.ObservationWindowStart,
            request.ObservationWindowEnd,
            forecastOriginDate,
            targetDates,
            ProviderForecastRunPurpose.HistoricalEvaluation,
            provenance));
    }

    public async Task<ProviderForecastGenerationResult> GenerateOperationalForecastAsync(
        CancellationToken cancellationToken = default)
    {
        if (!featureOptions.Value.EnableWeatherModelIntegration)
        {
            return ProviderForecastGenerationResult.Failure(
                ProviderForecastPreparationErrorCodes.FeatureDisabled,
                "Weather model integration is disabled.");
        }

        if (!string.Equals(
                featureOptions.Value.OperationalWeatherSource,
                ApprovedOperationalSource,
                StringComparison.Ordinal))
        {
            return ProviderForecastGenerationResult.Failure(
                ProviderForecastPreparationErrorCodes.UnsupportedOperationalSource,
                "Operational weather source is not configured for the approved fresh Open-Meteo Forecast API past-days adapter.");
        }

        if (!featureOptions.Value.AcceptOperationalWeatherSource)
        {
            return ProviderForecastGenerationResult.Failure(
                ProviderForecastPreparationErrorCodes.OperationalSourceNotAccepted,
                "Operational weather source acceptance is pending real-data compatibility evaluation and owner approval.");
        }

        var currentDateResult = TryGetCurrentDate();
        if (!currentDateResult.Succeeded)
        {
            return ProviderForecastGenerationResult.Failure(
                ProviderForecastPreparationErrorCodes.InvalidObservationWindow,
                currentDateResult.Message);
        }

        var observationEnd = currentDateResult.CurrentDate.AddDays(-1);
        var observationStart = observationEnd.AddDays(-(RequiredObservationDays - 1));
        var preparationResult = await PrepareOperationalForecastAsync(
            new ProviderForecastPreparationRequest(observationStart, observationEnd),
            cancellationToken);

        if (!preparationResult.Succeeded || preparationResult.Value is null)
        {
            return ProviderForecastGenerationResult.Failure(
                preparationResult.ErrorCode ?? ProviderForecastPreparationErrorCodes.InvalidProviderData,
                preparationResult.Message ?? "Weather provider observations were invalid.");
        }

        var prepared = preparationResult.Value;
        var forecastResult = await forecastingClient.GenerateForecastAsync(prepared.ForecastRequest, cancellationToken);
        if (!forecastResult.Succeeded || forecastResult.Value is null)
        {
            return ProviderForecastGenerationResult.Failure(
                ToGenerationErrorCode(forecastResult.ErrorCode),
                SafeForecastingMessage(forecastResult.ErrorCode));
        }

        var response = forecastResult.Value;
        var forecastError = ValidateForecastResponse(
            response,
            prepared.ForecastOriginDate,
            prepared.ForecastTargetDates);
        if (forecastError is not null)
        {
            return ProviderForecastGenerationResult.Failure(
                ProviderForecastPreparationErrorCodes.InvalidForecastOutput,
                forecastError);
        }

        var createdAt = timeProvider.GetUtcNow();
        var forecastRunId = Guid.NewGuid();
        var provenance = prepared.Provenance;

        await using var transaction = await TryBeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.ForecastRuns.Add(new ForecastRun
            {
                ForecastRunId = forecastRunId,
                RunPurpose = ApprovedForecastRunPurposes.Operational,
                CreatedAt = createdAt,
                ForecastOriginDate = prepared.ForecastOriginDate,
                TargetStartDate = prepared.ForecastTargetDates[0],
                TargetEndDate = prepared.ForecastTargetDates[^1],
                ModelVersion = response.ModelVersion,
                SourceProvider = provenance.ProviderName,
                SourceProduct = provenance.ProviderProduct,
                RequestedLatitude = provenance.RequestedLatitude,
                RequestedLongitude = provenance.RequestedLongitude,
                Timezone = provenance.Timezone,
                ObservationStartDate = provenance.ObservationWindowStart,
                ObservationEndDate = provenance.ObservationWindowEnd,
                SourceRetrievedAt = provenance.LocallyRecordedRetrievedAt,
                DataQualityStatus = "Accepted"
            });

            foreach (var forecast in response.Forecasts.OrderBy(forecast => forecast.TargetDate))
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
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateException)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            return ProviderForecastGenerationResult.Failure(
                ProviderForecastPreparationErrorCodes.PersistenceFailed,
                "Operational forecast run could not be saved.");
        }

        return ProviderForecastGenerationResult.Success(new OperationalForecastGenerationResponse(
            forecastRunId,
            ApprovedForecastRunPurposes.Operational,
            provenance.ProviderName,
            provenance.ProviderProduct,
            provenance.RequestedLatitude,
            provenance.RequestedLongitude,
            provenance.ReturnedLatitude,
            provenance.ReturnedLongitude,
            provenance.Timezone,
            provenance.ObservationWindowStart,
            provenance.ObservationWindowEnd,
            prepared.ForecastOriginDate,
            prepared.ForecastTargetDates[0],
            prepared.ForecastTargetDates[^1],
            response.ModelVersion,
            createdAt,
            response.Forecasts.OrderBy(forecast => forecast.TargetDate).ToList()));
    }

    private async Task<ProviderForecastPreparationResult> PrepareOperationalForecastAsync(
        ProviderForecastPreparationRequest request,
        CancellationToken cancellationToken)
    {
        var currentDateResult = TryGetCurrentDate();
        if (!currentDateResult.Succeeded)
        {
            return ProviderForecastPreparationResult.Failure(
                ProviderForecastPreparationErrorCodes.InvalidObservationWindow,
                currentDateResult.Message);
        }

        var windowError = ValidateWindow(request, currentDateResult.CurrentDate);
        if (windowError is not null)
        {
            return ProviderForecastPreparationResult.Failure(
                ProviderForecastPreparationErrorCodes.InvalidObservationWindow,
                windowError);
        }

        if (request.ObservationWindowEnd != currentDateResult.CurrentDate.AddDays(-1))
        {
            return ProviderForecastPreparationResult.Failure(
                ProviderForecastPreparationErrorCodes.InvalidObservationWindow,
                "Operational observation range must end on yesterday in the approved local timezone.");
        }

        var sourceRequest = new WeatherObservationSourceRequest(
            ApprovedLatitude,
            ApprovedLongitude,
            request.ObservationWindowStart,
            request.ObservationWindowEnd,
            ApprovedOperationalProduct,
            ApprovedTimezone);

        var sourceResult = await freshWeatherObservationSource.GetDailyObservationsAsync(sourceRequest, cancellationToken);
        if (!sourceResult.Succeeded || sourceResult.Value is null)
        {
            return ProviderForecastPreparationResult.Failure(
                ToPreparationErrorCode(sourceResult.ErrorCode),
                SafeProviderMessage(sourceResult.ErrorCode));
        }

        var observations = sourceResult.Value.Observations;
        var dataError = ValidateObservations(request, observations, currentDateResult.CurrentDate);
        if (dataError is not null)
        {
            return ProviderForecastPreparationResult.Failure(
                ProviderForecastPreparationErrorCodes.InvalidProviderData,
                dataError);
        }

        var forecastRequest = new CreateForecastRequest(observations
            .Select(observation => new ForecastObservationRequest(
                observation.Date,
                observation.Temperature,
                observation.Rainfall,
                observation.Humidity))
            .ToList());

        var forecastOriginDate = request.ObservationWindowEnd;
        var targetDates = Enumerable.Range(1, ForecastHorizonDays)
            .Select(forecastOriginDate.AddDays)
            .ToList();

        var provenance = new ProviderForecastProvenance(
            ProviderName,
            ApprovedOperationalProduct,
            ApprovedLatitude,
            ApprovedLongitude,
            sourceResult.Value.Metadata.ReturnedLatitude,
            sourceResult.Value.Metadata.ReturnedLongitude,
            ApprovedTimezone,
            request.ObservationWindowStart,
            request.ObservationWindowEnd,
            forecastOriginDate,
            targetDates[0],
            targetDates[^1],
            ProviderForecastRunPurpose.Operational,
            sourceResult.Value.Metadata.RetrievedAt);

        return ProviderForecastPreparationResult.Success(new PreparedProviderForecast(
            forecastRequest,
            request.ObservationWindowStart,
            request.ObservationWindowEnd,
            forecastOriginDate,
            targetDates,
            ProviderForecastRunPurpose.Operational,
            provenance));
    }

    private static string? ValidateWindow(
        ProviderForecastPreparationRequest request,
        DateOnly currentDate)
    {
        if (request.ObservationWindowEnd < request.ObservationWindowStart)
        {
            return "Observation end date must not be before the start date.";
        }

        var days = request.ObservationWindowEnd.DayNumber - request.ObservationWindowStart.DayNumber + 1;
        if (days != RequiredObservationDays)
        {
            return "Exactly 30 daily observations are required.";
        }

        if (request.ObservationWindowEnd >= currentDate)
        {
            return "Observation range must not include the current or a future local date.";
        }

        return null;
    }

    private static string? ValidateObservations(
        ProviderForecastPreparationRequest request,
        IReadOnlyList<WeatherObservation> observations,
        DateOnly currentDate)
    {
        if (observations.Count != RequiredObservationDays)
        {
            return "Provider data must contain exactly 30 daily observations.";
        }

        var seenDates = new HashSet<DateOnly>();
        for (var index = 0; index < observations.Count; index++)
        {
            var observation = observations[index];
            if (!seenDates.Add(observation.Date))
            {
                return "Provider data contained duplicate observation dates.";
            }

            var expectedDate = request.ObservationWindowStart.AddDays(index);
            if (observation.Date != expectedDate)
            {
                return "Provider data must be chronological consecutive daily observations.";
            }

            if (observation.Date >= currentDate)
            {
                return "Provider data must not include the current or a future local date.";
            }

            var valueError = ValidateWeatherValues(observation);
            if (valueError is not null)
            {
                return valueError;
            }
        }

        if (observations[0].Date != request.ObservationWindowStart ||
            observations[^1].Date != request.ObservationWindowEnd)
        {
            return "Provider data did not match the requested observation window.";
        }

        return null;
    }

    private static string? ValidateWeatherValues(WeatherObservation observation)
    {
        if (!IsFinite(observation.Rainfall) ||
            !IsFinite(observation.Temperature) ||
            !IsFinite(observation.Humidity))
        {
            return "Provider data contained non-finite weather values.";
        }

        if (observation.Rainfall < 0m)
        {
            return "Provider data contained negative precipitation.";
        }

        if (observation.Humidity < 0m || observation.Humidity > 100m)
        {
            return "Provider data contained an invalid relative humidity value.";
        }

        if (observation.Temperature < MinimumPlausibleTemperature ||
            observation.Temperature > MaximumPlausibleTemperature)
        {
            return "Provider data contained an implausible temperature value.";
        }

        return null;
    }

    private static bool IsFinite(decimal value) =>
        value != decimal.MinValue && value != decimal.MaxValue;

    private static string? ValidateForecastResponse(
        ForecastResponse response,
        DateOnly forecastOriginDate,
        IReadOnlyList<DateOnly> targetDates)
    {
        if (response.Forecasts.Count != ForecastHorizonDays)
        {
            return "Forecasting service must return exactly seven daily predictions.";
        }

        if (response.ForecastDate != forecastOriginDate)
        {
            return "Forecasting service returned an unexpected forecast origin date.";
        }

        var orderedForecasts = response.Forecasts.OrderBy(forecast => forecast.TargetDate).ToList();
        if (!orderedForecasts.Select(forecast => forecast.TargetDate).SequenceEqual(targetDates) ||
            orderedForecasts.Select(forecast => forecast.TargetDate).Distinct().Count() != ForecastHorizonDays ||
            orderedForecasts.Select(forecast => forecast.ForecastDate).Distinct().Count() != 1 ||
            orderedForecasts.Select(forecast => forecast.ModelVersion).Distinct(StringComparer.Ordinal).Count() != 1 ||
            !string.Equals(orderedForecasts[0].ModelVersion, response.ModelVersion, StringComparison.Ordinal))
        {
            return "Forecasting service returned inconsistent forecast metadata.";
        }

        if (orderedForecasts.Any(forecast =>
                forecast.Rainfall < 0m ||
                forecast.Humidity < 0m ||
                forecast.Humidity > 100m ||
                !IsFinite(forecast.Rainfall) ||
                !IsFinite(forecast.Temperature) ||
                !IsFinite(forecast.Humidity)))
        {
            return "Forecasting service returned invalid weather values.";
        }

        return null;
    }

    private static string ToPreparationErrorCode(string? sourceErrorCode) =>
        sourceErrorCode switch
        {
            WeatherObservationSourceErrorCodes.RateLimited => ProviderForecastPreparationErrorCodes.RateLimited,
            WeatherObservationSourceErrorCodes.ProviderUnavailable => ProviderForecastPreparationErrorCodes.ProviderUnavailable,
            _ => ProviderForecastPreparationErrorCodes.InvalidProviderData
        };

    private static string SafeProviderMessage(string? sourceErrorCode) =>
        sourceErrorCode switch
        {
            WeatherObservationSourceErrorCodes.RateLimited => "Weather provider request was rate limited.",
            WeatherObservationSourceErrorCodes.ProviderUnavailable => "Weather provider observations are temporarily unavailable.",
            _ => "Weather provider observations were invalid."
        };

    private static string ToGenerationErrorCode(string? forecastErrorCode) =>
        forecastErrorCode switch
        {
            ForecastErrorCodes.ServiceUnavailable => ProviderForecastPreparationErrorCodes.ForecastingServiceUnavailable,
            _ => ProviderForecastPreparationErrorCodes.InvalidForecastOutput
        };

    private static string SafeForecastingMessage(string? forecastErrorCode) =>
        forecastErrorCode switch
        {
            ForecastErrorCodes.ServiceUnavailable => "Forecasting service is unavailable.",
            _ => "Forecasting service returned invalid forecast output."
        };

    private CurrentDateResult TryGetCurrentDate()
    {
        try
        {
            var timezone = TimeZoneInfo.FindSystemTimeZoneById(ApprovedTimezone);
            var localNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timezone);
            return CurrentDateResult.Success(DateOnly.FromDateTime(localNow.DateTime));
        }
        catch (TimeZoneNotFoundException)
        {
            return CurrentDateResult.Failure("Approved Asia/Colombo timezone was not available on this system.");
        }
        catch (InvalidTimeZoneException)
        {
            return CurrentDateResult.Failure("Approved Asia/Colombo timezone was invalid on this system.");
        }
    }

    private async Task<IDbContextTransaction?> TryBeginTransactionAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.Database.BeginTransactionAsync(cancellationToken);
        }
        catch (InvalidOperationException) when (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            return null;
        }
    }

    private sealed record CurrentDateResult(bool Succeeded, DateOnly CurrentDate, string Message)
    {
        public static CurrentDateResult Success(DateOnly currentDate) => new(true, currentDate, string.Empty);

        public static CurrentDateResult Failure(string message) => new(false, default, message);
    }
}
