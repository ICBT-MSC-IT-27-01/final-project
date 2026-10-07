using System.Net;
using System.Net.Http.Json;
using AnuradhapuraAI.Application.Forecasting;

namespace AnuradhapuraAI.Infrastructure.Forecasting;

public sealed class PythonForecastingClient(HttpClient httpClient) : IWeatherForecastingClient
{
    public async Task<ForecastResult<ForecastResponse>> GenerateForecastAsync(
        CreateForecastRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync("forecast", ToPythonRequest(request), cancellationToken);
        }
        catch (HttpRequestException)
        {
            return ForecastResult<ForecastResponse>.Failure(
                ForecastErrorCodes.ServiceUnavailable,
                "Forecasting service is unavailable.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ForecastResult<ForecastResponse>.Failure(
                ForecastErrorCodes.ServiceUnavailable,
                "Forecasting service request timed out.");
        }
        catch (InvalidOperationException)
        {
            return ForecastResult<ForecastResponse>.Failure(
                ForecastErrorCodes.ServiceUnavailable,
                "Forecasting service base URL is not configured.");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return ForecastResult<ForecastResponse>.Failure(
                ForecastErrorCodes.InvalidInput,
                "Forecast request was rejected by the forecasting service.");
        }

        if (!response.IsSuccessStatusCode)
        {
            return ForecastResult<ForecastResponse>.Failure(
                ForecastErrorCodes.ServiceUnavailable,
                "Forecasting service returned an unavailable response.");
        }

        PythonForecastResponse? pythonResponse;
        try
        {
            pythonResponse = await response.Content.ReadFromJsonAsync<PythonForecastResponse>(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return MalformedResponse();
        }

        if (pythonResponse is null || pythonResponse.Forecasts is null || pythonResponse.Forecasts.Count != 7)
        {
            return MalformedResponse();
        }

        var forecasts = pythonResponse.Forecasts
            .Select(item => new ForecastDayResponse(
                item.ForecastDate,
                item.TargetDate,
                item.Temperature,
                item.Rainfall,
                item.Humidity,
                item.ModelVersion))
            .ToList();

        if (forecasts.Any(item => item.ModelVersion != pythonResponse.ModelVersion))
        {
            return MalformedResponse();
        }

        return ForecastResult<ForecastResponse>.Success(
            new ForecastResponse(pythonResponse.ModelVersion, pythonResponse.ForecastDate, forecasts));
    }

    private static ForecastResult<ForecastResponse> MalformedResponse()
        => ForecastResult<ForecastResponse>.Failure(
            ForecastErrorCodes.DownstreamError,
            "Forecasting service returned a malformed response.");

    private static PythonForecastRequest ToPythonRequest(CreateForecastRequest request)
        => new(request.Observations
            .Select(item => new PythonObservationRequest(
                item.Date,
                item.Temperature!.Value,
                item.Rainfall!.Value,
                item.Humidity!.Value))
            .ToList());

    private sealed record PythonForecastRequest(IReadOnlyList<PythonObservationRequest> Observations);

    private sealed record PythonObservationRequest(
        DateOnly Date,
        decimal Temperature,
        decimal Rainfall,
        decimal Humidity);

    private sealed record PythonForecastResponse(
        string ModelVersion,
        DateOnly ForecastDate,
        IReadOnlyList<PythonForecastItem> Forecasts);

    private sealed record PythonForecastItem(
        DateOnly ForecastDate,
        DateOnly TargetDate,
        decimal Temperature,
        decimal Rainfall,
        decimal Humidity,
        string ModelVersion);
}
