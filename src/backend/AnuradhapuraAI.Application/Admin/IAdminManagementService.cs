namespace AnuradhapuraAI.Application.Admin;

public interface IAdminManagementService
{
    Task<PagedResponse<CropResponse>> ListCropsAsync(bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AdminResult<CropResponse>> GetCropAsync(int id, CancellationToken cancellationToken = default);
    Task<AdminResult<CropResponse>> CreateCropAsync(CreateCropRequest request, CancellationToken cancellationToken = default);
    Task<AdminResult<CropResponse>> UpdateCropAsync(int id, UpdateCropRequest request, CancellationToken cancellationToken = default);
    Task<AdminResult<CropResponse>> SetCropActiveStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default);

    Task<PagedResponse<CropRequirementResponse>> ListCropRequirementsAsync(int? cropId, string? variableType, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AdminResult<CropRequirementResponse>> GetCropRequirementAsync(int id, CancellationToken cancellationToken = default);
    Task<AdminResult<CropRequirementResponse>> CreateCropRequirementAsync(CreateCropRequirementRequest request, CancellationToken cancellationToken = default);
    Task<AdminResult<CropRequirementResponse>> UpdateCropRequirementAsync(int id, UpdateCropRequirementRequest request, CancellationToken cancellationToken = default);
    Task<AdminResult<CropRequirementResponse>> SetCropRequirementActiveStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default);

    Task<PagedResponse<SoilCompatibilityResponse>> ListSoilCompatibilitiesAsync(int? cropId, string? soilType, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AdminResult<SoilCompatibilityResponse>> GetSoilCompatibilityAsync(int id, CancellationToken cancellationToken = default);
    Task<AdminResult<SoilCompatibilityResponse>> CreateSoilCompatibilityAsync(CreateSoilCompatibilityRequest request, CancellationToken cancellationToken = default);
    Task<AdminResult<SoilCompatibilityResponse>> UpdateSoilCompatibilityAsync(int id, UpdateSoilCompatibilityRequest request, CancellationToken cancellationToken = default);
    Task<AdminResult<SoilCompatibilityResponse>> SetSoilCompatibilityActiveStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default);

    Task<PagedResponse<SuitabilityConfigurationResponse>> ListSuitabilityConfigurationsAsync(string? configurationType, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AdminResult<SuitabilityConfigurationResponse>> GetSuitabilityConfigurationAsync(int id, CancellationToken cancellationToken = default);
    Task<AdminResult<SuitabilityConfigurationResponse>> CreateSuitabilityConfigurationAsync(CreateSuitabilityConfigurationRequest request, int administratorUserId, CancellationToken cancellationToken = default);
    Task<AdminResult<SuitabilityConfigurationResponse>> UpdateSuitabilityConfigurationAsync(int id, UpdateSuitabilityConfigurationRequest request, int administratorUserId, CancellationToken cancellationToken = default);
    Task<AdminResult<SuitabilityConfigurationResponse>> SetSuitabilityConfigurationActiveStatusAsync(int id, bool isActive, int administratorUserId, CancellationToken cancellationToken = default);

    Task<PagedResponse<AdminUserResponse>> ListUsersAsync(string? role, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AdminResult<AdminUserResponse>> GetUserAsync(int id, CancellationToken cancellationToken = default);
    Task<AdminResult<AdminUserResponse>> SetUserActiveStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default);
    Task<AdminResult<AdminUserResponse>> UpdateUserRoleAsync(int id, UpdateUserRoleRequest request, CancellationToken cancellationToken = default);

    Task<PagedResponse<AdminRecommendationSummaryResponse>> ListRecommendationsAsync(int? userId, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AdminResult<AdminRecommendationDetailResponse>> GetRecommendationAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResponse<AdminValidationResponse>> ListValidationsAsync(string? status, int? officerUserId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AdminResult<AdminValidationResponse>> GetValidationAsync(int id, CancellationToken cancellationToken = default);
}
