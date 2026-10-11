namespace AnuradhapuraAI.Application.Forecasting;

public sealed record ProviderForecastPreparationRequest(
    DateOnly ObservationWindowStart,
    DateOnly ObservationWindowEnd);

public sealed record PreparedProviderForecast(
    CreateForecastRequest ForecastRequest,
    DateOnly ObservationWindowStart,
    DateOnly ObservationWindowEnd,
    DateOnly ForecastOriginDate,
    IReadOnlyList<DateOnly> ForecastTargetDates,
    ProviderForecastRunPurpose RunPurpose,
    ProviderForecastProvenance Provenance);

public sealed record ProviderForecastProvenance(
    string ProviderName,
    string ProviderProduct,
    decimal RequestedLatitude,
    decimal RequestedLongitude,
    string Timezone,
    DateOnly ObservationWindowStart,
    DateOnly ObservationWindowEnd,
    DateOnly ForecastOriginDate,
    DateOnly ForecastTargetStart,
    DateOnly ForecastTargetEnd,
    ProviderForecastRunPurpose RunPurpose,
    DateTimeOffset? LocallyRecordedRetrievedAt);

public sealed record ProviderForecastPreparationResult
{
    private ProviderForecastPreparationResult(
        bool succeeded,
        PreparedProviderForecast? value,
        string? errorCode,
        string? message)
    {
        Succeeded = succeeded;
        Value = value;
        ErrorCode = errorCode;
        Message = message;
    }

    public bool Succeeded { get; }

    public PreparedProviderForecast? Value { get; }

    public string? ErrorCode { get; }

    public string? Message { get; }

    public static ProviderForecastPreparationResult Success(PreparedProviderForecast value) =>
        new(true, value, null, null);

    public static ProviderForecastPreparationResult Failure(string errorCode, string message) =>
        new(false, null, errorCode, message);
}

public enum ProviderForecastRunPurpose
{
    Operational = 0,
    HistoricalEvaluation = 1
}

public static class ProviderForecastPreparationErrorCodes
{
    public const string FeatureDisabled = nameof(FeatureDisabled);
    public const string InvalidObservationWindow = nameof(InvalidObservationWindow);
    public const string InvalidProviderData = nameof(InvalidProviderData);
    public const string ProviderUnavailable = nameof(ProviderUnavailable);
    public const string RateLimited = nameof(RateLimited);
}
