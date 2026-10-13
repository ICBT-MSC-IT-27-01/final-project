namespace AnuradhapuraAI.Application.Forecasting;

public interface IProviderForecastOrchestrationService
{
    Task<ProviderForecastPreparationResult> PrepareHistoricalForecastAsync(
        ProviderForecastPreparationRequest request,
        CancellationToken cancellationToken = default);

    Task<ProviderForecastGenerationResult> GenerateOperationalForecastAsync(
        CancellationToken cancellationToken = default);
}
