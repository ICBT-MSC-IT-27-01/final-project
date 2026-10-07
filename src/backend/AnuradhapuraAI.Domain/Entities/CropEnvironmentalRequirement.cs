namespace AnuradhapuraAI.Domain.Entities;

public sealed class CropEnvironmentalRequirement
{
    public int Id { get; set; }

    public int CropId { get; set; }

    public string VariableType { get; set; } = string.Empty;

    public decimal MinimumValue { get; set; }

    public decimal MaximumValue { get; set; }

    public decimal? AcceptableMinimumValue { get; set; }

    public decimal? AcceptableMaximumValue { get; set; }

    public string Unit { get; set; } = string.Empty;

    public string? TimeBasis { get; set; }

    public bool IsCompatibleWithSevenDayForecast { get; set; }

    public bool IsActive { get; set; }

    public Crop? Crop { get; set; }
}
