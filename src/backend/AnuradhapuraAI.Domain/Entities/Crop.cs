namespace AnuradhapuraAI.Domain.Entities;

public sealed class Crop
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public ICollection<CropEnvironmentalRequirement> EnvironmentalRequirements { get; set; } = [];

    public ICollection<SoilCompatibility> SoilCompatibilities { get; set; } = [];

    public ICollection<RecommendationCrop> RecommendationCrops { get; set; } = [];
}
