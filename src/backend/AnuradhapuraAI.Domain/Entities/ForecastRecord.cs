namespace AnuradhapuraAI.Domain.Entities;

public sealed class ForecastRecord
{
    public int Id { get; set; }

    public DateOnly ForecastDate { get; set; }

    public DateOnly TargetDate { get; set; }

    public decimal Rainfall { get; set; }

    public decimal Temperature { get; set; }

    public decimal Humidity { get; set; }

    public string? ModelVersion { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
