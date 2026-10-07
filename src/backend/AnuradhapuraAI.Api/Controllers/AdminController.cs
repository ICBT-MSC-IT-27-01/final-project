using System.Security.Claims;
using AnuradhapuraAI.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnuradhapuraAI.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdministratorOnly")]
[Route("api/admin")]
public sealed class AdminController(IAdminManagementService adminService) : ControllerBase
{
    [HttpGet("crops")]
    public Task<PagedResponse<CropResponse>> ListCrops([FromQuery] bool? isActive, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => adminService.ListCropsAsync(isActive, page, pageSize, cancellationToken);

    [HttpGet("crops/{id:int}")]
    public async Task<IActionResult> GetCrop(int id, CancellationToken cancellationToken) => ToActionResult(await adminService.GetCropAsync(id, cancellationToken));

    [HttpPost("crops")]
    public async Task<IActionResult> CreateCrop(CreateCropRequest request, CancellationToken cancellationToken) => ToCreatedResult(await adminService.CreateCropAsync(request, cancellationToken));

    [HttpPut("crops/{id:int}")]
    public async Task<IActionResult> UpdateCrop(int id, UpdateCropRequest request, CancellationToken cancellationToken) => ToActionResult(await adminService.UpdateCropAsync(id, request, cancellationToken));

    [HttpPatch("crops/{id:int}/active-status")]
    public async Task<IActionResult> SetCropActiveStatus(int id, UpdateActiveStatusRequest request, CancellationToken cancellationToken) => ToActionResult(await adminService.SetCropActiveStatusAsync(id, request.IsActive, cancellationToken));

    [HttpGet("crop-requirements")]
    public Task<PagedResponse<CropRequirementResponse>> ListCropRequirements([FromQuery] int? cropId, [FromQuery] string? variableType, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => adminService.ListCropRequirementsAsync(cropId, variableType, page, pageSize, cancellationToken);

    [HttpGet("crop-requirements/{id:int}")]
    public async Task<IActionResult> GetCropRequirement(int id, CancellationToken cancellationToken) => ToActionResult(await adminService.GetCropRequirementAsync(id, cancellationToken));

    [HttpPost("crop-requirements")]
    public async Task<IActionResult> CreateCropRequirement(CreateCropRequirementRequest request, CancellationToken cancellationToken) => ToCreatedResult(await adminService.CreateCropRequirementAsync(request, cancellationToken));

    [HttpPut("crop-requirements/{id:int}")]
    public async Task<IActionResult> UpdateCropRequirement(int id, UpdateCropRequirementRequest request, CancellationToken cancellationToken) => ToActionResult(await adminService.UpdateCropRequirementAsync(id, request, cancellationToken));

    [HttpPatch("crop-requirements/{id:int}/active-status")]
    public async Task<IActionResult> SetCropRequirementActiveStatus(int id, UpdateActiveStatusRequest request, CancellationToken cancellationToken) => ToActionResult(await adminService.SetCropRequirementActiveStatusAsync(id, request.IsActive, cancellationToken));

    [HttpGet("soil-compatibility")]
    public Task<PagedResponse<SoilCompatibilityResponse>> ListSoilCompatibility([FromQuery] int? cropId, [FromQuery] string? soilType, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => adminService.ListSoilCompatibilitiesAsync(cropId, soilType, page, pageSize, cancellationToken);

    [HttpGet("soil-compatibility/{id:int}")]
    public async Task<IActionResult> GetSoilCompatibility(int id, CancellationToken cancellationToken) => ToActionResult(await adminService.GetSoilCompatibilityAsync(id, cancellationToken));

    [HttpPost("soil-compatibility")]
    public async Task<IActionResult> CreateSoilCompatibility(CreateSoilCompatibilityRequest request, CancellationToken cancellationToken) => ToCreatedResult(await adminService.CreateSoilCompatibilityAsync(request, cancellationToken));

    [HttpPut("soil-compatibility/{id:int}")]
    public async Task<IActionResult> UpdateSoilCompatibility(int id, UpdateSoilCompatibilityRequest request, CancellationToken cancellationToken) => ToActionResult(await adminService.UpdateSoilCompatibilityAsync(id, request, cancellationToken));

    [HttpPatch("soil-compatibility/{id:int}/active-status")]
    public async Task<IActionResult> SetSoilCompatibilityActiveStatus(int id, UpdateActiveStatusRequest request, CancellationToken cancellationToken) => ToActionResult(await adminService.SetSoilCompatibilityActiveStatusAsync(id, request.IsActive, cancellationToken));

    [HttpGet("suitability-config")]
    public Task<PagedResponse<SuitabilityConfigurationResponse>> ListSuitabilityConfigurations([FromQuery] string? configurationType, [FromQuery] bool? isActive, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => adminService.ListSuitabilityConfigurationsAsync(configurationType, isActive, page, pageSize, cancellationToken);

    [HttpGet("suitability-config/{id:int}")]
    public async Task<IActionResult> GetSuitabilityConfiguration(int id, CancellationToken cancellationToken) => ToActionResult(await adminService.GetSuitabilityConfigurationAsync(id, cancellationToken));

    [HttpPost("suitability-config")]
    public async Task<IActionResult> CreateSuitabilityConfiguration(CreateSuitabilityConfigurationRequest request, CancellationToken cancellationToken) => ToCreatedResult(await adminService.CreateSuitabilityConfigurationAsync(request, GetUserId(), cancellationToken));

    [HttpPut("suitability-config/{id:int}")]
    public async Task<IActionResult> UpdateSuitabilityConfiguration(int id, UpdateSuitabilityConfigurationRequest request, CancellationToken cancellationToken) => ToActionResult(await adminService.UpdateSuitabilityConfigurationAsync(id, request, GetUserId(), cancellationToken));

    [HttpPatch("suitability-config/{id:int}/active-status")]
    public async Task<IActionResult> SetSuitabilityConfigurationActiveStatus(int id, UpdateActiveStatusRequest request, CancellationToken cancellationToken) => ToActionResult(await adminService.SetSuitabilityConfigurationActiveStatusAsync(id, request.IsActive, GetUserId(), cancellationToken));

    [HttpGet("users")]
    public Task<PagedResponse<AdminUserResponse>> ListUsers([FromQuery] string? role, [FromQuery] bool? isActive, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => adminService.ListUsersAsync(role, isActive, page, pageSize, cancellationToken);

    [HttpGet("users/{id:int}")]
    public async Task<IActionResult> GetUser(int id, CancellationToken cancellationToken) => ToActionResult(await adminService.GetUserAsync(id, cancellationToken));

    [HttpPatch("users/{id:int}/active-status")]
    public async Task<IActionResult> SetUserActiveStatus(int id, UpdateActiveStatusRequest request, CancellationToken cancellationToken) => ToActionResult(await adminService.SetUserActiveStatusAsync(id, request.IsActive, cancellationToken));

    [HttpPut("users/{id:int}/role")]
    public async Task<IActionResult> UpdateUserRole(int id, UpdateUserRoleRequest request, CancellationToken cancellationToken) => ToActionResult(await adminService.UpdateUserRoleAsync(id, request, cancellationToken));

    [HttpGet("recommendations")]
    public Task<PagedResponse<AdminRecommendationSummaryResponse>> ListRecommendations([FromQuery] int? userId, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => adminService.ListRecommendationsAsync(userId, from, to, page, pageSize, cancellationToken);

    [HttpGet("recommendations/{id:int}")]
    public async Task<IActionResult> GetRecommendation(int id, CancellationToken cancellationToken) => ToActionResult(await adminService.GetRecommendationAsync(id, cancellationToken));

    [HttpGet("validations")]
    public Task<PagedResponse<AdminValidationResponse>> ListValidations([FromQuery] string? status, [FromQuery] int? officerUserId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => adminService.ListValidationsAsync(status, officerUserId, page, pageSize, cancellationToken);

    [HttpGet("validations/{id:int}")]
    public async Task<IActionResult> GetValidation(int id, CancellationToken cancellationToken) => ToActionResult(await adminService.GetValidationAsync(id, cancellationToken));

    private int GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : 0;
    }

    private IActionResult ToCreatedResult<T>(AdminResult<T> result) => result.Succeeded ? StatusCode(StatusCodes.Status201Created, result.Value) : ToFailureResult(result.ErrorCode);

    private IActionResult ToActionResult<T>(AdminResult<T> result) => result.Succeeded ? Ok(result.Value) : ToFailureResult(result.ErrorCode);

    private IActionResult ToFailureResult(string? errorCode) => errorCode switch
    {
        AdminErrorCodes.NotFound => NotFound(new { message = "Resource was not found." }),
        AdminErrorCodes.Duplicate => Conflict(new { message = "A duplicate record already exists." }),
        AdminErrorCodes.InvalidRole => BadRequest(new { message = "Role is invalid." }),
        AdminErrorCodes.InvalidVariableType => BadRequest(new { message = "Variable type is invalid." }),
        AdminErrorCodes.InvalidConfigurationType => BadRequest(new { message = "Configuration type is invalid." }),
        AdminErrorCodes.InvalidConfigurationKey => BadRequest(new { message = "Configuration key is invalid." }),
        AdminErrorCodes.InvalidRange => BadRequest(new { message = "Minimum value must not exceed maximum value." }),
        AdminErrorCodes.InvalidReference => BadRequest(new { message = "Referenced record is invalid." }),
        _ => BadRequest(new { message = "Request is invalid." })
    };
}
