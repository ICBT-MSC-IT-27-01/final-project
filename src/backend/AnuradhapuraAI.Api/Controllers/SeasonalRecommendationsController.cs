using AnuradhapuraAI.Application.Seasonal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace AnuradhapuraAI.Api.Controllers;
[ApiController, Route("api/seasonal-recommendations")]
public sealed class SeasonalRecommendationsController(ISeasonalClimateService service) : ControllerBase
{
 [AllowAnonymous, HttpPost("by-date-range")] public async Task<IActionResult> ByDateRange(SeasonalRangeRequest request, CancellationToken ct) { try { return Ok(await service.FindRecommendedCropsAsync(request,ct)); } catch (ArgumentException e) { return BadRequest(new { message=e.Message}); } catch (FileNotFoundException e) { return StatusCode(503,new { message=e.Message, status="HISTORICAL_DATA_UNAVAILABLE"}); } }
 [AllowAnonymous, HttpPost("by-crop")] public async Task<IActionResult> ByCrop(SeasonalCropRequest request, CancellationToken ct) { try { return Ok(await service.FindSuitablePeriodsAsync(request,ct)); } catch (ArgumentException e) { return BadRequest(new { message=e.Message}); } catch (FileNotFoundException e) { return StatusCode(503,new { message=e.Message, status="HISTORICAL_DATA_UNAVAILABLE"}); } }
 [AllowAnonymous, HttpGet("supported-crops")] public IActionResult Crops() => Ok(new[] { "Paddy", "Maize", "Green Gram", "Cowpea", "Groundnut", "Chilli" });
}
