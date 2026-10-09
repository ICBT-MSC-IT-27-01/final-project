namespace AnuradhapuraAI.Domain.Entities;

public sealed class Recommendation
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public Guid ForecastRunId { get; set; }

    public string? SoilType { get; set; }

    public string EvidenceSnapshotJson { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public User? User { get; set; }

    public ICollection<RecommendationCrop> RecommendationCrops { get; set; } = [];

    public ICollection<RecommendationValidation> RecommendationValidations { get; set; } = [];
}
