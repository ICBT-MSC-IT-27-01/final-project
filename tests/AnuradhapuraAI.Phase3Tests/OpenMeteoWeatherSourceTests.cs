using System.Globalization;
using System.Net;
using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Infrastructure.Forecasting;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class OpenMeteoWeatherSourceTests
{
    private static readonly DateTimeOffset FixedNow = DateTimeOffset.Parse("2026-10-10T00:00:00Z", CultureInfo.InvariantCulture);

    [Fact]
    public async Task GetDailyObservations_ConstructsApprovedOpenMeteoArchiveRequest()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(ValidProviderJson()));
        var source = CreateSource(handler);

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.True(result.Succeeded);
        Assert.NotNull(handler.LastRequestUri);
        Assert.Equal("https", handler.LastRequestUri!.Scheme);
        Assert.Equal("archive-api.open-meteo.com", handler.LastRequestUri.Host);
        Assert.Equal("/v1/archive", handler.LastRequestUri.AbsolutePath);
        Assert.Contains("latitude=8.312229", handler.LastRequestUri.Query);
        Assert.Contains("longitude=80.413055", handler.LastRequestUri.Query);
        Assert.Contains("start_date=2026-09-01", handler.LastRequestUri.Query);
        Assert.Contains("end_date=2026-09-30", handler.LastRequestUri.Query);
        Assert.Contains("models=era5", handler.LastRequestUri.Query);
        Assert.Contains("timezone=Asia%2FColombo", handler.LastRequestUri.Query);
        Assert.Contains("daily=precipitation_sum,temperature_2m_mean,relative_humidity_2m_mean", handler.LastRequestUri.Query);
    }

    [Fact]
    public async Task GetDailyObservations_ReturnsExactlyThirtyValidatedObservations()
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => JsonResponse(ValidProviderJson())));

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.True(result.Succeeded);
        Assert.Equal(OpenMeteoWeatherSource.ProviderName, result.Value!.Metadata.Provider);
        Assert.Equal(OpenMeteoWeatherSource.ApprovedProduct, result.Value.Metadata.Product);
        Assert.Equal(OpenMeteoWeatherSource.ApprovedTimezone, result.Value.Metadata.Timezone);
        Assert.Equal(OpenMeteoWeatherSource.ApprovedLatitude, result.Value.Metadata.Latitude);
        Assert.Equal(OpenMeteoWeatherSource.ApprovedLongitude, result.Value.Metadata.Longitude);
        Assert.Equal(FixedNow, result.Value.Metadata.RetrievedAt);
        Assert.Equal(30, result.Value.Observations.Count);
        Assert.Equal(new DateOnly(2026, 9, 1), result.Value.Observations[0].Date);
        Assert.Equal(new DateOnly(2026, 9, 30), result.Value.Observations[^1].Date);
        Assert.Equal(1.0m, result.Value.Observations[0].Rainfall);
        Assert.Equal(27.0m, result.Value.Observations[0].Temperature);
        Assert.Equal(78.0m, result.Value.Observations[0].Humidity);
    }

    [Theory]
    [InlineData("era5_land", "Only the owner-approved Open-Meteo ERA5 product")]
    [InlineData("best_match", "Only the owner-approved Open-Meteo ERA5 product")]
    public async Task GetDailyObservations_RejectsUnapprovedProduct(string product, string expectedMessage)
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(ValidProviderJson()));
        var source = CreateSource(handler);

        var result = await source.GetDailyObservationsAsync(ApprovedRequest(Product: product));

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.InvalidRequest, result.ErrorCode);
        Assert.Contains(expectedMessage, result.Message);
        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task GetDailyObservations_RejectsWrongTimezone()
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
    public async Task GetDailyObservations_RejectsWrongCoordinate()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(ValidProviderJson()));
        var source = CreateSource(handler);

        var result = await source.GetDailyObservationsAsync(ApprovedRequest(Latitude: 8.3m));

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.InvalidRequest, result.ErrorCode);
        Assert.Contains("representative coordinate", result.Message);
        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task GetDailyObservations_RejectsDateRangeThatIsNotThirtyDays()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(ValidProviderJson()));
        var source = CreateSource(handler);

        var result = await source.GetDailyObservationsAsync(ApprovedRequest(EndDate: new DateOnly(2026, 9, 29)));

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.InvalidRequest, result.ErrorCode);
        Assert.Contains("Exactly 30", result.Message);
        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task GetDailyObservations_RejectsCurrentOrFutureDateRanges()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(ValidProviderJson()));
        var source = CreateSource(handler);

        var result = await source.GetDailyObservationsAsync(ApprovedRequest(
            StartDate: new DateOnly(2026, 9, 11),
            EndDate: new DateOnly(2026, 10, 10)));

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.InvalidRequest, result.ErrorCode);
        Assert.Contains("current or a future date", result.Message);
        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task GetDailyObservations_RejectsMissingDays()
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => JsonResponse(ProviderJson(count: 29))));

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.MissingWeatherDays, result.ErrorCode);
    }

    [Fact]
    public async Task GetDailyObservations_RejectsExtraDays()
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => JsonResponse(ProviderJson(count: 31))));

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.MissingWeatherDays, result.ErrorCode);
    }

    [Fact]
    public async Task GetDailyObservations_RejectsDuplicateDates()
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => JsonResponse(ProviderJson(dateOverride: (10, "2026-09-10")))));

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.InvalidResponse, result.ErrorCode);
        Assert.Contains("duplicate", result.Message);
    }

    [Fact]
    public async Task GetDailyObservations_RejectsNonChronologicalDates()
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => JsonResponse(ProviderJson(dateOverride: (10, "2026-09-20")))));

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.MissingWeatherDays, result.ErrorCode);
        Assert.Contains("consecutive", result.Message);
    }

    [Fact]
    public async Task GetDailyObservations_RejectsMalformedDates()
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => JsonResponse(ProviderJson(dateOverride: (0, "not-a-date")))));

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.InvalidResponse, result.ErrorCode);
        Assert.Contains("malformed", result.Message);
    }

    [Fact]
    public async Task GetDailyObservations_RejectsMismatchedArrays()
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => JsonResponse(ProviderJson(dropLastTemperature: true))));

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.InvalidResponse, result.ErrorCode);
        Assert.Contains("mismatched", result.Message);
    }

    [Fact]
    public async Task GetDailyObservations_RejectsMissingDailyArrays()
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => JsonResponse("""{"daily":{"time":[]}}""")));

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.InvalidResponse, result.ErrorCode);
        Assert.Contains("required daily weather arrays", result.Message);
    }

    [Theory]
    [InlineData("precipitation_sum", "-1", "negative precipitation")]
    [InlineData("temperature_2m_mean", "61", "implausible temperature")]
    [InlineData("relative_humidity_2m_mean", "-1", "invalid relative humidity")]
    [InlineData("relative_humidity_2m_mean", "101", "invalid relative humidity")]
    public async Task GetDailyObservations_RejectsInvalidPhysicalValues(
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
    public async Task GetDailyObservations_RejectsNullValues()
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => JsonResponse(ProviderJson(valueOverride: ("temperature_2m_mean", 0, "null")))));

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.InvalidResponse, result.ErrorCode);
        Assert.Contains("missing weather values", result.Message);
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
    public async Task GetDailyObservations_MapsNetworkFailureSafely()
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => throw new HttpRequestException("network")));

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(WeatherObservationSourceErrorCodes.ProviderUnavailable, result.ErrorCode);
        Assert.Contains("temporarily unavailable", result.Message);
    }

    [Fact]
    public async Task GetDailyObservations_PropagatesCallerCancellation()
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => JsonResponse(ValidProviderJson())));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            source.GetDailyObservationsAsync(ApprovedRequest(), cts.Token));
    }

    [Fact]
    public async Task GetDailyObservations_DoesNotInvokeForecastInferenceOrPersistenceContracts()
    {
        var source = CreateSource(new StubHttpMessageHandler(_ => JsonResponse(ValidProviderJson())));

        var result = await source.GetDailyObservationsAsync(ApprovedRequest());

        Assert.True(result.Succeeded);
        Assert.IsAssignableFrom<IWeatherObservationSource>(source);
        Assert.DoesNotContain(
            typeof(IForecastService).Name,
            typeof(OpenMeteoWeatherSource).GetConstructors().Single().GetParameters().Select(parameter => parameter.ParameterType.Name));
    }

    private static OpenMeteoWeatherSource CreateSource(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler), new FixedTimeProvider(FixedNow));

    private static WeatherObservationSourceRequest ApprovedRequest(
        decimal Latitude = OpenMeteoWeatherSource.ApprovedLatitude,
        decimal Longitude = OpenMeteoWeatherSource.ApprovedLongitude,
        DateOnly? StartDate = null,
        DateOnly? EndDate = null,
        string Product = OpenMeteoWeatherSource.ApprovedProduct,
        string Timezone = OpenMeteoWeatherSource.ApprovedTimezone) =>
        new(
            Latitude,
            Longitude,
            StartDate ?? new DateOnly(2026, 9, 1),
            EndDate ?? new DateOnly(2026, 9, 30),
            Product,
            Timezone);

    private static string ValidProviderJson() => ProviderJson();

    private static string ProviderJson(
        int count = 30,
        (int Index, string Date)? dateOverride = null,
        (string Field, int Index, string Value)? valueOverride = null,
        bool dropLastTemperature = false)
    {
        var dates = Enumerable.Range(0, count)
            .Select(index => new DateOnly(2026, 9, 1).AddDays(index).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .ToList();
        if (dateOverride is { } dateChange)
        {
            dates[dateChange.Index] = dateChange.Date;
        }

        var rainfall = Enumerable.Range(0, count).Select(index => (1m + index / 10m).ToString(CultureInfo.InvariantCulture)).ToList();
        var temperature = Enumerable.Range(0, count).Select(index => (27m + index / 10m).ToString(CultureInfo.InvariantCulture)).ToList();
        var humidity = Enumerable.Range(0, count).Select(index => (78m + index % 5).ToString(CultureInfo.InvariantCulture)).ToList();
        if (dropLastTemperature)
        {
            temperature.RemoveAt(temperature.Count - 1);
        }

        if (valueOverride is { } valueChange)
        {
            var target = valueChange.Field switch
            {
                "precipitation_sum" => rainfall,
                "temperature_2m_mean" => temperature,
                "relative_humidity_2m_mean" => humidity,
                _ => throw new ArgumentOutOfRangeException(nameof(valueOverride), valueChange.Field, "Unknown field.")
            };
            target[valueChange.Index] = valueChange.Value;
        }

        return $$"""
            {
              "daily": {
                "time": [{{QuoteJoin(dates)}}],
                "precipitation_sum": [{{string.Join(",", rainfall)}}],
                "temperature_2m_mean": [{{string.Join(",", temperature)}}],
                "relative_humidity_2m_mean": [{{string.Join(",", humidity)}}]
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

        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WasCalled = true;
            LastRequestUri = request.RequestUri;
            return Task.FromResult(respond(request));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
