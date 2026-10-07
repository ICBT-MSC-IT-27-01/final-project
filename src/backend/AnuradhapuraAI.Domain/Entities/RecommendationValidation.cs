namespace AnuradhapuraAI.Domain.Entities;

public sealed class RecommendationValidation
{
    public int Id { get; set; }

    public int RecommendationId { get; set; }

    public int OfficerUserId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Comment { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Recommendation? Recommendation { get; set; }

    public User? OfficerUser { get; set; }
}
