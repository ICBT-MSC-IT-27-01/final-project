using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AnuradhapuraAI.Application.Forecasting;

namespace AnuradhapuraAI.Infrastructure.Forecasting;

public sealed class OpenMeteoForecastPastDaysWeatherSource(HttpClient httpClient, TimeProvider timeProvider) : IFreshWeatherObservationSource
{
    public const string ProviderName = "Open-Meteo";
    public const string ApprovedProduct = "forecast_api_past_days_auto";
    public const string ApprovedTimezone = "Asia/Colombo";
    public const decimal ApprovedLatitude = 8.312229m;
    public const decimal ApprovedLongitude = 80.413055m;

    private const int RequiredObservationDays = 30;
    private const int RequiredHoursPerDay = 24;
    private const decimal MinimumPlausibleTemperature = -20m;
    private const decimal MaximumPlausibleTemperature = 60m;
    private static readonly Uri ForecastBaseUri = new("https://api.open-meteo.com/v1/forecast");

    public async Task<WeatherObservationSourceResult> GetDailyObservationsAsync(
        WeatherObservationSourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentDate = GetCurrentDate();
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
                    "Open-Meteo forecast request was rate limited.");
            }

            if (response.StatusCode is HttpStatusCode.BadRequest)
            {
                return WeatherObservationSourceResult.Failure(
                    WeatherObservationSourceErrorCodes.InvalidRequest,
                    "Open-Meteo rejected the forecast past-days request.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return WeatherObservationSourceResult.Failure(
                    WeatherObservationSourceErrorCodes.ProviderUnavailable,
                    "Open-Meteo forecast past-days observations are temporarily unavailable.");
            }

            var payload = await response.Content.ReadFromJsonAsync<OpenMeteoForecastResponse>(
                cancellationToken: cancellationToken);

            return payload is null
                ? InvalidResponse("Open-Meteo returned an empty forecast response.")
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
                "Open-Meteo forecast past-days observations timed out.");
        }
        catch (HttpRequestException)
        {
            return WeatherObservationSourceResult.Failure(
                WeatherObservationSourceErrorCodes.ProviderUnavailable,
                "Open-Meteo forecast past-days observations are temporarily unavailable.");
        }
        catch (System.Text.Json.JsonException)
        {
            return InvalidResponse("Open-Meteo returned malformed forecast past-days data.");
        }
    }

    public static Uri BuildRequestUri(WeatherObservationSourceRequest request)
    {
        var query = string.Join(
            "&",
            [
                $"latitude={FormatDecimal(request.Latitude)}",
                $"longitude={FormatDecimal(request.Longitude)}",
                "hourly=temperature_2m,relative_humidity_2m,precipitation",
                $"past_days={RequiredObservationDays.ToString(CultureInfo.InvariantCulture)}",
                "forecast_days=0",
                $"timezone={Uri.EscapeDataString(request.Timezone)}",
                "temperature_unit=celsius",
                "precipitation_unit=mm"
            ]);

        return new Uri($"{ForecastBaseUri}?{query}");
    }

    private static string? ValidateRequest(WeatherObservationSourceRequest request, DateOnly currentDate)
    {
        if (request.Product != ApprovedProduct)
        {
            return "Only the owner-approved Open-Meteo Forecast API past-days source is supported for operational mocked validation.";
        }

        if (request.Timezone != ApprovedTimezone)
        {
            return "Only the owner-approved Asia/Colombo timezone is supported for operational mocked validation.";
        }

        if (request.Latitude != ApprovedLatitude || request.Longitude != ApprovedLongitude)
        {
            return "Only the owner-approved Anuradhapura representative coordinate is supported for operational mocked validation.";
        }

        if (request.EndDate != currentDate.AddDays(-1))
        {
            return "Operational observation range must end on yesterday in the approved local timezone.";
        }

        if (request.StartDate != request.EndDate.AddDays(-(RequiredObservationDays - 1)))
        {
            return "Operational observation range must contain exactly 30 daily observations.";
        }

        return null;
    }

    private WeatherObservationSourceResult ToValidatedResult(
        WeatherObservationSourceRequest request,
        OpenMeteoForecastResponse payload,
        DateOnly currentDate)
    {
        if (!string.Equals(payload.Timezone, request.Timezone, StringComparison.Ordinal))
        {
            return InvalidResponse("Open-Meteo response timezone did not match the requested timezone.");
        }

        if (payload.Hourly is null ||
            payload.Hourly.Time is null ||
            payload.Hourly.Temperature2m is null ||
            payload.Hourly.RelativeHumidity2m is null ||
            payload.Hourly.Precipitation is null)
        {
            return InvalidResponse("Open-Meteo response did not contain all required hourly weather arrays.");
        }

        var count = payload.Hourly.Time.Count;
        if (count != payload.Hourly.Temperature2m.Count ||
            count != payload.Hourly.RelativeHumidity2m.Count ||
            count != payload.Hourly.Precipitation.Count)
        {
            return InvalidResponse("Open-Meteo hourly weather arrays had mismatched lengths.");
        }

        var unitError = ValidateUnits(payload.HourlyUnits);
        if (unitError is not null)
        {
            return InvalidResponse(unitError);
        }

        var hourlyValues = new List<HourlyWeatherValue>(count);
        var seenTimes = new HashSet<DateTime>();
        for (var index = 0; index < count; index++)
        {
            if (!DateTime.TryParseExact(
                    payload.Hourly.Time[index],
                    "yyyy-MM-ddTHH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var localTimestamp))
            {
                return InvalidResponse("Open-Meteo response contained a malformed hourly timestamp.");
            }

            if (!seenTimes.Add(localTimestamp))
            {
                return InvalidResponse("Open-Meteo response contained duplicate hourly timestamps.");
            }

            var date = DateOnly.FromDateTime(localTimestamp);
            if (date < request.StartDate || date > request.EndDate || date >= currentDate)
            {
                return InvalidResponse("Open-Meteo response contained an hourly value outside the requested operational window.");
            }

            var temperature = payload.Hourly.Temperature2m[index];
            var humidity = payload.Hourly.RelativeHumidity2m[index];
            var precipitation = payload.Hourly.Precipitation[index];
            var valueError = ValidateWeatherValues(temperature, humidity, precipitation);
            if (valueError is not null)
            {
                return InvalidResponse(valueError);
            }

            hourlyValues.Add(new HourlyWeatherValue(
                localTimestamp,
                date,
                localTimestamp.Hour,
                temperature.GetValueOrDefault(),
                humidity.GetValueOrDefault(),
                precipitation.GetValueOrDefault()));
        }

        var observations = new List<WeatherObservation>(RequiredObservationDays);
        for (var dayIndex = 0; dayIndex < RequiredObservationDays; dayIndex++)
        {
            var expectedDate = request.StartDate.AddDays(dayIndex);
            var values = hourlyValues
                .Where(value => value.Date == expectedDate)
                .OrderBy(value => value.Hour)
                .ToList();

            if (values.Count != RequiredHoursPerDay ||
                values.Select(value => value.Hour).Distinct().Count() != RequiredHoursPerDay ||
                !values.Select(value => value.Hour).SequenceEqual(Enumerable.Range(0, RequiredHoursPerDay)))
            {
                return WeatherObservationSourceResult.Failure(
                    WeatherObservationSourceErrorCodes.MissingWeatherDays,
                    "Open-Meteo response did not contain complete hourly coverage for every operational day.");
            }

            observations.Add(new WeatherObservation(
                expectedDate,
                Rainfall: values.Sum(value => value.Precipitation),
                Temperature: values.Average(value => value.Temperature),
                Humidity: values.Average(value => value.Humidity)));
        }

        var metadata = new WeatherObservationSourceMetadata(
            ProviderName,
            ApprovedProduct,
            request.Latitude,
            request.Longitude,
            payload.Latitude,
            payload.Longitude,
            request.Timezone,
            request.StartDate,
            request.EndDate,
            timeProvider.GetUtcNow());

        return WeatherObservationSourceResult.Success(new WeatherObservationSourceResponse(metadata, observations));
    }

    private static string? ValidateUnits(OpenMeteoHourlyUnits? units)
    {
        if (units is null)
        {
            return "Open-Meteo response did not contain hourly units.";
        }

        if (!string.Equals(units.Temperature2m, "°C", StringComparison.Ordinal) &&
            !string.Equals(units.Temperature2m, "°C (°F)", StringComparison.Ordinal))
        {
            return "Open-Meteo temperature units were not Celsius.";
        }

        if (!string.Equals(units.RelativeHumidity2m, "%", StringComparison.Ordinal))
        {
            return "Open-Meteo relative humidity units were not percent.";
        }

        if (!string.Equals(units.Precipitation, "mm", StringComparison.Ordinal))
        {
            return "Open-Meteo precipitation units were not millimeters.";
        }

        return null;
    }

    private static string? ValidateWeatherValues(decimal? temperature, decimal? humidity, decimal? precipitation)
    {
        if (temperature is null || humidity is null || precipitation is null)
        {
            return "Open-Meteo response contained missing hourly weather values.";
        }

        if (!IsFinite(temperature.Value) || !IsFinite(humidity.Value) || !IsFinite(precipitation.Value))
        {
            return "Open-Meteo response contained non-finite hourly weather values.";
        }

        if (temperature < MinimumPlausibleTemperature || temperature > MaximumPlausibleTemperature)
        {
            return "Open-Meteo response contained an implausible hourly temperature value.";
        }

        if (humidity < 0m || humidity > 100m)
        {
            return "Open-Meteo response contained an invalid hourly relative humidity value.";
        }

        if (precipitation < 0m)
        {
            return "Open-Meteo response contained negative hourly precipitation.";
        }

        return null;
    }

    private DateOnly GetCurrentDate()
    {
        var timezone = TimeZoneInfo.FindSystemTimeZoneById(ApprovedTimezone);
        var localNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timezone);
        return DateOnly.FromDateTime(localNow.DateTime);
    }

    private static bool IsFinite(decimal value) =>
        value != decimal.MinValue && value != decimal.MaxValue;

    private static WeatherObservationSourceResult InvalidResponse(string message) =>
        WeatherObservationSourceResult.Failure(WeatherObservationSourceErrorCodes.InvalidResponse, message);

    private static string FormatDecimal(decimal value) =>
        value.ToString("0.######", CultureInfo.InvariantCulture);

    private sealed record HourlyWeatherValue(
        DateTime Timestamp,
        DateOnly Date,
        int Hour,
        decimal Temperature,
        decimal Humidity,
        decimal Precipitation);

    private sealed record OpenMeteoForecastResponse(
        [property: JsonPropertyName("latitude")] decimal? Latitude,
        [property: JsonPropertyName("longitude")] decimal? Longitude,
        [property: JsonPropertyName("timezone")] string? Timezone,
        [property: JsonPropertyName("hourly_units")] OpenMeteoHourlyUnits? HourlyUnits,
        [property: JsonPropertyName("hourly")] OpenMeteoHourlyResponse? Hourly);

    private sealed record OpenMeteoHourlyUnits(
        [property: JsonPropertyName("temperature_2m")] string? Temperature2m,
        [property: JsonPropertyName("relative_humidity_2m")] string? RelativeHumidity2m,
        [property: JsonPropertyName("precipitation")] string? Precipitation);

    private sealed record OpenMeteoHourlyResponse(
        [property: JsonPropertyName("time")] IReadOnlyList<string>? Time,
        [property: JsonPropertyName("temperature_2m")] IReadOnlyList<decimal?>? Temperature2m,
        [property: JsonPropertyName("relative_humidity_2m")] IReadOnlyList<decimal?>? RelativeHumidity2m,
        [property: JsonPropertyName("precipitation")] IReadOnlyList<decimal?>? Precipitation);
}
