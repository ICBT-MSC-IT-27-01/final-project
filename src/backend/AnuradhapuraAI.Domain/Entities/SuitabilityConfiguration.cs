namespace AnuradhapuraAI.Domain.Entities;

public sealed class SuitabilityConfiguration
{
    public int Id { get; set; }

    public string ConfigurationType { get; set; } = string.Empty;

    public string ConfigurationKey { get; set; } = string.Empty;

    public decimal Value { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public int? UpdatedByUserId { get; set; }

    public User? UpdatedByUser { get; set; }
}
