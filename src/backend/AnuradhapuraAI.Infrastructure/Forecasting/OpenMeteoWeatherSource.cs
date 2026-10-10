using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AnuradhapuraAI.Application.Forecasting;

namespace AnuradhapuraAI.Infrastructure.Forecasting;

public sealed class OpenMeteoWeatherSource(HttpClient httpClient, TimeProvider timeProvider) : IWeatherObservationSource
{
    public const string ProviderName = "Open-Meteo";
    public const string ApprovedProduct = "era5";
    public const string ApprovedTimezone = "Asia/Colombo";
    public const decimal ApprovedLatitude = 8.312229m;
    public const decimal ApprovedLongitude = 80.413055m;

    private const int RequiredObservationDays = 30;
    private static readonly Uri ArchiveBaseUri = new("https://archive-api.open-meteo.com/v1/archive");
    public async Task<WeatherObservationSourceResult> GetDailyObservationsAsync(
        WeatherObservationSourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var requestError = ValidateRequest(request, currentDate);
        if (requestError is not null)
        {
            return WeatherObservationSourceResult.Failure(
                WeatherObservationSourceErrorCodes.InvalidRequest,
                requestError);
        }

        try
        {
            using var response = await httpClient.GetAsync(BuildRequestUri(request), cancellationToken);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return WeatherObservationSourceResult.Failure(
                    WeatherObservationSourceErrorCodes.RateLimited,
                    "Open-Meteo request was rate limited.");
            }

            if (response.StatusCode is HttpStatusCode.BadRequest)
            {
                return WeatherObservationSourceResult.Failure(
                    WeatherObservationSourceErrorCodes.InvalidRequest,
                    "Open-Meteo rejected the weather observation request.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return WeatherObservationSourceResult.Failure(
                    WeatherObservationSourceErrorCodes.ProviderUnavailable,
                    "Open-Meteo weather observations are temporarily unavailable.");
            }

            var payload = await response.Content.ReadFromJsonAsync<OpenMeteoArchiveResponse>(
                cancellationToken: cancellationToken);

            return payload is null
                ? InvalidResponse("Open-Meteo returned an empty response.")
                : ToValidatedResult(request, payload, currentDate);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            return WeatherObservationSourceResult.Failure(
                WeatherObservationSourceErrorCodes.ProviderUnavailable,
                "Open-Meteo weather observations timed out.");
        }
        catch (HttpRequestException)
        {
            return WeatherObservationSourceResult.Failure(
                WeatherObservationSourceErrorCodes.ProviderUnavailable,
                "Open-Meteo weather observations are temporarily unavailable.");
        }
        catch (System.Text.Json.JsonException)
        {
            return InvalidResponse("Open-Meteo returned malformed weather data.");
        }
    }

    public static Uri BuildRequestUri(WeatherObservationSourceRequest request)
    {
        var query = string.Join(
            "&",
            [
                $"latitude={FormatDecimal(request.Latitude)}",
                $"longitude={FormatDecimal(request.Longitude)}",
                $"start_date={request.StartDate:yyyy-MM-dd}",
                $"end_date={request.EndDate:yyyy-MM-dd}",
                "daily=precipitation_sum,temperature_2m_mean,relative_humidity_2m_mean",
                $"models={Uri.EscapeDataString(request.Product)}",
                $"timezone={Uri.EscapeDataString(request.Timezone)}"
            ]);

        return new Uri($"{ArchiveBaseUri}?{query}");
    }

    private static string? ValidateRequest(WeatherObservationSourceRequest request, DateOnly currentDate)
    {
        if (request.Product != ApprovedProduct)
        {
            return "Only the owner-approved Open-Meteo ERA5 product is supported for mocked validation.";
        }

        if (request.Timezone != ApprovedTimezone)
        {
            return "Only the owner-approved Asia/Colombo timezone is supported for mocked validation.";
        }

        if (request.Latitude != ApprovedLatitude || request.Longitude != ApprovedLongitude)
        {
            return "Only the owner-approved Anuradhapura representative coordinate is supported for mocked validation.";
        }

        if (request.EndDate < request.StartDate)
        {
            return "Observation end date must not be before the start date.";
        }

        var days = request.EndDate.DayNumber - request.StartDate.DayNumber + 1;
        if (days != RequiredObservationDays)
        {
            return "Exactly 30 daily observations are required.";
        }

        if (request.EndDate >= currentDate)
        {
            return "Observation range must not include the current or a future date.";
        }

        return null;
    }

    private WeatherObservationSourceResult ToValidatedResult(
        WeatherObservationSourceRequest request,
        OpenMeteoArchiveResponse payload,
        DateOnly currentDate)
    {
        if (payload.Daily is null ||
            payload.Daily.Time is null ||
            payload.Daily.PrecipitationSum is null ||
            payload.Daily.Temperature2mMean is null ||
            payload.Daily.RelativeHumidity2mMean is null)
        {
            return InvalidResponse("Open-Meteo response did not contain all required daily weather arrays.");
        }

        var count = payload.Daily.Time.Count;
        if (count != payload.Daily.PrecipitationSum.Count ||
            count != payload.Daily.Temperature2mMean.Count ||
            count != payload.Daily.RelativeHumidity2mMean.Count)
        {
            return InvalidResponse("Open-Meteo daily weather arrays had mismatched lengths.");
        }

        if (count != RequiredObservationDays)
        {
            return WeatherObservationSourceResult.Failure(
                WeatherObservationSourceErrorCodes.MissingWeatherDays,
                "Open-Meteo response did not contain exactly 30 daily observations.");
        }

        var observations = new List<WeatherObservation>(RequiredObservationDays);
        var seenDates = new HashSet<DateOnly>();
        for (var index = 0; index < count; index++)
        {
            if (!DateOnly.TryParseExact(
                    payload.Daily.Time[index],
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date))
            {
                return InvalidResponse("Open-Meteo response contained a malformed observation date.");
            }

            if (!seenDates.Add(date))
            {
                return InvalidResponse("Open-Meteo response contained duplicate observation dates.");
            }

            if (date != request.StartDate.AddDays(index))
            {
                return WeatherObservationSourceResult.Failure(
                    WeatherObservationSourceErrorCodes.MissingWeatherDays,
                    "Open-Meteo response dates were not consecutive chronological daily values.");
            }

            if (date >= currentDate)
            {
                return WeatherObservationSourceResult.Failure(
                    WeatherObservationSourceErrorCodes.InvalidResponse,
                    "Open-Meteo response included the current or a future date.");
            }

            var rainfall = payload.Daily.PrecipitationSum[index];
            var temperature = payload.Daily.Temperature2mMean[index];
            var humidity = payload.Daily.RelativeHumidity2mMean[index];
            var valueError = ValidateWeatherValues(rainfall, temperature, humidity);
            if (valueError is not null)
            {
                return InvalidResponse(valueError);
            }

            observations.Add(new WeatherObservation(
                date,
                rainfall.GetValueOrDefault(),
                temperature.GetValueOrDefault(),
                humidity.GetValueOrDefault()));
        }

        var metadata = new WeatherObservationSourceMetadata(
            ProviderName,
            ApprovedProduct,
            request.Latitude,
            request.Longitude,
            request.Timezone,
            request.StartDate,
            request.EndDate,
            timeProvider.GetUtcNow());

        return WeatherObservationSourceResult.Success(new WeatherObservationSourceResponse(metadata, observations));
    }

    private static string? ValidateWeatherValues(decimal? rainfall, decimal? temperature, decimal? humidity)
    {
        if (rainfall is null || temperature is null || humidity is null)
        {
            return "Open-Meteo response contained missing weather values.";
        }

        if (!IsFinite(rainfall.Value) || !IsFinite(temperature.Value) || !IsFinite(humidity.Value))
        {
            return "Open-Meteo response contained non-finite weather values.";
        }

        if (rainfall < 0m)
        {
            return "Open-Meteo response contained negative precipitation.";
        }

        if (temperature < -20m || temperature > 60m)
        {
            return "Open-Meteo response contained an implausible temperature value.";
        }

        if (humidity < 0m || humidity > 100m)
        {
            return "Open-Meteo response contained an invalid relative humidity value.";
        }

        return null;
    }

    private static bool IsFinite(decimal value) =>
        value != decimal.MinValue && value != decimal.MaxValue;

    private static WeatherObservationSourceResult InvalidResponse(string message) =>
        WeatherObservationSourceResult.Failure(WeatherObservationSourceErrorCodes.InvalidResponse, message);

    private static string FormatDecimal(decimal value) =>
        value.ToString("0.######", CultureInfo.InvariantCulture);

    private sealed record OpenMeteoArchiveResponse(
        [property: JsonPropertyName("daily")] OpenMeteoDailyResponse? Daily);

    private sealed record OpenMeteoDailyResponse(
        [property: JsonPropertyName("time")] IReadOnlyList<string>? Time,
        [property: JsonPropertyName("precipitation_sum")] IReadOnlyList<decimal?>? PrecipitationSum,
        [property: JsonPropertyName("temperature_2m_mean")] IReadOnlyList<decimal?>? Temperature2mMean,
        [property: JsonPropertyName("relative_humidity_2m_mean")] IReadOnlyList<decimal?>? RelativeHumidity2mMean);
}
