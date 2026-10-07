namespace AnuradhapuraAI.Domain.Entities;

public sealed class User
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public UserRole? Role { get; set; }

    public ICollection<SuitabilityConfiguration> UpdatedSuitabilityConfigurations { get; set; } = [];

    public ICollection<Recommendation> Recommendations { get; set; } = [];

    public ICollection<RecommendationValidation> OfficerRecommendationValidations { get; set; } = [];
}
