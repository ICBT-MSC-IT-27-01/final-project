using System.Net.Http;

namespace AnuradhapuraAI.Infrastructure.Forecasting;

public sealed class OpenMeteoNoRedirectHttpClientHandler : HttpClientHandler
{
    public OpenMeteoNoRedirectHttpClientHandler()
    {
        AllowAutoRedirect = false;
    }
}
