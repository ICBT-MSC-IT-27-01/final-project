using System.Security.Claims;
using AnuradhapuraAI.Application.Validations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnuradhapuraAI.Api.Controllers;

[ApiController]
[Authorize(Policy = "AgriculturalOfficerOnly")]
[Route("api/validations")]
public sealed class ValidationsController(IRecommendationValidationService validationService) : ControllerBase
{
    [HttpGet("pending")]
    public async Task<IActionResult> ListPending(CancellationToken cancellationToken)
    {
        var result = await validationService.ListPendingReviewsAsync(cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : ToFailureResult(result.ErrorCode);
    }

    [HttpGet("recommendations/{recommendationId:int}")]
    public async Task<IActionResult> GetRecommendationReview(
        int recommendationId,
        CancellationToken cancellationToken)
    {
        var result = await validationService.GetReviewAsync(recommendationId, cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : ToFailureResult(result.ErrorCode);
    }

    [HttpPost("recommendations/{recommendationId:int}")]
    public async Task<IActionResult> SubmitValidation(
        int recommendationId,
        SubmitRecommendationValidationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetTrustedOfficerUserId(out var officerUserId))
        {
            return Unauthorized(new { message = "Authenticated Agricultural Officer identity is required." });
        }

        var result = await validationService.SubmitValidationAsync(
            recommendationId,
            new RecommendationValidationSubmission(request.Status, request.Comment, officerUserId),
            cancellationToken);

        return result.Succeeded
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : ToFailureResult(result.ErrorCode);
    }

    private bool TryGetTrustedOfficerUserId(out int officerUserId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out officerUserId) && officerUserId > 0;
    }

    private IActionResult ToFailureResult(string? errorCode) => errorCode switch
    {
        RecommendationValidationErrorCodes.FeatureDisabled => StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            new { message = "Agricultural Officer validation is disabled." }),
        RecommendationValidationErrorCodes.RecommendationNotFound => NotFound(
            new { message = "Recommendation was not found." }),
        RecommendationValidationErrorCodes.InvalidReviewer => Forbid(),
        RecommendationValidationErrorCodes.InvalidStatus => BadRequest(
            new { message = "Validation status is invalid." }),
        RecommendationValidationErrorCodes.InvalidComment => BadRequest(
            new { message = "Validation comment is too long." }),
        RecommendationValidationErrorCodes.DatabaseReadFailed => StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            new { message = "Recommendation validation data is temporarily unavailable." }),
        RecommendationValidationErrorCodes.PersistenceFailed => StatusCode(
            StatusCodes.Status500InternalServerError,
            new { message = "Recommendation validation could not be saved." }),
        _ => BadRequest(new { message = "Validation request failed." })
    };
}
