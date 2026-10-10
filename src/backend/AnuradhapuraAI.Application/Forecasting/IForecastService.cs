namespace AnuradhapuraAI.Application.Forecasting;

public interface IForecastService
{
    Task<ForecastResult<ForecastResponse>> GenerateForecastAsync(
        CreateForecastRequest request,
        CancellationToken cancellationToken = default);

    Task<ForecastResult<LatestForecastResponse>> GetLatestForecastAsync(
        CancellationToken cancellationToken = default);
}
