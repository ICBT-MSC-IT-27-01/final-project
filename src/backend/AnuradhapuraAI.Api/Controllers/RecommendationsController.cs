using System.Security.Claims;
using AnuradhapuraAI.Application.Recommendations;
using AnuradhapuraAI.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnuradhapuraAI.Api.Controllers;

[ApiController]
[Route("api/recommendations")]
public sealed class RecommendationsController(
    IRecommendationPersistenceService recommendationService,
    IRecommendationHistoryService historyService,
    IRecommendationOrchestrationService orchestrationService,
    IRecommendationRankingService rankingService) : ControllerBase
{
    private const int MaximumSoilTypeLength = 100;

    [AllowAnonymous]
    [HttpPost("evaluate")]
    public async Task<IActionResult> EvaluateRecommendation(
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

        if (User.Identity?.IsAuthenticated == true)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (!string.Equals(role, ApprovedRoleNames.RegisteredUser, StringComparison.Ordinal) &&
                !string.Equals(role, ApprovedRoleNames.AgriculturalOfficer, StringComparison.Ordinal) &&
                !string.Equals(role, ApprovedRoleNames.Administrator, StringComparison.Ordinal))
            {
                return Forbid();
            }
        }

        var soilType = NormalizeSoilType(request.SoilType);
        var result = await orchestrationService.EvaluateLatestForecastAsync(
            new RecommendationEvaluationRequest(soilType),
            cancellationToken);

        if (!result.Succeeded || result.Value is null)
        {
            return ToFailureResult(result.ErrorCode, result.Message);
        }

        var ranked = rankingService.Rank(result.Value);
        return Ok(ToEvaluationResponse(ranked, soilType));
    }

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

        var soilType = NormalizeSoilType(request.SoilType);
        var result = await recommendationService.CreateRecommendationAsync(
            new CreateRecommendationPersistenceRequest(soilType, registeredUserIdResult.UserId),
            cancellationToken);

        if (result.Succeeded && result.Value is not null)
        {
            return Ok(ToResponse(result.Value));
        }

        return ToFailureResult(result.ErrorCode, result.Message);
    }

    [Authorize]
    [HttpGet("history")]
    public async Task<IActionResult> ListHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var registeredUserIdResult = GetTrustedRegisteredUserId();
        if (!registeredUserIdResult.Succeeded || registeredUserIdResult.UserId is null)
        {
            return registeredUserIdResult.IsAuthenticated
                ? Forbid()
                : Unauthorized(new { message = "Registered user identity is required." });
        }

        var result = await historyService.ListHistoryAsync(
            new RecommendationHistoryQuery(registeredUserIdResult.UserId.Value, page, pageSize),
            cancellationToken);

        return result.Succeeded && result.Value is not null
            ? Ok(result.Value)
            : ToHistoryFailureResult(result.ErrorCode);
    }

    [Authorize]
    [HttpGet("{recommendationId:int}")]
    public async Task<IActionResult> GetHistoryDetail(
        int recommendationId,
        CancellationToken cancellationToken = default)
    {
        var registeredUserIdResult = GetTrustedRegisteredUserId();
        if (!registeredUserIdResult.Succeeded || registeredUserIdResult.UserId is null)
        {
            return registeredUserIdResult.IsAuthenticated
                ? Forbid()
                : Unauthorized(new { message = "Registered user identity is required." });
        }

        var result = await historyService.GetDetailAsync(
            recommendationId,
            registeredUserIdResult.UserId.Value,
            cancellationToken);

        return result.Succeeded && result.Value is not null
            ? Ok(result.Value)
            : ToHistoryFailureResult(result.ErrorCode);
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

    private static string? NormalizeSoilType(string? soilType) =>
        string.IsNullOrWhiteSpace(soilType)
            ? null
            : soilType.Trim();

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

    private static RecommendationEvaluationApiResponse ToEvaluationResponse(
        RankedRecommendationEvaluationResponse response,
        string? soilType) =>
        new(
            response.Forecast.ForecastRunId,
            soilType,
            ToForecastResponse(response.Forecast),
            response.Crops.Select(ToCropResponse).ToList(),
            [
                "This is a technical suitability evaluation, not a guarantee of crop success, yield, or profitability.",
                "Missing factors are reported explicitly and are not replaced with fabricated scores.",
                "Scientific/agronomic validation remains separate from technical system validation."
            ]);

    private static RecommendationResponse ToResponse(PersistedRecommendationResponse response) =>
        new(
            response.RecommendationId,
            response.ForecastRunId,
            response.UserId,
            response.SoilType,
            response.CreatedAt,
            ToForecastResponse(response.Forecast),
            response.Crops.Select(ToCropResponse).ToList());

    private static RecommendationForecastResponse ToForecastResponse(SelectedForecastRun forecast) =>
        new(
            forecast.ForecastRunId,
            forecast.ForecastDate,
            forecast.TargetStartDate,
            forecast.TargetEndDate,
            forecast.ModelVersion,
            forecast.CreatedAt,
            forecast.ForecastDays
                .Select(day => new RecommendationForecastDayResponse(
                    day.TargetDate,
                    day.Rainfall,
                    day.Temperature,
                    day.Humidity))
                .ToList());

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
            crop.Explanation)
        {
            AssessmentState = crop.AssessmentState,
            AssessmentBasis = crop.AssessmentBasis,
            EvidenceSources = crop.EvidenceSources,
            ScientificLimitations = crop.ScientificLimitations
        };

    private IActionResult ToFailureResult(string? errorCode, string? message) => errorCode switch
    {
        RecommendationOrchestrationErrorCodes.FeatureDisabled => StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            new { message = message ?? "Recommendation history is disabled." }),
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

    private IActionResult ToHistoryFailureResult(string? errorCode) => errorCode switch
    {
        RecommendationHistoryErrorCodes.FeatureDisabled => StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            new { message = "Recommendation history is disabled." }),
        RecommendationHistoryErrorCodes.InvalidUserContext => Forbid(),
        RecommendationHistoryErrorCodes.InvalidPaging => BadRequest(
            new { message = "History pagination is invalid." }),
        RecommendationHistoryErrorCodes.RecommendationNotFound => NotFound(
            new { message = "Recommendation was not found." }),
        RecommendationHistoryErrorCodes.DatabaseReadFailed => StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            new { message = "Recommendation history is temporarily unavailable." }),
        _ => BadRequest(new { message = "Recommendation history request failed." })
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
