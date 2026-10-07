using AnuradhapuraAI.Application.Forecasting;
using Microsoft.AspNetCore.Mvc;

namespace AnuradhapuraAI.Api.Controllers;

[ApiController]
[Route("api/forecasts")]
public sealed class ForecastsController(IForecastService forecastService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateForecast(
        CreateForecastRequest request,
        CancellationToken cancellationToken)
    {
        var result = await forecastService.GenerateForecastAsync(request, cancellationToken);
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        return result.ErrorCode switch
        {
            ForecastErrorCodes.IntegrationDisabled => StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = result.Message }),
            ForecastErrorCodes.InvalidInput => BadRequest(new { message = result.Message }),
            ForecastErrorCodes.ServiceUnavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = result.Message }),
            ForecastErrorCodes.DownstreamError => StatusCode(StatusCodes.Status502BadGateway, new { message = result.Message }),
            ForecastErrorCodes.PersistenceFailed => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.Message }),
            _ => BadRequest(new { message = "Forecast request failed." })
        };
    }
}
