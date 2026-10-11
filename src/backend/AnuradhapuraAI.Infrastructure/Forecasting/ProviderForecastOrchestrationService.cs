using AnuradhapuraAI.Application.Forecasting;
using Microsoft.Extensions.Options;

namespace AnuradhapuraAI.Infrastructure.Forecasting;

public sealed class ProviderForecastOrchestrationService(
    IWeatherObservationSource weatherObservationSource,
    IOptions<WeatherModelFeatureOptions> featureOptions,
    TimeProvider timeProvider) : IProviderForecastOrchestrationService
{
    private const int RequiredObservationDays = 30;
    private const int ForecastHorizonDays = 7;
    private const decimal ApprovedLatitude = 8.312229m;
    private const decimal ApprovedLongitude = 80.413055m;
    private const string ProviderName = "Open-Meteo";
    private const string ApprovedProduct = "era5";
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

    private sealed record CurrentDateResult(bool Succeeded, DateOnly CurrentDate, string Message)
    {
        public static CurrentDateResult Success(DateOnly currentDate) => new(true, currentDate, string.Empty);

        public static CurrentDateResult Failure(string message) => new(false, default, message);
    }
}
