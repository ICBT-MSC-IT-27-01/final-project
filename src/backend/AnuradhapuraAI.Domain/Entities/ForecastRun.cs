namespace AnuradhapuraAI.Domain.Entities;

public sealed class ForecastRun
{
    public Guid ForecastRunId { get; set; }

    public string RunPurpose { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateOnly? ForecastOriginDate { get; set; }

    public DateOnly? TargetStartDate { get; set; }

    public DateOnly? TargetEndDate { get; set; }

    public string? ModelVersion { get; set; }

    public string? SourceProvider { get; set; }

    public string? SourceProduct { get; set; }

    public decimal? RequestedLatitude { get; set; }

    public decimal? RequestedLongitude { get; set; }

    public string? Timezone { get; set; }

    public DateOnly? ObservationStartDate { get; set; }

    public DateOnly? ObservationEndDate { get; set; }

    public DateTimeOffset? SourceRetrievedAt { get; set; }

    public string? DataQualityStatus { get; set; }

    public ICollection<ForecastRecord> ForecastRecords { get; set; } = [];
}
