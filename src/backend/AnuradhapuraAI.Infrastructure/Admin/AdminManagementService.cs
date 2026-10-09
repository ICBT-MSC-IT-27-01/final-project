using AnuradhapuraAI.Application.Admin;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnuradhapuraAI.Infrastructure.Admin;

public sealed class AdminManagementService(
    AnuradhapuraAiDbContext dbContext,
    TimeProvider timeProvider) : IAdminManagementService
{
    private const int MaxPageSize = 100;

    public async Task<PagedResponse<CropResponse>> ListCropsAsync(bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Crops.AsNoTracking();
        if (isActive.HasValue) query = query.Where(crop => crop.IsActive == isActive.Value);

        return await ToPagedResponseAsync(
            query.OrderBy(crop => crop.Name)
                .Select(crop => new CropResponse(crop.Id, crop.Name, crop.IsActive)),
            page,
            pageSize,
            cancellationToken);
    }

    public async Task<AdminResult<CropResponse>> GetCropAsync(int id, CancellationToken cancellationToken = default)
    {
        var crop = await dbContext.Crops.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return crop is null ? NotFound<CropResponse>() : AdminResult<CropResponse>.Success(ToCropResponse(crop));
    }

    public async Task<AdminResult<CropResponse>> CreateCropAsync(CreateCropRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (await dbContext.Crops.AnyAsync(crop => crop.Name == name, cancellationToken)) return Duplicate<CropResponse>();

        var crop = new Crop { Name = name, IsActive = true };
        dbContext.Crops.Add(crop);
        await dbContext.SaveChangesAsync(cancellationToken);
        return AdminResult<CropResponse>.Success(ToCropResponse(crop));
    }

    public async Task<AdminResult<CropResponse>> UpdateCropAsync(int id, UpdateCropRequest request, CancellationToken cancellationToken = default)
    {
        var crop = await dbContext.Crops.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (crop is null) return NotFound<CropResponse>();
        var name = request.Name.Trim();
        if (await dbContext.Crops.AnyAsync(candidate => candidate.Id != id && candidate.Name == name, cancellationToken)) return Duplicate<CropResponse>();

        crop.Name = name;
        crop.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        return AdminResult<CropResponse>.Success(ToCropResponse(crop));
    }

    public async Task<AdminResult<CropResponse>> SetCropActiveStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var crop = await dbContext.Crops.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (crop is null) return NotFound<CropResponse>();
        crop.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        return AdminResult<CropResponse>.Success(ToCropResponse(crop));
    }

    public async Task<PagedResponse<CropRequirementResponse>> ListCropRequirementsAsync(int? cropId, string? variableType, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.CropEnvironmentalRequirements.AsNoTracking().Include(requirement => requirement.Crop).AsQueryable();
        if (cropId.HasValue) query = query.Where(requirement => requirement.CropId == cropId.Value);
        if (!string.IsNullOrWhiteSpace(variableType)) query = query.Where(requirement => requirement.VariableType == variableType.Trim());

        return await ToPagedResponseAsync(
            query.OrderBy(requirement => requirement.CropId)
                .ThenBy(requirement => requirement.VariableType)
                .Select(requirement => new CropRequirementResponse(
                    requirement.Id,
                    requirement.CropId,
                    requirement.Crop == null ? string.Empty : requirement.Crop.Name,
                    requirement.VariableType,
                    requirement.MinimumValue,
                    requirement.MaximumValue,
                    requirement.AcceptableMinimumValue,
                    requirement.AcceptableMaximumValue,
                    requirement.Unit,
                    requirement.TimeBasis,
                    requirement.IsCompatibleWithSevenDayForecast,
                    requirement.IsActive)),
            page,
            pageSize,
            cancellationToken);
    }

    public async Task<AdminResult<CropRequirementResponse>> GetCropRequirementAsync(int id, CancellationToken cancellationToken = default)
    {
        var requirement = await dbContext.CropEnvironmentalRequirements.AsNoTracking().Include(candidate => candidate.Crop).SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return requirement is null ? NotFound<CropRequirementResponse>() : AdminResult<CropRequirementResponse>.Success(ToRequirementResponse(requirement));
    }

    public async Task<AdminResult<CropRequirementResponse>> CreateCropRequirementAsync(CreateCropRequirementRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateRequirementAsync(request, excludingId: null, cancellationToken);
        if (validation is not null) return AdminResult<CropRequirementResponse>.Failure(validation);

        var requirement = new CropEnvironmentalRequirement
        {
            CropId = request.CropId,
            VariableType = request.VariableType.Trim(),
            MinimumValue = request.MinimumValue,
            MaximumValue = request.MaximumValue,
            AcceptableMinimumValue = request.AcceptableMinimumValue,
            AcceptableMaximumValue = request.AcceptableMaximumValue,
            Unit = request.Unit.Trim(),
            TimeBasis = string.IsNullOrWhiteSpace(request.TimeBasis) ? null : request.TimeBasis.Trim(),
            IsCompatibleWithSevenDayForecast = request.IsCompatibleWithSevenDayForecast,
            IsActive = true
        };
        dbContext.CropEnvironmentalRequirements.Add(requirement);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(requirement).Reference(item => item.Crop).LoadAsync(cancellationToken);
        return AdminResult<CropRequirementResponse>.Success(ToRequirementResponse(requirement));
    }

    public async Task<AdminResult<CropRequirementResponse>> UpdateCropRequirementAsync(int id, UpdateCropRequirementRequest request, CancellationToken cancellationToken = default)
    {
        var requirement = await dbContext.CropEnvironmentalRequirements.Include(candidate => candidate.Crop).SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (requirement is null) return NotFound<CropRequirementResponse>();
        var validation = await ValidateRequirementAsync(request, id, cancellationToken);
        if (validation is not null) return AdminResult<CropRequirementResponse>.Failure(validation);

        requirement.CropId = request.CropId;
        requirement.VariableType = request.VariableType.Trim();
        requirement.MinimumValue = request.MinimumValue;
        requirement.MaximumValue = request.MaximumValue;
        requirement.AcceptableMinimumValue = request.AcceptableMinimumValue;
        requirement.AcceptableMaximumValue = request.AcceptableMaximumValue;
        requirement.Unit = request.Unit.Trim();
        requirement.TimeBasis = string.IsNullOrWhiteSpace(request.TimeBasis) ? null : request.TimeBasis.Trim();
        requirement.IsCompatibleWithSevenDayForecast = request.IsCompatibleWithSevenDayForecast;
        requirement.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(requirement).Reference(item => item.Crop).LoadAsync(cancellationToken);
        return AdminResult<CropRequirementResponse>.Success(ToRequirementResponse(requirement));
    }

    public async Task<AdminResult<CropRequirementResponse>> SetCropRequirementActiveStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var requirement = await dbContext.CropEnvironmentalRequirements.Include(candidate => candidate.Crop).SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (requirement is null) return NotFound<CropRequirementResponse>();
        requirement.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        return AdminResult<CropRequirementResponse>.Success(ToRequirementResponse(requirement));
    }

    public async Task<PagedResponse<SoilCompatibilityResponse>> ListSoilCompatibilitiesAsync(int? cropId, string? soilType, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.SoilCompatibilities.AsNoTracking().Include(item => item.Crop).AsQueryable();
        if (cropId.HasValue) query = query.Where(item => item.CropId == cropId.Value);
        if (!string.IsNullOrWhiteSpace(soilType)) query = query.Where(item => item.SoilType == soilType.Trim());

        return await ToPagedResponseAsync(
            query.OrderBy(item => item.CropId)
                .ThenBy(item => item.SoilType)
                .Select(item => new SoilCompatibilityResponse(
                    item.Id,
                    item.CropId,
                    item.Crop == null ? string.Empty : item.Crop.Name,
                    item.SoilType,
                    item.CompatibilityScore,
                    item.IsActive)),
            page,
            pageSize,
            cancellationToken);
    }

    public async Task<AdminResult<SoilCompatibilityResponse>> GetSoilCompatibilityAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.SoilCompatibilities.AsNoTracking().Include(candidate => candidate.Crop).SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return item is null ? NotFound<SoilCompatibilityResponse>() : AdminResult<SoilCompatibilityResponse>.Success(ToSoilResponse(item));
    }

    public async Task<AdminResult<SoilCompatibilityResponse>> CreateSoilCompatibilityAsync(CreateSoilCompatibilityRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateSoilAsync(request.CropId, request.SoilType, excludingId: null, cancellationToken);
        if (validation is not null) return AdminResult<SoilCompatibilityResponse>.Failure(validation);

        var item = new SoilCompatibility { CropId = request.CropId, SoilType = request.SoilType.Trim(), CompatibilityScore = request.CompatibilityScore, IsActive = true };
        dbContext.SoilCompatibilities.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(item).Reference(entry => entry.Crop).LoadAsync(cancellationToken);
        return AdminResult<SoilCompatibilityResponse>.Success(ToSoilResponse(item));
    }

    public async Task<AdminResult<SoilCompatibilityResponse>> UpdateSoilCompatibilityAsync(int id, UpdateSoilCompatibilityRequest request, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.SoilCompatibilities.Include(candidate => candidate.Crop).SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (item is null) return NotFound<SoilCompatibilityResponse>();
        var validation = await ValidateSoilAsync(request.CropId, request.SoilType, id, cancellationToken);
        if (validation is not null) return AdminResult<SoilCompatibilityResponse>.Failure(validation);

        item.CropId = request.CropId;
        item.SoilType = request.SoilType.Trim();
        item.CompatibilityScore = request.CompatibilityScore;
        item.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(item).Reference(entry => entry.Crop).LoadAsync(cancellationToken);
        return AdminResult<SoilCompatibilityResponse>.Success(ToSoilResponse(item));
    }

    public async Task<AdminResult<SoilCompatibilityResponse>> SetSoilCompatibilityActiveStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.SoilCompatibilities.Include(candidate => candidate.Crop).SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (item is null) return NotFound<SoilCompatibilityResponse>();
        item.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        return AdminResult<SoilCompatibilityResponse>.Success(ToSoilResponse(item));
    }

    public async Task<PagedResponse<SuitabilityConfigurationResponse>> ListSuitabilityConfigurationsAsync(string? configurationType, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.SuitabilityConfigurations.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(configurationType)) query = query.Where(configuration => configuration.ConfigurationType == configurationType.Trim());
        if (isActive.HasValue) query = query.Where(configuration => configuration.IsActive == isActive.Value);

        return await ToPagedResponseAsync(
            query.OrderBy(configuration => configuration.ConfigurationType)
                .ThenBy(configuration => configuration.ConfigurationKey)
                .Select(configuration => new SuitabilityConfigurationResponse(
                    configuration.Id,
                    configuration.ConfigurationType,
                    configuration.ConfigurationKey,
                    configuration.Value,
                    configuration.IsActive,
                    configuration.UpdatedAt,
                    configuration.UpdatedByUserId)),
            page,
            pageSize,
            cancellationToken);
    }

    public async Task<AdminResult<SuitabilityConfigurationResponse>> GetSuitabilityConfigurationAsync(int id, CancellationToken cancellationToken = default)
    {
        var configuration = await dbContext.SuitabilityConfigurations.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return configuration is null ? NotFound<SuitabilityConfigurationResponse>() : AdminResult<SuitabilityConfigurationResponse>.Success(ToConfigurationResponse(configuration));
    }

    public async Task<AdminResult<SuitabilityConfigurationResponse>> CreateSuitabilityConfigurationAsync(CreateSuitabilityConfigurationRequest request, int administratorUserId, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateConfigurationAsync(request.ConfigurationType, request.ConfigurationKey, excludingId: null, cancellationToken);
        if (validation is not null) return AdminResult<SuitabilityConfigurationResponse>.Failure(validation);

        var configuration = new SuitabilityConfiguration
        {
            ConfigurationType = request.ConfigurationType.Trim(),
            ConfigurationKey = request.ConfigurationKey.Trim(),
            Value = request.Value,
            IsActive = true,
            UpdatedAt = timeProvider.GetUtcNow(),
            UpdatedByUserId = administratorUserId
        };
        dbContext.SuitabilityConfigurations.Add(configuration);
        await dbContext.SaveChangesAsync(cancellationToken);
        return AdminResult<SuitabilityConfigurationResponse>.Success(ToConfigurationResponse(configuration));
    }

    public async Task<AdminResult<SuitabilityConfigurationResponse>> UpdateSuitabilityConfigurationAsync(int id, UpdateSuitabilityConfigurationRequest request, int administratorUserId, CancellationToken cancellationToken = default)
    {
        var configuration = await dbContext.SuitabilityConfigurations.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (configuration is null) return NotFound<SuitabilityConfigurationResponse>();
        var validation = await ValidateConfigurationAsync(request.ConfigurationType, request.ConfigurationKey, id, cancellationToken);
        if (validation is not null) return AdminResult<SuitabilityConfigurationResponse>.Failure(validation);

        configuration.ConfigurationType = request.ConfigurationType.Trim();
        configuration.ConfigurationKey = request.ConfigurationKey.Trim();
        configuration.Value = request.Value;
        configuration.IsActive = request.IsActive;
        configuration.UpdatedAt = timeProvider.GetUtcNow();
        configuration.UpdatedByUserId = administratorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return AdminResult<SuitabilityConfigurationResponse>.Success(ToConfigurationResponse(configuration));
    }

    public async Task<AdminResult<SuitabilityConfigurationResponse>> SetSuitabilityConfigurationActiveStatusAsync(int id, bool isActive, int administratorUserId, CancellationToken cancellationToken = default)
    {
        var configuration = await dbContext.SuitabilityConfigurations.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (configuration is null) return NotFound<SuitabilityConfigurationResponse>();
        configuration.IsActive = isActive;
        configuration.UpdatedAt = timeProvider.GetUtcNow();
        configuration.UpdatedByUserId = administratorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return AdminResult<SuitabilityConfigurationResponse>.Success(ToConfigurationResponse(configuration));
    }

    public async Task<PagedResponse<AdminUserResponse>> ListUsersAsync(string? role, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Users.AsNoTracking().Include(user => user.Role).AsQueryable();
        if (!string.IsNullOrWhiteSpace(role)) query = query.Where(user => user.Role != null && user.Role.Name == role.Trim());
        if (isActive.HasValue) query = query.Where(user => user.IsActive == isActive.Value);

        return await ToPagedResponseAsync(
            query.OrderBy(user => user.Email)
                .Select(user => new AdminUserResponse(
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Role == null ? string.Empty : user.Role.Name,
                    user.IsActive,
                    user.CreatedAt)),
            page,
            pageSize,
            cancellationToken);
    }

    public async Task<AdminResult<AdminUserResponse>> GetUserAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.AsNoTracking().Include(candidate => candidate.Role).SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return user is null ? NotFound<AdminUserResponse>() : AdminResult<AdminUserResponse>.Success(ToUserResponse(user));
    }

    public async Task<AdminResult<AdminUserResponse>> SetUserActiveStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.Include(candidate => candidate.Role).SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (user is null) return NotFound<AdminUserResponse>();
        user.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        return AdminResult<AdminUserResponse>.Success(ToUserResponse(user));
    }

    public async Task<AdminResult<AdminUserResponse>> UpdateUserRoleAsync(int id, UpdateUserRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (!ApprovedRoleNames.AuthenticatedRoles.Contains(request.Role)) return AdminResult<AdminUserResponse>.Failure(AdminErrorCodes.InvalidRole);
        var user = await dbContext.Users.Include(candidate => candidate.Role).SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (user is null) return NotFound<AdminUserResponse>();
        var role = await dbContext.UserRoles.SingleOrDefaultAsync(candidate => candidate.Name == request.Role, cancellationToken);
        if (role is null) return AdminResult<AdminUserResponse>.Failure(AdminErrorCodes.InvalidRole);
        user.RoleId = role.Id;
        user.Role = role;
        await dbContext.SaveChangesAsync(cancellationToken);
        return AdminResult<AdminUserResponse>.Success(ToUserResponse(user));
    }

    public async Task<PagedResponse<AdminRecommendationSummaryResponse>> ListRecommendationsAsync(int? userId, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Recommendations.AsNoTracking().Include(recommendation => recommendation.User).Include(recommendation => recommendation.RecommendationCrops).AsQueryable();
        if (userId.HasValue) query = query.Where(recommendation => recommendation.UserId == userId.Value);
        if (from.HasValue) query = query.Where(recommendation => recommendation.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(recommendation => recommendation.CreatedAt <= to.Value);

        return await ToPagedResponseAsync(query.OrderByDescending(recommendation => recommendation.CreatedAt).Select(recommendation => new AdminRecommendationSummaryResponse(recommendation.Id, recommendation.UserId, recommendation.User == null ? null : recommendation.User.Email, recommendation.ForecastRunId, recommendation.SoilType, recommendation.CreatedAt, recommendation.RecommendationCrops.Count)), page, pageSize, cancellationToken);
    }

    public async Task<AdminResult<AdminRecommendationDetailResponse>> GetRecommendationAsync(int id, CancellationToken cancellationToken = default)
    {
        var recommendation = await dbContext.Recommendations.AsNoTracking()
            .Include(candidate => candidate.User)
            .Include(candidate => candidate.RecommendationCrops).ThenInclude(result => result.Crop)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (recommendation is null) return NotFound<AdminRecommendationDetailResponse>();
        return AdminResult<AdminRecommendationDetailResponse>.Success(new AdminRecommendationDetailResponse(recommendation.Id, recommendation.UserId, recommendation.User?.Email, recommendation.ForecastRunId, recommendation.SoilType, recommendation.EvidenceSnapshotJson, recommendation.CreatedAt, recommendation.RecommendationCrops.OrderBy(result => result.Rank ?? int.MaxValue).Select(ToRecommendationCropResponse).ToList()));
    }

    public async Task<PagedResponse<AdminValidationResponse>> ListValidationsAsync(string? status, int? officerUserId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.RecommendationValidations.AsNoTracking().Include(validation => validation.OfficerUser).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(validation => validation.Status == status.Trim());
        if (officerUserId.HasValue) query = query.Where(validation => validation.OfficerUserId == officerUserId.Value);
        return await ToPagedResponseAsync(
            query.OrderByDescending(validation => validation.CreatedAt)
                .Select(validation => new AdminValidationResponse(
                    validation.Id,
                    validation.RecommendationId,
                    validation.OfficerUserId,
                    validation.OfficerUser == null ? string.Empty : validation.OfficerUser.Email,
                    validation.Status,
                    validation.Comment,
                    validation.CreatedAt)),
            page,
            pageSize,
            cancellationToken);
    }

    public async Task<AdminResult<AdminValidationResponse>> GetValidationAsync(int id, CancellationToken cancellationToken = default)
    {
        var validation = await dbContext.RecommendationValidations.AsNoTracking().Include(candidate => candidate.OfficerUser).SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return validation is null ? NotFound<AdminValidationResponse>() : AdminResult<AdminValidationResponse>.Success(ToValidationResponse(validation));
    }

    private async Task<string?> ValidateRequirementAsync(CreateCropRequirementRequest request, int? excludingId, CancellationToken cancellationToken)
    {
        var variableType = request.VariableType.Trim();
        var timeBasis = request.TimeBasis?.Trim();
        if (!await dbContext.Crops.AnyAsync(crop => crop.Id == request.CropId, cancellationToken)) return AdminErrorCodes.InvalidReference;
        if (!ApprovedVariableTypes.All.Contains(variableType)) return AdminErrorCodes.InvalidVariableType;
        if (request.MinimumValue > request.MaximumValue) return AdminErrorCodes.InvalidRange;
        if (request.AcceptableMinimumValue.HasValue &&
            request.AcceptableMaximumValue.HasValue &&
            (request.AcceptableMinimumValue.Value > request.MinimumValue || request.MaximumValue > request.AcceptableMaximumValue!.Value))
        {
            return AdminErrorCodes.InvalidRange;
        }

        if (!string.IsNullOrWhiteSpace(timeBasis) && !ApprovedTimeBases.All.Contains(timeBasis)) return AdminErrorCodes.InvalidTimeBasis;
        if (await dbContext.CropEnvironmentalRequirements.AnyAsync(item => item.Id != excludingId && item.CropId == request.CropId && item.VariableType == variableType, cancellationToken)) return AdminErrorCodes.Duplicate;
        return null;
    }

    private async Task<string?> ValidateSoilAsync(int cropId, string soilType, int? excludingId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Crops.AnyAsync(crop => crop.Id == cropId, cancellationToken)) return AdminErrorCodes.InvalidReference;
        if (await dbContext.SoilCompatibilities.AnyAsync(item => item.Id != excludingId && item.CropId == cropId && item.SoilType == soilType.Trim(), cancellationToken)) return AdminErrorCodes.Duplicate;
        return null;
    }

    private async Task<string?> ValidateConfigurationAsync(string configurationType, string configurationKey, int? excludingId, CancellationToken cancellationToken)
    {
        var type = configurationType.Trim();
        var key = configurationKey.Trim();
        if (!ApprovedSuitabilityConfiguration.Types.All.Contains(type)) return AdminErrorCodes.InvalidConfigurationType;
        if (!ApprovedSuitabilityConfiguration.Keys.All.Contains(key)) return AdminErrorCodes.InvalidConfigurationKey;
        if (await dbContext.SuitabilityConfigurations.AnyAsync(item => item.Id != excludingId && item.ConfigurationType == type && item.ConfigurationKey == key, cancellationToken)) return AdminErrorCodes.Duplicate;
        return null;
    }

    private static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResponse<T>(items, page, pageSize, totalCount);
    }

    private static CropResponse ToCropResponse(Crop crop) => new(crop.Id, crop.Name, crop.IsActive);
    private static CropRequirementResponse ToRequirementResponse(CropEnvironmentalRequirement item) => new(item.Id, item.CropId, item.Crop?.Name ?? string.Empty, item.VariableType, item.MinimumValue, item.MaximumValue, item.AcceptableMinimumValue, item.AcceptableMaximumValue, item.Unit, item.TimeBasis, item.IsCompatibleWithSevenDayForecast, item.IsActive);
    private static SoilCompatibilityResponse ToSoilResponse(SoilCompatibility item) => new(item.Id, item.CropId, item.Crop?.Name ?? string.Empty, item.SoilType, item.CompatibilityScore, item.IsActive);
    private static SuitabilityConfigurationResponse ToConfigurationResponse(SuitabilityConfiguration item) => new(item.Id, item.ConfigurationType, item.ConfigurationKey, item.Value, item.IsActive, item.UpdatedAt, item.UpdatedByUserId);
    private static AdminUserResponse ToUserResponse(User user) => new(user.Id, user.Name, user.Email, user.Role?.Name ?? string.Empty, user.IsActive, user.CreatedAt);
    private static AdminRecommendationCropResponse ToRecommendationCropResponse(RecommendationCrop item) => new(item.Id, item.CropId, item.Crop?.Name ?? string.Empty, item.RainfallScore, item.TemperatureScore, item.HumidityScore, item.SoilScore, item.OverallScore, item.SuitabilityCategory, item.Explanation, item.Rank, item.EvaluationStatus, item.EvidenceSnapshotJson);
    private static AdminValidationResponse ToValidationResponse(RecommendationValidation item) => new(item.Id, item.RecommendationId, item.OfficerUserId, item.OfficerUser?.Email ?? string.Empty, item.Status, item.Comment, item.CreatedAt);

    private static AdminResult<T> NotFound<T>() => AdminResult<T>.Failure(AdminErrorCodes.NotFound);
    private static AdminResult<T> Duplicate<T>() => AdminResult<T>.Failure(AdminErrorCodes.Duplicate);
}
