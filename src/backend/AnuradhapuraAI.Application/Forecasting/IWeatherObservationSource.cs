namespace AnuradhapuraAI.Application.Forecasting;

public interface IWeatherObservationSource
{
    Task<WeatherObservationSourceResult> GetDailyObservationsAsync(
        WeatherObservationSourceRequest request,
        CancellationToken cancellationToken = default);
}
