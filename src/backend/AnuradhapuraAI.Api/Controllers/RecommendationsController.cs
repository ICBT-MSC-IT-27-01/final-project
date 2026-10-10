using System.Security.Claims;
using AnuradhapuraAI.Application.Recommendations;
using AnuradhapuraAI.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnuradhapuraAI.Api.Controllers;

[ApiController]
[Route("api/recommendations")]
public sealed class RecommendationsController(IRecommendationPersistenceService recommendationService) : ControllerBase
{
    private const int MaximumSoilTypeLength = 100;

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> CreateRecommendation(
        CreateRecommendationRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Recommendation request body is required." });
        }

        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var registeredUserIdResult = GetTrustedRegisteredUserId();
        if (!registeredUserIdResult.Succeeded)
        {
            return registeredUserIdResult.IsAuthenticated
                ? Forbid()
                : Unauthorized(new { message = "Authenticated recommendation requests require a valid user identity." });
        }

        var soilType = string.IsNullOrWhiteSpace(request.SoilType)
            ? null
            : request.SoilType.Trim();
        var result = await recommendationService.CreateRecommendationAsync(
            new CreateRecommendationPersistenceRequest(soilType, registeredUserIdResult.UserId),
            cancellationToken);

        if (result.Succeeded && result.Value is not null)
        {
            return Ok(ToResponse(result.Value));
        }

        return ToFailureResult(result.ErrorCode, result.Message);
    }

    private static string? ValidateRequest(CreateRecommendationRequest request)
    {
        if (!IsSupportedDistrict(request.District))
        {
            return "Only Anuradhapura District recommendations are supported.";
        }

        if (request.SoilType is not null && request.SoilType.Length > MaximumSoilTypeLength)
        {
            return "Soil type is too long.";
        }

        return null;
    }

    private TrustedUserResult GetTrustedRegisteredUserId()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return TrustedUserResult.Public();
        }

        var role = User.FindFirstValue(ClaimTypes.Role);
        if (!string.Equals(role, ApprovedRoleNames.RegisteredUser, StringComparison.Ordinal))
        {
            return TrustedUserResult.Forbidden();
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) && userId > 0
            ? TrustedUserResult.Registered(userId)
            : TrustedUserResult.InvalidAuthenticated();
    }

    private static bool IsSupportedDistrict(string? district) =>
        !string.IsNullOrWhiteSpace(district) &&
        (string.Equals(district.Trim(), "Anuradhapura", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(district.Trim(), "Anuradhapura District", StringComparison.OrdinalIgnoreCase));

    private static RecommendationResponse ToResponse(PersistedRecommendationResponse response) =>
        new(
            response.RecommendationId,
            response.ForecastRunId,
            response.UserId,
            response.SoilType,
            response.CreatedAt,
            new RecommendationForecastResponse(
                response.Forecast.ForecastRunId,
                response.Forecast.ForecastDate,
                response.Forecast.TargetStartDate,
                response.Forecast.TargetEndDate,
                response.Forecast.ModelVersion,
                response.Forecast.CreatedAt,
                response.Forecast.ForecastDays
                    .Select(day => new RecommendationForecastDayResponse(
                        day.TargetDate,
                        day.Rainfall,
                        day.Temperature,
                        day.Humidity))
                    .ToList()),
            response.Crops.Select(ToCropResponse).ToList());

    private static RecommendationCropResponse ToCropResponse(RankedCropRecommendationEvaluation crop) =>
        new(
            crop.CropName,
            crop.EvaluationStatus,
            crop.RainfallScore,
            crop.TemperatureScore,
            crop.HumidityScore,
            crop.SoilScore,
            crop.OverallScore,
            crop.SuitabilityCategory,
            crop.Rank,
            crop.UnavailableFactors,
            crop.Factors.Select(factor => new RecommendationFactorResponse(
                    factor.Factor,
                    factor.IsEvaluable,
                    factor.Score,
                    factor.AggregatedValue,
                    factor.ConfiguredWeight,
                    factor.EffectiveWeight,
                    factor.Explanation))
                .ToList(),
            crop.EvidenceCoverage,
            crop.ClimateRisks,
            crop.Explanation);

    private IActionResult ToFailureResult(string? errorCode, string? message) => errorCode switch
    {
        RecommendationOrchestrationErrorCodes.ForecastUnavailable => StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            new { message = message ?? "No complete forecast is available." }),
        RecommendationOrchestrationErrorCodes.InvalidUserContext => Forbid(),
        RecommendationOrchestrationErrorCodes.InvalidConfiguration => StatusCode(
            StatusCodes.Status500InternalServerError,
            new { message = "Recommendation configuration is invalid." }),
        RecommendationOrchestrationErrorCodes.DatabaseReadFailed => StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            new { message = "Recommendation data is temporarily unavailable." }),
        RecommendationOrchestrationErrorCodes.PersistenceFailed => StatusCode(
            StatusCodes.Status500InternalServerError,
            new { message = "Recommendation could not be saved." }),
        RecommendationOrchestrationErrorCodes.SnapshotSerializationFailed => StatusCode(
            StatusCodes.Status500InternalServerError,
            new { message = "Recommendation evidence could not be prepared." }),
        _ => BadRequest(new { message = "Recommendation request failed." })
    };

    private sealed record TrustedUserResult(
        bool Succeeded,
        bool IsAuthenticated,
        int? UserId)
    {
        public static TrustedUserResult Public() => new(true, false, null);

        public static TrustedUserResult Registered(int userId) => new(true, true, userId);

        public static TrustedUserResult Forbidden() => new(false, true, null);

        public static TrustedUserResult InvalidAuthenticated() => new(false, false, null);
    }
}
