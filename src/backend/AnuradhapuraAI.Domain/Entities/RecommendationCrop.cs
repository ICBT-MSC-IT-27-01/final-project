namespace AnuradhapuraAI.Domain.Entities;

public sealed class RecommendationCrop
{
    public int Id { get; set; }

    public int RecommendationId { get; set; }

    public int CropId { get; set; }

    public decimal RainfallScore { get; set; }

    public decimal TemperatureScore { get; set; }

    public decimal HumidityScore { get; set; }

    public decimal SoilScore { get; set; }

    public decimal OverallScore { get; set; }

    public string SuitabilityCategory { get; set; } = string.Empty;

    public string Explanation { get; set; } = string.Empty;

    public int Rank { get; set; }

    public Recommendation? Recommendation { get; set; }

    public Crop? Crop { get; set; }
}
