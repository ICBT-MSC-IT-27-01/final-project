using System.ComponentModel.DataAnnotations;

namespace AnuradhapuraAI.Application.Admin;

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record AdminResult<T>(bool Succeeded, T? Value, string? ErrorCode)
{
    public static AdminResult<T> Success(T value) => new(true, value, null);

    public static AdminResult<T> Failure(string errorCode) => new(false, default, errorCode);
}

public static class AdminErrorCodes
{
    public const string NotFound = "NotFound";
    public const string Duplicate = "Duplicate";
    public const string InvalidRole = "InvalidRole";
    public const string InvalidVariableType = "InvalidVariableType";
    public const string InvalidConfigurationType = "InvalidConfigurationType";
    public const string InvalidConfigurationKey = "InvalidConfigurationKey";
    public const string InvalidRange = "InvalidRange";
    public const string InvalidTimeBasis = "InvalidTimeBasis";
    public const string InvalidReference = "InvalidReference";
}

public sealed record CropResponse(int Id, string Name, bool IsActive);

public sealed class CreateCropRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}

public sealed class UpdateCropRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}

public sealed class UpdateActiveStatusRequest
{
    public bool IsActive { get; set; }
}

public sealed record CropRequirementResponse(
    int Id,
    int CropId,
    string CropName,
    string VariableType,
    decimal MinimumValue,
    decimal MaximumValue,
    decimal? AcceptableMinimumValue,
    decimal? AcceptableMaximumValue,
    string Unit,
    string? TimeBasis,
    bool IsCompatibleWithSevenDayForecast,
    bool IsActive);

public class CreateCropRequirementRequest
{
    public int CropId { get; set; }

    [Required]
    [MaxLength(50)]
    public string VariableType { get; set; } = string.Empty;

    public decimal MinimumValue { get; set; }

    public decimal MaximumValue { get; set; }

    public decimal? AcceptableMinimumValue { get; set; }

    public decimal? AcceptableMaximumValue { get; set; }

    [Required]
    [MaxLength(50)]
    public string Unit { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? TimeBasis { get; set; }

    public bool IsCompatibleWithSevenDayForecast { get; set; }
}

public sealed class UpdateCropRequirementRequest : CreateCropRequirementRequest
{
    public bool IsActive { get; set; }
}

public sealed record SoilCompatibilityResponse(
    int Id,
    int CropId,
    string CropName,
    string SoilType,
    decimal CompatibilityScore,
    bool IsActive);

public class CreateSoilCompatibilityRequest
{
    public int CropId { get; set; }

    [Required]
    [MaxLength(100)]
    public string SoilType { get; set; } = string.Empty;

    public decimal CompatibilityScore { get; set; }
}

public sealed class UpdateSoilCompatibilityRequest : CreateSoilCompatibilityRequest
{
    public bool IsActive { get; set; }
}

public sealed record SuitabilityConfigurationResponse(
    int Id,
    string ConfigurationType,
    string ConfigurationKey,
    decimal Value,
    bool IsActive,
    DateTimeOffset UpdatedAt,
    int? UpdatedByUserId);

public class CreateSuitabilityConfigurationRequest
{
    [Required]
    [MaxLength(50)]
    public string ConfigurationType { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ConfigurationKey { get; set; } = string.Empty;

    public decimal Value { get; set; }
}

public sealed class UpdateSuitabilityConfigurationRequest : CreateSuitabilityConfigurationRequest
{
    public bool IsActive { get; set; }
}

public sealed record AdminUserResponse(
    int Id,
    string Name,
    string Email,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed class UpdateUserRoleRequest
{
    [Required]
    [MaxLength(100)]
    public string Role { get; set; } = string.Empty;
}

public sealed record AdminRecommendationCropResponse(
    int Id,
    int CropId,
    string CropName,
    decimal RainfallScore,
    decimal TemperatureScore,
    decimal HumidityScore,
    decimal SoilScore,
    decimal OverallScore,
    string SuitabilityCategory,
    string Explanation,
    int Rank);

public sealed record AdminRecommendationSummaryResponse(
    int Id,
    int? UserId,
    string? UserEmail,
    DateTimeOffset CreatedAt,
    int CropResultCount);

public sealed record AdminRecommendationDetailResponse(
    int Id,
    int? UserId,
    string? UserEmail,
    DateTimeOffset CreatedAt,
    IReadOnlyList<AdminRecommendationCropResponse> Crops);

public sealed record AdminValidationResponse(
    int Id,
    int RecommendationId,
    int OfficerUserId,
    string OfficerEmail,
    string Status,
    string? Comment,
    DateTimeOffset CreatedAt);
