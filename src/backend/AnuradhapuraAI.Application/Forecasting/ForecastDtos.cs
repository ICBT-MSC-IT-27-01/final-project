namespace AnuradhapuraAI.Application.Forecasting;

public sealed record ForecastObservationRequest(
    DateOnly Date,
    decimal? Temperature,
    decimal? Rainfall,
    decimal? Humidity);

public sealed record CreateForecastRequest(
    IReadOnlyList<ForecastObservationRequest> Observations);

public sealed record ForecastDayResponse(
    DateOnly ForecastDate,
    DateOnly TargetDate,
    decimal Temperature,
    decimal Rainfall,
    decimal Humidity,
    string ModelVersion);

public sealed record ForecastResponse(
    string ModelVersion,
    DateOnly ForecastDate,
    IReadOnlyList<ForecastDayResponse> Forecasts);

public sealed record ForecastingServiceOptions
{
    public const string SectionName = "ForecastingService";

    public string BaseUrl { get; set; } = string.Empty;
}

public sealed record WeatherModelFeatureOptions
{
    public const string SectionName = "FeatureFlags";

    public bool EnableOfficerValidation { get; set; }

    public bool EnableRecommendationHistory { get; set; }

    public bool EnableClimateRiskIndicators { get; set; }

    public bool EnableWeatherModelIntegration { get; set; }
}
