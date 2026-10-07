using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Infrastructure.Forecasting;
using System.Net;
using System.Text;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class PythonForecastingClientTests
{
    [Fact]
    public async Task GenerateForecastAsync_ReturnsMalformedResponse_WhenJsonShapeIsInvalid()
    {
        using var httpClient = new HttpClient(new FixedResponseHandler(HttpStatusCode.OK, "{}"))
        {
            BaseAddress = new Uri("http://forecasting-service.test/")
        };
        var client = new PythonForecastingClient(httpClient);

        var result = await client.GenerateForecastAsync(SampleRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(ForecastErrorCodes.DownstreamError, result.ErrorCode);
    }

    [Fact]
    public async Task GenerateForecastAsync_ReturnsUnavailable_WhenServiceCannotBeReached()
    {
        using var httpClient = new HttpClient(new ThrowingHandler())
        {
            BaseAddress = new Uri("http://forecasting-service.test/")
        };
        var client = new PythonForecastingClient(httpClient);

        var result = await client.GenerateForecastAsync(SampleRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(ForecastErrorCodes.ServiceUnavailable, result.ErrorCode);
    }

    private static CreateForecastRequest SampleRequest()
    {
        var start = new DateOnly(2025, 1, 1);
        return new CreateForecastRequest(Enumerable.Range(0, 30)
            .Select(index => new ForecastObservationRequest(start.AddDays(index), 25, 1, 80))
            .ToList());
    }

    private sealed class FixedResponseHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("Unavailable.");
    }
}
