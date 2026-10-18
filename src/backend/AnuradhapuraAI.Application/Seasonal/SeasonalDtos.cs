namespace AnuradhapuraAI.Application.Seasonal;

public sealed record SeasonalRangeRequest(DateOnly StartDate, DateOnly EndDate, string? SoilType = null);
public sealed record SeasonalCropRequest(string CropName, DateOnly StartDate, DateOnly EndDate, string? SoilType = null);
public sealed record SeasonalClimateMonth(int Month, string MonthName, decimal MeanTemperature, decimal MeanRainfall, decimal MeanHumidity, decimal RainfallStandardDeviation, int ObservationCount, int YearCount, int RequestedDayCount);
public sealed record CropCalendarEvidence(string GeographicApplicability, IReadOnlyList<string> DocumentedConditions, IReadOnlyList<string> SourceUrls, string ValidationStatus, IReadOnlyList<string> Limitations);
public sealed record SeasonalCultivationWindow(string Id, string Season, IReadOnlyList<int> EstablishmentMonths, IReadOnlyList<int> HarvestMonths, IReadOnlyList<int> SourcePages, string EvidenceStatus, string Interpretation, IReadOnlyList<string> Conditions, bool RequiresLocalReview);
public sealed record SeasonalCropResult(string CropName, string EvidenceStatus, string? SuitabilityCategory, string Explanation, IReadOnlyList<string> MissingFactors, CropCalendarEvidence CalendarEvidence)
{
    public IReadOnlyList<SeasonalCultivationWindow> CandidateWindows { get; init; } = [];
    public IReadOnlyList<SeasonalCultivationWindow> AlternativeWindows { get; init; } = [];
}
public sealed record SeasonalRangeResponse(DateOnly StartDate, DateOnly EndDate, string AssessmentType, string DataSource, string EvidenceStatus, IReadOnlyList<SeasonalClimateMonth> ClimateMonths, IReadOnlyList<SeasonalCropResult> Crops, IReadOnlyList<string> Limitations);
public sealed record SeasonalCropResponse(string CropName, DateOnly StartDate, DateOnly EndDate, string AssessmentType, string DataSource, string EvidenceStatus, IReadOnlyList<SeasonalClimateMonth> ClimateMonths, IReadOnlyList<string> CandidateMonths, string Explanation, IReadOnlyList<string> Limitations, CropCalendarEvidence CalendarEvidence)
{
    public IReadOnlyList<SeasonalCultivationWindow> CandidateWindows { get; init; } = [];
    public IReadOnlyList<SeasonalCultivationWindow> AlternativeWindows { get; init; } = [];
}
public interface ISeasonalClimateService
{
    Task<SeasonalRangeResponse> FindRecommendedCropsAsync(SeasonalRangeRequest request, CancellationToken cancellationToken);
    Task<SeasonalCropResponse> FindSuitablePeriodsAsync(SeasonalCropRequest request, CancellationToken cancellationToken);
}
