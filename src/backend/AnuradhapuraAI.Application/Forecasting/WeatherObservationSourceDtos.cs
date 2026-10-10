namespace AnuradhapuraAI.Application.Forecasting;

public sealed record WeatherObservationSourceRequest(
    decimal Latitude,
    decimal Longitude,
    DateOnly StartDate,
    DateOnly EndDate,
    string Product,
    string Timezone);

public sealed record WeatherObservationSourceMetadata(
    string Provider,
    string Product,
    decimal Latitude,
    decimal Longitude,
    string Timezone,
    DateOnly StartDate,
    DateOnly EndDate,
    DateTimeOffset RetrievedAt);

public sealed record WeatherObservation(
    DateOnly Date,
    decimal Rainfall,
    decimal Temperature,
    decimal Humidity);

public sealed record WeatherObservationSourceResponse(
    WeatherObservationSourceMetadata Metadata,
    IReadOnlyList<WeatherObservation> Observations);

public sealed record WeatherObservationSourceResult
{
    private WeatherObservationSourceResult(
        bool succeeded,
        WeatherObservationSourceResponse? value,
        string? errorCode,
        string? message)
    {
        Succeeded = succeeded;
        Value = value;
        ErrorCode = errorCode;
        Message = message;
    }

    public bool Succeeded { get; }

    public WeatherObservationSourceResponse? Value { get; }

    public string? ErrorCode { get; }

    public string? Message { get; }

    public static WeatherObservationSourceResult Success(WeatherObservationSourceResponse value) =>
        new(true, value, null, null);

    public static WeatherObservationSourceResult Failure(string errorCode, string message) =>
        new(false, null, errorCode, message);
}

public static class WeatherObservationSourceErrorCodes
{
    public const string InvalidRequest = nameof(InvalidRequest);
    public const string ProviderUnavailable = nameof(ProviderUnavailable);
    public const string RateLimited = nameof(RateLimited);
    public const string InvalidResponse = nameof(InvalidResponse);
    public const string MissingWeatherDays = nameof(MissingWeatherDays);
}
