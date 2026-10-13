using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Infrastructure.Persistence;
using AnuradhapuraAI.Infrastructure.Forecasting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class ProviderForecastOrchestrationServiceTests
{
    private static readonly DateTimeOffset FixedNow = DateTimeOffset.Parse("2026-10-10T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
    private static readonly DateOnly StartDate = new(2026, 9, 1);
    private static readonly DateOnly EndDate = new(2026, 9, 30);

    [Fact]
    public async Task PrepareHistoricalForecast_ReturnsDisabledAndDoesNotCallSource_WhenFeatureFlagOff()
    {
        var source = new FakeWeatherObservationSource();
        var service = CreateService(source, enabled: false);

        var result = await service.PrepareHistoricalForecastAsync(ApprovedRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(ProviderForecastPreparationErrorCodes.FeatureDisabled, result.ErrorCode);
        Assert.Equal(0, source.CallCount);
    }

    [Fact]
    public async Task PrepareHistoricalForecast_PreparesThirtyRowsAndHistoricalEvaluation_WhenFeatureFlagOn()
    {
        var source = new FakeWeatherObservationSource();
        var service = CreateService(source);

        var result = await service.PrepareHistoricalForecastAsync(ApprovedRequest());

        Assert.True(result.Succeeded);
        Assert.Equal(1, source.CallCount);
        Assert.Equal(30, result.Value!.ForecastRequest.Observations.Count);
        Assert.Equal(StartDate, result.Value.ObservationWindowStart);
        Assert.Equal(EndDate, result.Value.ObservationWindowEnd);
        Assert.Equal(EndDate, result.Value.ForecastOriginDate);
        Assert.Equal(ProviderForecastRunPurpose.HistoricalEvaluation, result.Value.RunPurpose);
        Assert.Equal(ProviderForecastRunPurpose.HistoricalEvaluation, result.Value.Provenance.RunPurpose);
        Assert.Equal(StartDate, result.Value.ForecastRequest.Observations[0].Date);
        Assert.Equal(EndDate, result.Value.ForecastRequest.Observations[^1].Date);
    }

    [Theory]
    [InlineData("2026-09-01", "2026-09-29")]
    [InlineData("2026-09-01", "2026-10-01")]
    [InlineData("2026-09-30", "2026-09-01")]
    [InlineData("2026-09-11", "2026-10-10")]
    [InlineData("2026-10-10", "2026-11-08")]
    public async Task PrepareHistoricalForecast_RejectsInvalidObservationWindows(
        string start,
        string end)
    {
        var source = new FakeWeatherObservationSource();
        var service = CreateService(source);

        var result = await service.PrepareHistoricalForecastAsync(new ProviderForecastPreparationRequest(
            DateOnly.Parse(start, System.Globalization.CultureInfo.InvariantCulture),
            DateOnly.Parse(end, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.False(result.Succeeded);
        Assert.Equal(ProviderForecastPreparationErrorCodes.InvalidObservationWindow, result.ErrorCode);
        Assert.Equal(0, source.CallCount);
    }

    [Fact]
    public async Task PrepareHistoricalForecast_UsesFixedServerControlledProviderSettings()
    {
        var source = new FakeWeatherObservationSource();
        var service = CreateService(source);

        var result = await service.PrepareHistoricalForecastAsync(ApprovedRequest());

        Assert.True(result.Succeeded);
        Assert.NotNull(source.LastRequest);
        Assert.Equal(8.312229m, source.LastRequest!.Latitude);
        Assert.Equal(80.413055m, source.LastRequest.Longitude);
        Assert.Equal("era5", source.LastRequest.Product);
        Assert.Equal("Asia/Colombo", source.LastRequest.Timezone);
        Assert.Equal("Open-Meteo", result.Value!.Provenance.ProviderName);
        Assert.Equal("era5", result.Value.Provenance.ProviderProduct);
        Assert.Equal(8.312229m, result.Value.Provenance.RequestedLatitude);
        Assert.Equal(80.413055m, result.Value.Provenance.RequestedLongitude);
        Assert.Equal("Asia/Colombo", result.Value.Provenance.Timezone);
    }

    [Fact]
    public async Task PrepareHistoricalForecast_PreservesWeatherValueMapping()
    {
        var observations = ValidObservations()
            .Select((observation, index) => observation with
            {
                Rainfall = 100m + index,
                Temperature = 20m + index,
                Humidity = 50m + index
            })
            .ToList();
        var service = CreateService(new FakeWeatherObservationSource(observations));

        var result = await service.PrepareHistoricalForecastAsync(ApprovedRequest());

        Assert.True(result.Succeeded);
        var first = result.Value!.ForecastRequest.Observations[0];
        Assert.Equal(100m, first.Rainfall);
        Assert.Equal(20m, first.Temperature);
        Assert.Equal(50m, first.Humidity);
    }

    [Fact]
    public async Task PrepareHistoricalForecast_DerivesSevenConsecutiveTargetDates()
    {
        var service = CreateService(new FakeWeatherObservationSource());

        var result = await service.PrepareHistoricalForecastAsync(ApprovedRequest());

        Assert.True(result.Succeeded);
        Assert.Equal(
            Enumerable.Range(1, 7).Select(EndDate.AddDays).ToList(),
            result.Value!.ForecastTargetDates);
        Assert.Equal(new DateOnly(2026, 10, 1), result.Value.Provenance.ForecastTargetStart);
        Assert.Equal(new DateOnly(2026, 10, 7), result.Value.Provenance.ForecastTargetEnd);
    }

    [Fact]
    public async Task PrepareHistoricalForecast_DoesNotInventUnavailableProvenance()
    {
        var service = CreateService(new FakeWeatherObservationSource());

        var result = await service.PrepareHistoricalForecastAsync(ApprovedRequest());

        Assert.True(result.Succeeded);
        Assert.Null(result.Value!.Provenance.GetType().GetProperty("ReturnedGridLatitude"));
        Assert.Null(result.Value.Provenance.GetType().GetProperty("ModelVersion"));
        Assert.Null(result.Value.Provenance.GetType().GetProperty("ForecastRunId"));
        Assert.Equal(FixedNow, result.Value.Provenance.LocallyRecordedRetrievedAt);
    }

    [Theory]
    [MemberData(nameof(InvalidObservationCases))]
    public async Task PrepareHistoricalForecast_RejectsInvalidSourceObservations(
        IReadOnlyList<WeatherObservation> observations,
        string expectedMessage)
    {
        var service = CreateService(new FakeWeatherObservationSource(observations));

        var result = await service.PrepareHistoricalForecastAsync(ApprovedRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(ProviderForecastPreparationErrorCodes.InvalidProviderData, result.ErrorCode);
        Assert.Contains(expectedMessage, result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(WeatherObservationSourceErrorCodes.ProviderUnavailable, ProviderForecastPreparationErrorCodes.ProviderUnavailable)]
    [InlineData(WeatherObservationSourceErrorCodes.RateLimited, ProviderForecastPreparationErrorCodes.RateLimited)]
    [InlineData(WeatherObservationSourceErrorCodes.InvalidResponse, ProviderForecastPreparationErrorCodes.InvalidProviderData)]
    public async Task PrepareHistoricalForecast_MapsProviderFailuresSafely(
        string sourceErrorCode,
        string expectedErrorCode)
    {
        var source = FakeWeatherObservationSource.Failure(sourceErrorCode);
        var service = CreateService(source);

        var result = await service.PrepareHistoricalForecastAsync(ApprovedRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(expectedErrorCode, result.ErrorCode);
        Assert.DoesNotContain("http", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PrepareHistoricalForecast_PropagatesCancellation()
    {
        var service = CreateService(new FakeWeatherObservationSource());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.PrepareHistoricalForecastAsync(ApprovedRequest(), cts.Token));
    }

    [Fact]
    public async Task GenerateOperationalForecast_PersistsOperationalRunAndSevenChildren_WhenPipelineIsValid()
    {
        await using var dbContext = CreateDbContext();
        var observationEnd = new DateOnly(2026, 10, 9);
        var source = new FakeWeatherObservationSource(ValidObservations(new DateOnly(2026, 9, 10)));
        var forecastingClient = new FakeForecastingClient(SampleForecastResponse(observationEnd));
        var service = CreateService(source, forecastingClient: forecastingClient, dbContext: dbContext);

        var result = await service.GenerateOperationalForecastAsync();

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(1, source.CallCount);
        Assert.Equal(1, forecastingClient.CallCount);
        Assert.NotNull(source.LastRequest);
        Assert.Equal(OpenMeteoForecastPastDaysWeatherSource.ApprovedProduct, source.LastRequest!.Product);
        Assert.Equal("Asia/Colombo", source.LastRequest.Timezone);
        Assert.Equal(ApprovedForecastRunPurposes.Operational, result.Value!.RunPurpose);
        Assert.Equal(new DateOnly(2026, 9, 10), result.Value.ObservationWindowStart);
        Assert.Equal(observationEnd, result.Value.ObservationWindowEnd);
        Assert.Equal(new DateOnly(2026, 10, 10), result.Value.TargetStartDate);
        Assert.Equal(new DateOnly(2026, 10, 16), result.Value.TargetEndDate);

        var run = Assert.Single(dbContext.ForecastRuns.Include(item => item.ForecastRecords));
        Assert.Equal(ApprovedForecastRunPurposes.Operational, run.RunPurpose);
        Assert.Equal("Open-Meteo", run.SourceProvider);
        Assert.Equal(OpenMeteoForecastPastDaysWeatherSource.ApprovedProduct, run.SourceProduct);
        Assert.Equal("Asia/Colombo", run.Timezone);
        Assert.Equal("Accepted", run.DataQualityStatus);
        Assert.Equal(7, run.ForecastRecords.Count);
        Assert.All(run.ForecastRecords, record => Assert.Equal(run.ForecastRunId, record.ForecastRunId));
    }

    [Fact]
    public async Task GenerateOperationalForecast_DoesNotCallProviderOrPython_WhenFeatureFlagOff()
    {
        await using var dbContext = CreateDbContext();
        var source = new FakeWeatherObservationSource(ValidObservations(new DateOnly(2026, 9, 10)));
        var forecastingClient = new FakeForecastingClient(SampleForecastResponse(new DateOnly(2026, 10, 9)));
        var service = CreateService(source, enabled: false, forecastingClient: forecastingClient, dbContext: dbContext);

        var result = await service.GenerateOperationalForecastAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(ProviderForecastPreparationErrorCodes.FeatureDisabled, result.ErrorCode);
        Assert.Equal(0, source.CallCount);
        Assert.Equal(0, forecastingClient.CallCount);
        Assert.Empty(dbContext.ForecastRuns);
        Assert.Empty(dbContext.ForecastRecords);
    }

    [Fact]
    public async Task GenerateOperationalForecast_DoesNotCallProviderOrPython_WhenSourceNotAccepted()
    {
        await using var dbContext = CreateDbContext();
        var source = new FakeWeatherObservationSource(ValidObservations(new DateOnly(2026, 9, 10)));
        var forecastingClient = new FakeForecastingClient(SampleForecastResponse(new DateOnly(2026, 10, 9)));
        var service = CreateService(
            source,
            forecastingClient: forecastingClient,
            dbContext: dbContext,
            acceptOperationalSource: false);

        var result = await service.GenerateOperationalForecastAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(ProviderForecastPreparationErrorCodes.OperationalSourceNotAccepted, result.ErrorCode);
        Assert.Equal(0, source.CallCount);
        Assert.Equal(0, forecastingClient.CallCount);
        Assert.Empty(dbContext.ForecastRuns);
        Assert.Empty(dbContext.ForecastRecords);
    }

    [Fact]
    public async Task GenerateOperationalForecast_DoesNotFallbackToHistoricalSource_WhenOperationalSourceIsUnknown()
    {
        await using var dbContext = CreateDbContext();
        var source = new FakeWeatherObservationSource(ValidObservations(new DateOnly(2026, 9, 10)));
        var forecastingClient = new FakeForecastingClient(SampleForecastResponse(new DateOnly(2026, 10, 9)));
        var service = CreateService(
            source,
            forecastingClient: forecastingClient,
            dbContext: dbContext,
            operationalSource: "era5");

        var result = await service.GenerateOperationalForecastAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(ProviderForecastPreparationErrorCodes.UnsupportedOperationalSource, result.ErrorCode);
        Assert.Equal(0, source.CallCount);
        Assert.Equal(0, forecastingClient.CallCount);
        Assert.Empty(dbContext.ForecastRuns);
        Assert.Empty(dbContext.ForecastRecords);
    }

    [Fact]
    public async Task GenerateOperationalForecast_DoesNotPersistPartialRun_WhenForecastingServiceFails()
    {
        await using var dbContext = CreateDbContext();
        var source = new FakeWeatherObservationSource(ValidObservations(new DateOnly(2026, 9, 10)));
        var forecastingClient = FakeForecastingClient.Failure(ForecastErrorCodes.ServiceUnavailable);
        var service = CreateService(source, forecastingClient: forecastingClient, dbContext: dbContext);

        var result = await service.GenerateOperationalForecastAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(ProviderForecastPreparationErrorCodes.ForecastingServiceUnavailable, result.ErrorCode);
        Assert.Equal(1, source.CallCount);
        Assert.Equal(1, forecastingClient.CallCount);
        Assert.Empty(dbContext.ForecastRuns);
        Assert.Empty(dbContext.ForecastRecords);
    }

    [Fact]
    public async Task GenerateOperationalForecast_DoesNotCallPythonOrPersist_WhenOperationalSourceReturnsRedirectFailure()
    {
        await using var dbContext = CreateDbContext();
        var source = FakeWeatherObservationSource.Failure(WeatherObservationSourceErrorCodes.ProviderUnavailable);
        var forecastingClient = new FakeForecastingClient(SampleForecastResponse(new DateOnly(2026, 10, 9)));
        var service = CreateService(source, forecastingClient: forecastingClient, dbContext: dbContext);

        var result = await service.GenerateOperationalForecastAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(ProviderForecastPreparationErrorCodes.ProviderUnavailable, result.ErrorCode);
        Assert.Equal(1, source.CallCount);
        Assert.Equal(0, forecastingClient.CallCount);
        Assert.Empty(dbContext.ForecastRuns);
        Assert.Empty(dbContext.ForecastRecords);
    }

    [Fact]
    public async Task PrepareHistoricalForecast_KeepsControllerOutOfProviderOrchestration()
    {
        var constructors = typeof(ProviderForecastOrchestrationService).GetConstructors();
        var parameterTypes = constructors.Single().GetParameters().Select(parameter => parameter.ParameterType.Name).ToList();

        Assert.Contains(nameof(IWeatherObservationSource), parameterTypes);
        Assert.Contains(nameof(IWeatherForecastingClient), parameterTypes);
        Assert.Contains("AnuradhapuraAiDbContext", parameterTypes);
        Assert.Contains(constructors.Single().GetParameters(), parameter =>
            parameter.ParameterType.IsGenericType &&
            parameter.ParameterType.GetGenericTypeDefinition() == typeof(IOptions<>));
        Assert.Contains(nameof(TimeProvider), parameterTypes);
        Assert.DoesNotContain(nameof(IForecastService), parameterTypes);
        Assert.DoesNotContain("Controller", typeof(ProviderForecastOrchestrationService).Name);
    }

    public static IEnumerable<object[]> InvalidObservationCases()
    {
        yield return [ValidObservations().Take(29).ToList(), "exactly 30"];
        yield return [ValidObservations().Take(29).Append(ValidObservations()[28]).ToList(), "duplicate"];
        yield return [ValidObservations().Select((item, index) => index == 10 ? item with { Date = item.Date.AddDays(1) } : item).ToList(), "consecutive"];
        yield return [ValidObservations().Select((item, index) => index == 0 ? item with { Date = item.Date.AddDays(1) } : item).ToList(), "consecutive"];
        yield return [ValidObservations().Select((item, index) => index == 0 ? item with { Rainfall = decimal.MinValue } : item).ToList(), "non-finite"];
        yield return [ValidObservations().Select((item, index) => index == 0 ? item with { Temperature = decimal.MaxValue } : item).ToList(), "non-finite"];
        yield return [ValidObservations().Select((item, index) => index == 0 ? item with { Humidity = decimal.MaxValue } : item).ToList(), "non-finite"];
        yield return [ValidObservations().Select((item, index) => index == 0 ? item with { Rainfall = -1m } : item).ToList(), "negative"];
        yield return [ValidObservations().Select((item, index) => index == 0 ? item with { Humidity = -1m } : item).ToList(), "relative humidity"];
        yield return [ValidObservations().Select((item, index) => index == 0 ? item with { Humidity = 101m } : item).ToList(), "relative humidity"];
        yield return [ValidObservations().Select((item, index) => index == 0 ? item with { Temperature = 61m } : item).ToList(), "temperature"];
        yield return [ValidObservations().Select((item, index) => index == 29 ? item with { Date = new DateOnly(2026, 10, 10) } : item).ToList(), "consecutive"];
    }

    private static ProviderForecastOrchestrationService CreateService(
        FakeWeatherObservationSource source,
        bool enabled = true,
        IWeatherForecastingClient? forecastingClient = null,
        AnuradhapuraAiDbContext? dbContext = null,
        string operationalSource = "OpenMeteoForecastPastDays",
        bool acceptOperationalSource = true) =>
        new(
            source,
            source,
            forecastingClient ?? new FakeForecastingClient(SampleForecastResponse(EndDate)),
            dbContext ?? CreateDbContext(),
            Options.Create(new WeatherModelFeatureOptions
            {
                EnableWeatherModelIntegration = enabled,
                OperationalWeatherSource = operationalSource,
                AcceptOperationalWeatherSource = acceptOperationalSource
            }),
            new FixedTimeProvider(FixedNow));

    private static AnuradhapuraAiDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AnuradhapuraAiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AnuradhapuraAiDbContext(options);
    }

    private static ForecastResponse SampleForecastResponse(DateOnly forecastOriginDate)
    {
        var forecasts = Enumerable.Range(1, 7)
            .Select(index => new ForecastDayResponse(
                forecastOriginDate,
                forecastOriginDate.AddDays(index),
                Temperature: 27m + index,
                Rainfall: index,
                Humidity: 70m + index,
                ModelVersion: "v1"))
            .ToList();

        return new ForecastResponse("v1", forecastOriginDate, forecasts);
    }

    private static ProviderForecastPreparationRequest ApprovedRequest() =>
        new(StartDate, EndDate);

    private static IReadOnlyList<WeatherObservation> ValidObservations(DateOnly? startDate = null)
    {
        var start = startDate ?? StartDate;
        return
        Enumerable.Range(0, 30)
            .Select(index => new WeatherObservation(
                start.AddDays(index),
                Rainfall: index,
                Temperature: 25m + index / 10m,
                Humidity: 70m + index % 10))
            .ToList();
    }

    private sealed class FakeWeatherObservationSource : IFreshWeatherObservationSource
    {
        private readonly IReadOnlyList<WeatherObservation> observations;
        private readonly WeatherObservationSourceResult? failureResult;

        public FakeWeatherObservationSource()
            : this(ValidObservations())
        {
        }

        public FakeWeatherObservationSource(IReadOnlyList<WeatherObservation> observations)
        {
            this.observations = observations;
        }

        private FakeWeatherObservationSource(WeatherObservationSourceResult failureResult)
        {
            this.observations = [];
            this.failureResult = failureResult;
        }

        public int CallCount { get; private set; }

        public WeatherObservationSourceRequest? LastRequest { get; private set; }

        public static FakeWeatherObservationSource Failure(string errorCode) =>
            new(WeatherObservationSourceResult.Failure(errorCode, "Unsafe provider details should not be surfaced."));

        public Task<WeatherObservationSourceResult> GetDailyObservationsAsync(
            WeatherObservationSourceRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            LastRequest = request;
            if (failureResult is not null)
            {
                return Task.FromResult(failureResult);
            }

            var metadata = new WeatherObservationSourceMetadata(
                "Open-Meteo",
                "era5",
                8.312229m,
                80.413055m,
                8.25m,
                80.5m,
                "Asia/Colombo",
                request.StartDate,
                request.EndDate,
                FixedNow);
            return Task.FromResult(WeatherObservationSourceResult.Success(
                new WeatherObservationSourceResponse(metadata, observations)));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FakeForecastingClient : IWeatherForecastingClient
    {
        private readonly ForecastResult<ForecastResponse> result;

        public FakeForecastingClient(ForecastResponse response)
            : this(ForecastResult<ForecastResponse>.Success(response))
        {
        }

        private FakeForecastingClient(ForecastResult<ForecastResponse> result)
        {
            this.result = result;
        }

        public int CallCount { get; private set; }

        public static FakeForecastingClient Failure(string errorCode) =>
            new(ForecastResult<ForecastResponse>.Failure(errorCode, "Unsafe forecasting details should not be surfaced."));

        public Task<ForecastResult<ForecastResponse>> GenerateForecastAsync(
            CreateForecastRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            return Task.FromResult(result);
        }
    }
}
