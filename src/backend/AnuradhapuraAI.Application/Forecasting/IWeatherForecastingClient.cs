namespace AnuradhapuraAI.Application.Forecasting;

public interface IWeatherForecastingClient
{
    Task<ForecastResult<ForecastResponse>> GenerateForecastAsync(
        CreateForecastRequest request,
        CancellationToken cancellationToken = default);
}
