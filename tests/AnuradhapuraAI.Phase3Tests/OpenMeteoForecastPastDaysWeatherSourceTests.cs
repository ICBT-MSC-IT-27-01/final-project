using System.Globalization;
using System.Net;
using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Infrastructure.Forecasting;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class OpenMeteoForecastPastDaysWeatherSourceTests
{
    private static readonly DateTimeOffset FixedNow = DateTimeOffset.Parse("2026-10-10T00:00:00Z", CultureInfo.InvariantCulture);

    [Fact]
    public async Task GetDailyObservations_ConstructsApprovedForecastPastDaysRequest()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(ValidProviderJson()));
        var source = CreateSource(handler);

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.True(result.Succeeded);
        Assert.NotNull(handler.LastRequestUri);
        Assert.Equal("https", handler.LastRequestUri!.Scheme);
        Assert.Equal("api.open-meteo.com", handler.LastRequestUri.Host);
        Assert.Equal("/v1/forecast", handler.LastRequestUri.AbsolutePath);
        Assert.Contains("latitude=8.312229", handler.LastRequestUri.Query);
        Assert.Contains("longitude=80.413055", handler.LastRequestUri.Query);
        Assert.Contains("past_days=30", handler.LastRequestUri.Query);
        Assert.Contains("forecast_days=0", handler.LastRequestUri.Query);
        Assert.Contains("timezone=Asia%2FColombo", handler.LastRequestUri.Query);
        Assert.Contains("hourly=temperature_2m,relative_humidity_2m,precipitation", handler.LastRequestUri.Query);
    }

    [Fact]
    public async Task GetDailyObservations_AggregatesCompleteHourlyCoverageIntoThirtyDailyRows()
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => JsonResponse(ValidProviderJson())));

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(OpenMeteoForecastPastDaysWeatherSource.ProviderName, result.Value!.Metadata.Provider);
        Assert.Equal(OpenMeteoForecastPastDaysWeatherSource.ApprovedProduct, result.Value.Metadata.Product);
        Assert.Equal(OpenMeteoForecastPastDaysWeatherSource.ApprovedTimezone, result.Value.Metadata.Timezone);
        Assert.Equal(OpenMeteoForecastPastDaysWeatherSource.ApprovedLatitude, result.Value.Metadata.Latitude);
        Assert.Equal(OpenMeteoForecastPastDaysWeatherSource.ApprovedLongitude, result.Value.Metadata.Longitude);
        Assert.Equal(8.25m, result.Value.Metadata.ReturnedLatitude);
        Assert.Equal(80.5m, result.Value.Metadata.ReturnedLongitude);
        Assert.Equal(FixedNow, result.Value.Metadata.RetrievedAt);
        Assert.Equal(30, result.Value.Observations.Count);
        Assert.Equal(new DateOnly(2026, 9, 10), result.Value.Observations[0].Date);
        Assert.Equal(new DateOnly(2026, 10, 9), result.Value.Observations[^1].Date);
        Assert.Equal(24m, result.Value.Observations[0].Rainfall);
        Assert.Equal(27m, result.Value.Observations[0].Temperature);
        Assert.Equal(78m, result.Value.Observations[0].Humidity);
    }

    [Fact]
    public void OpenMeteoNoRedirectHttpClientHandler_DisablesAutomaticRedirects()
    {
        using var handler = new OpenMeteoNoRedirectHttpClientHandler();

        Assert.False(handler.AllowAutoRedirect);
    }

    [Theory]
    [InlineData("era5")]
    [InlineData("best_match")]
    public async Task GetDailyObservations_RejectsUnsupportedProductWithoutProviderCall(string product)
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(ValidProviderJson()));
        var source = CreateSource(handler);

        var result = await source.GetDailyObservationsAsync(ApprovedRequest(Product: product));

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.InvalidRequest, result.ErrorCode);
        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task GetDailyObservations_RejectsWrongTimezoneWithoutProviderCall()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(ValidProviderJson()));
        var source = CreateSource(handler);

        var result = await source.GetDailyObservationsAsync(ApprovedRequest(Timezone: "UTC"));

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.InvalidRequest, result.ErrorCode);
        Assert.Contains("Asia/Colombo", result.Message);
        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task GetDailyObservations_RejectsWindowThatDoesNotEndYesterdayWithoutProviderCall()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(ValidProviderJson()));
        var source = CreateSource(handler);

        var result = await source.GetDailyObservationsAsync(ApprovedRequest(
            StartDate: new DateOnly(2026, 9, 9),
            EndDate: new DateOnly(2026, 10, 8)));

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.InvalidRequest, result.ErrorCode);
        Assert.Contains("yesterday", result.Message);
        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task GetDailyObservations_RejectsMissingHourlyCoverage()
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => JsonResponse(ProviderJson(dropHour: new DateTime(2026, 9, 10, 23, 0, 0)))));

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.MissingWeatherDays, result.ErrorCode);
        Assert.Contains("complete hourly coverage", result.Message);
    }

    [Fact]
    public async Task GetDailyObservations_RejectsDuplicateHourlyTimestamp()
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => JsonResponse(ProviderJson(duplicateFirstTimestamp: true))));

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.InvalidResponse, result.ErrorCode);
        Assert.Contains("duplicate", result.Message);
    }

    [Fact]
    public async Task GetDailyObservations_RejectsResponseTimezoneMismatch()
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => JsonResponse(ProviderJson(timezone: "UTC"))));

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.InvalidResponse, result.ErrorCode);
        Assert.Contains("timezone", result.Message);
    }

    [Theory]
    [InlineData("precipitation", "-1", "negative hourly precipitation")]
    [InlineData("temperature_2m", "61", "implausible hourly temperature")]
    [InlineData("relative_humidity_2m", "-1", "invalid hourly relative humidity")]
    [InlineData("relative_humidity_2m", "101", "invalid hourly relative humidity")]
    public async Task GetDailyObservations_RejectsInvalidHourlyValues(
        string field,
        string replacement,
        string expectedMessage)
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => JsonResponse(ProviderJson(valueOverride: (field, 0, replacement)))));

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.InvalidResponse, result.ErrorCode);
        Assert.Contains(expectedMessage, result.Message);
    }

    [Fact]
    public async Task GetDailyObservations_RejectsMissingHourlyValue()
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => JsonResponse(ProviderJson(valueOverride: ("temperature_2m", 0, "null")))));

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.InvalidResponse, result.ErrorCode);
        Assert.Contains("missing hourly weather values", result.Message);
    }

    [Fact]
    public async Task GetDailyObservations_RejectsInvalidJson()
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => JsonResponse("{ invalid-json")));

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.InvalidResponse, result.ErrorCode);
        Assert.Contains("malformed", result.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, WeatherObservationSourceErrorCodes.InvalidRequest)]
    [InlineData((HttpStatusCode)429, WeatherObservationSourceErrorCodes.RateLimited)]
    [InlineData(HttpStatusCode.InternalServerError, WeatherObservationSourceErrorCodes.ProviderUnavailable)]
    public async Task GetDailyObservations_MapsProviderHttpErrorsSafely(
        HttpStatusCode statusCode,
        string expectedErrorCode)
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => new HttpResponseMessage(statusCode)));

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(expectedErrorCode, result.ErrorCode);
        Assert.DoesNotContain("http", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(HttpStatusCode.MovedPermanently)]
    [InlineData(HttpStatusCode.Found)]
    [InlineData(HttpStatusCode.TemporaryRedirect)]
    [InlineData(HttpStatusCode.PermanentRedirect)]
    public async Task GetDailyObservations_TreatsRedirectResponsesAsProviderFailureWithoutSecondRequest(
        HttpStatusCode statusCode)
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
        {
            Headers = { Location = new Uri("https://example.test/redirected") }
        });
        var source = CreateSource(handler);

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.ProviderUnavailable, result.ErrorCode);
        Assert.Equal(1, handler.CallCount);
        Assert.Equal("api.open-meteo.com", handler.LastRequestUri!.Host);
        Assert.DoesNotContain("redirect", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetDailyObservations_MapsTimeoutSafely()
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => throw new TaskCanceledException("timeout")));

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.ProviderUnavailable, result.ErrorCode);
        Assert.Contains("timed out", result.Message);
    }

    [Fact]
    public async Task GetDailyObservations_DoesNotInvokeForecastInferenceOrPersistenceContracts()
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => JsonResponse(ValidProviderJson())));

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.True(result.Succeeded);
        Assert.IsAssignableFrom<IFreshWeatherObservationSource>(source);
        Assert.DoesNotContain(
            typeof(IForecastService).Name,
            typeof(OpenMeteoForecastPastDaysWeatherSource).GetConstructors().Single().GetParameters().Select(parameter => parameter.ParameterType.Name));
    }

    private static OpenMeteoForecastPastDaysWeatherSource CreateSource(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler), new FixedTimeProvider(FixedNow));

    private static WeatherObservationSourceRequest ApprovedRequest(
        decimal Latitude = OpenMeteoForecastPastDaysWeatherSource.ApprovedLatitude,
        decimal Longitude = OpenMeteoForecastPastDaysWeatherSource.ApprovedLongitude,
        DateOnly? StartDate = null,
        DateOnly? EndDate = null,
        string Product = OpenMeteoForecastPastDaysWeatherSource.ApprovedProduct,
        string Timezone = OpenMeteoForecastPastDaysWeatherSource.ApprovedTimezone) =>
        new(
            Latitude,
            Longitude,
            StartDate ?? new DateOnly(2026, 9, 10),
            EndDate ?? new DateOnly(2026, 10, 9),
            Product,
            Timezone);

    private static string ValidProviderJson() => ProviderJson();

    private static string ProviderJson(
        string timezone = OpenMeteoForecastPastDaysWeatherSource.ApprovedTimezone,
        DateTime? dropHour = null,
        bool duplicateFirstTimestamp = false,
        (string Field, int Index, string Value)? valueOverride = null)
    {
        var timestamps = new List<string>();
        var temperature = new List<string>();
        var humidity = new List<string>();
        var precipitation = new List<string>();

        for (var dayIndex = 0; dayIndex < 30; dayIndex++)
        {
            var date = new DateOnly(2026, 9, 10).AddDays(dayIndex);
            for (var hour = 0; hour < 24; hour++)
            {
                var timestamp = date.ToDateTime(new TimeOnly(hour, 0));
                if (dropHour == timestamp)
                {
                    continue;
                }

                timestamps.Add(timestamp.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture));
                temperature.Add("27");
                humidity.Add("78");
                precipitation.Add("1");
            }
        }

        if (duplicateFirstTimestamp)
        {
            timestamps[1] = timestamps[0];
        }

        if (valueOverride is { } valueChange)
        {
            var target = valueChange.Field switch
            {
                "precipitation" => precipitation,
                "temperature_2m" => temperature,
                "relative_humidity_2m" => humidity,
                _ => throw new ArgumentOutOfRangeException(nameof(valueOverride), valueChange.Field, "Unknown field.")
            };
            target[valueChange.Index] = valueChange.Value;
        }

        return $$"""
            {
              "latitude": 8.25,
              "longitude": 80.5,
              "timezone": "{{timezone}}",
              "hourly_units": {
                "temperature_2m": "°C",
                "relative_humidity_2m": "%",
                "precipitation": "mm"
              },
              "hourly": {
                "time": [{{QuoteJoin(timestamps)}}],
                "temperature_2m": [{{string.Join(",", temperature)}}],
                "relative_humidity_2m": [{{string.Join(",", humidity)}}],
                "precipitation": [{{string.Join(",", precipitation)}}]
              }
            }
            """;
    }

    private static string QuoteJoin(IEnumerable<string> values) =>
        string.Join(",", values.Select(value => $"\"{value}\""));

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public bool WasCalled { get; private set; }

        public int CallCount { get; private set; }

        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WasCalled = true;
            CallCount++;
            LastRequestUri = request.RequestUri;
            return Task.FromResult(respond(request));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
