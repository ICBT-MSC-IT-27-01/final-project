namespace AnuradhapuraAI.Application.Forecasting;

public interface IProviderForecastOrchestrationService
{
    Task<ProviderForecastPreparationResult> PrepareHistoricalForecastAsync(
        ProviderForecastPreparationRequest request,
        CancellationToken cancellationToken = default);
}
