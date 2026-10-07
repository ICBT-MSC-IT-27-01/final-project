namespace AnuradhapuraAI.Domain.Entities;

public sealed class SoilCompatibility
{
    public int Id { get; set; }

    public int CropId { get; set; }

    public string SoilType { get; set; } = string.Empty;

    public decimal CompatibilityScore { get; set; }

    public bool IsActive { get; set; }

    public Crop? Crop { get; set; }
}
