using System.Globalization;
using AnuradhapuraAI.Application.Seasonal;

namespace AnuradhapuraAI.Infrastructure.Seasonal;

public sealed class SeasonalClimateService(
    TimeProvider timeProvider,
    Microsoft.Extensions.Configuration.IConfiguration configuration,
    DoaCalendarMonthCatalog calendarCatalog) : ISeasonalClimateService
{
    private static readonly string[] Crops = ["Paddy", "Maize", "Green Gram", "Cowpea", "Groundnut", "Chilli"];
    private const string CalendarIndexUrl = "https://doa.gov.lk/naicc-publications-crop-calender/";
    private const string AnuradhapuraOfficeUrl = "https://doa.gov.lk/etc-inter-provincial-anuradhapura/";
    private static readonly IReadOnlyDictionary<string, string> IndividualCalendarUrls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Paddy"] = "https://drive.google.com/file/d/1yjqSoFI9T87uaEh_TbD-cN_wv2j4N4AR/view?usp=sharing",
        ["Cowpea"] = "https://drive.google.com/file/d/1nn4gxg5JHNhfEdlpsDpgpTvoBcqW6PSe/view?usp=sharing",
        ["Chilli"] = "https://drive.google.com/file/d/1AAEmT-y4nvl-m3kz84fzcO29mVSEczEd/view",
        ["Green Gram"] = "https://drive.google.com/file/d/1U6x48RpUDpnhk_eJt-J-XJVWV0BJM3qU/view?usp=sharing",
        ["Groundnut"] = "https://drive.google.com/file/d/1SOOJatHEThuFU2__H98PPb4omCm3vCTZ/view?usp=sharing",
        ["Maize"] = "https://drive.google.com/file/d/1_jT8vdyT5DSZzhV_19eLZlNnbIY32HHf/view?usp=sharing"
    };
    public async Task<SeasonalRangeResponse> FindRecommendedCropsAsync(SeasonalRangeRequest request, CancellationToken cancellationToken)
    {
        Validate(request.StartDate, request.EndDate);
        var climate = await GetClimateAsync(request.StartDate, request.EndDate, cancellationToken);
        var requestedMonths = EnumerateDays(request.StartDate, request.EndDate).Select(date => date.Month).ToHashSet();
        var cropResults = Crops.Select(cropName => ToRangeCropResult(cropName, requestedMonths)).ToList();
        var hasCandidate = cropResults.Any(crop => crop.EvidenceStatus == "CALENDAR_SUPPORTED_CANDIDATE");
        return new(request.StartDate, request.EndDate, "HISTORICAL_SEASONAL_CLIMATE", "ERA5 reanalysis via repository dataset (2010–2025)", hasCandidate ? "CALENDAR_SUPPORTED_CANDIDATES" : "CLIMATE_CONTEXT_ONLY", climate, cropResults,
            ["Candidate windows are approximate calendar-month establishment windows from the DoA/AFACI source PDF, not exact planting dates.", "Historical climatology is not a future daily weather forecast; the seven-day LSTM is not used beyond its horizon.", "Calendar candidates do not establish rainfall, humidity, soil, irrigation availability, or full agronomic suitability.", "Seasonal planning support is a requested scope extension; agricultural-officer/agronomist validation remains pending."]);
    }
    public async Task<SeasonalCropResponse> FindSuitablePeriodsAsync(SeasonalCropRequest request, CancellationToken cancellationToken)
    {
        if (!Crops.Contains(request.CropName, StringComparer.OrdinalIgnoreCase)) throw new ArgumentException("Unsupported crop.");
        Validate(request.StartDate, request.EndDate);
        var climate = await GetClimateAsync(request.StartDate, request.EndDate, cancellationToken);
        var crop = calendarCatalog.GetRequiredCrop(request.CropName);
        var requestedMonths = EnumerateDays(request.StartDate, request.EndDate).Select(date => date.Month).ToHashSet();
        var allWindows = crop.Patterns
            .Select(pattern => ToWindow(pattern, crop.Conditions))
            .ToList();
        var windows = allWindows
            .Where(window => window.EstablishmentMonths.Any(requestedMonths.Contains))
            .ToList();
        var candidateMonths = windows
            .SelectMany(window => window.EstablishmentMonths)
            .Distinct()
            .OrderBy(month => month)
            .Select(MonthName)
            .ToList();
        var hasCandidate = windows.Count > 0;
        return new(crop.CropName, request.StartDate, request.EndDate, "HISTORICAL_SEASONAL_CLIMATE", "ERA5 reanalysis via repository dataset (2010–2025)", hasCandidate ? "CALENDAR_SUPPORTED_CANDIDATE" : "CLIMATE_CONTEXT_ONLY", climate, candidateMonths,
            hasCandidate
                ? $"The requested period intersects {string.Join(" and ", windows.Select(window => window.Season).Distinct())} approximate crop-establishment month(s) documented in the supplied DoA calendar PDF. This is a conditional calendar candidate, not a weather-suitability recommendation."
                : "The requested period does not intersect a verified approximate establishment month in the supplied DoA calendar PDF. Historical climate is shown as context only.",
            ["Establishment months are month-approximate and require local review; no exact week or sowing date is inferred.", "Historical climate is not a forecast.", "Irrigation/rainfed conditions, soil and rainfall sufficiency are not established by the calendar alone."],
            GetCalendarEvidence(crop))
        {
            CandidateWindows = windows,
            AlternativeWindows = allWindows
                .Where(window => !windows.Any(candidate => candidate.Id == window.Id))
                .ToList()
        };
    }
    private void Validate(DateOnly start, DateOnly end)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        if (end < start) throw new ArgumentException("End date must be on or after start date.");
        if (start < today) throw new ArgumentException("Historical date ranges are not supported for forward seasonal planning.");
        if (end.DayNumber - start.DayNumber + 1 > 366) throw new ArgumentException("The maximum supported range is 366 days.");
    }
    private async Task<IReadOnlyList<SeasonalClimateMonth>> GetClimateAsync(DateOnly start, DateOnly end, CancellationToken ct)
    {
        var path = FindDataset(configuration["SeasonalClimate:DatasetPath"]);
        var rows = new List<(DateOnly Date, decimal Temperature, decimal Rainfall, decimal Humidity)>();
        foreach (var line in await File.ReadAllLinesAsync(path, ct))
        {
            var p = line.Split(','); if (p.Length != 4 || p[0] == "Date") continue;
            if (DateOnly.TryParse(p[0], CultureInfo.InvariantCulture, out var d) && decimal.TryParse(p[1], CultureInfo.InvariantCulture, out var t) && decimal.TryParse(p[2], CultureInfo.InvariantCulture, out var r) && decimal.TryParse(p[3], CultureInfo.InvariantCulture, out var h)) rows.Add((d,t,r,h));
        }
        var requestedDaysByMonth = EnumerateDays(start, end)
            .GroupBy(date => date.Month)
            .ToDictionary(group => group.Key, group => group.Select(date => date.Day).ToHashSet());

        return requestedDaysByMonth.OrderBy(item => item.Key).Select(item =>
        {
            var values = rows.Where(row => row.Date.Month == item.Key && item.Value.Contains(row.Date.Day)).ToList();
            if (values.Count == 0)
            {
                throw new FileNotFoundException("The ERA5 dataset did not contain observations for the requested calendar period.");
            }

            var rainByYear = values.GroupBy(row => row.Date.Year).Select(group => group.Sum(row => row.Rainfall)).ToList();
            var meanRain = rainByYear.Average();
            var standardDeviation = (decimal)Math.Sqrt(rainByYear.Select(value => Math.Pow((double)(value - meanRain), 2)).Average());
            return new SeasonalClimateMonth(item.Key, CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(item.Key), decimal.Round(values.Average(row => row.Temperature), 2), decimal.Round(meanRain, 2), decimal.Round(values.Average(row => row.Humidity), 2), decimal.Round(standardDeviation, 2), values.Count, rainByYear.Count, item.Value.Count);
        }).ToList();
    }

    private static IEnumerable<DateOnly> EnumerateDays(DateOnly start, DateOnly end)
    {
        for (var date = start; date <= end; date = date.AddDays(1)) yield return date;
    }

    private SeasonalCropResult ToRangeCropResult(string cropName, IReadOnlySet<int> requestedMonths)
    {
        var crop = calendarCatalog.GetRequiredCrop(cropName);
        var allWindows = crop.Patterns
            .Select(pattern => ToWindow(pattern, crop.Conditions))
            .ToList();
        var windows = allWindows
            .Where(window => window.EstablishmentMonths.Any(requestedMonths.Contains))
            .ToList();
        var hasCandidate = windows.Count > 0;
        return new SeasonalCropResult(
            crop.CropName,
            hasCandidate ? "CALENDAR_SUPPORTED_CANDIDATE" : "CLIMATE_CONTEXT_ONLY",
            null,
            hasCandidate
                ? $"The requested date range intersects the documented approximate {string.Join(" and ", windows.Select(window => window.Season).Distinct())} establishment month(s). This is a conditional calendar candidate only."
                : "The requested date range does not intersect a verified approximate establishment month. Historical climate is shown as context only.",
            ["No time-compatible rainfall requirement", "No crop-specific seven-day humidity requirement", "No validated soil compatibility", "No local agronomist approval"],
            GetCalendarEvidence(crop))
        {
            CandidateWindows = windows,
            AlternativeWindows = allWindows
                .Where(window => !windows.Any(candidate => candidate.Id == window.Id))
                .ToList()
        };
    }

    private static SeasonalCultivationWindow ToWindow(DoaCalendarPattern pattern, IReadOnlyList<string> cropConditions) =>
        new(
            pattern.Id,
            pattern.Season,
            pattern.SowingOrTransplantingMonths,
            pattern.HarvestMonths,
            pattern.SourcePages,
            pattern.EvidenceStatus,
            pattern.Interpretation,
            cropConditions,
            pattern.RequiresLocalReview);

    private static string MonthName(int month) => CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);

    private static CropCalendarEvidence GetCalendarEvidence(DoaCalendarCrop crop)
    {
        var conditions = new List<string>
        {
            crop.Zone,
            crop.AnuradhapuraApplicability,
            $"Source PDF: {crop.SourceFile}; page(s) {string.Join(", ", crop.SourcePages)}"
        };
        conditions.AddRange(crop.Conditions);

        return new CropCalendarEvidence(
            crop.AnuradhapuraApplicability,
            conditions,
            [CalendarIndexUrl, IndividualCalendarUrls[crop.CropName], AnuradhapuraOfficeUrl],
            crop.AgronomistValidated || crop.ProductionApproved
                ? "VALIDATION_STATUS_REQUIRES_REVIEW"
                : "MONTH_LEVEL_CALENDAR_EXTRACTION_NOT_AGRONOMIST_VALIDATED",
            ["Windows are visually verified, month-level interpretations of the supplied original Sinhala DoA/AFACI PDF; exact weeks and field-ready dates are not asserted.", "The calendar does not establish rainfall sufficiency, relative humidity, soil compatibility, irrigation availability, or scientific weather suitability."]);
    }
    private static string FindDataset(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath)) return configuredPath;
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null) { var candidate = Path.Combine(current.FullName, "docs", "anuradhapura_weather_2010_2025_era5.csv"); if (File.Exists(candidate)) return candidate; current = current.Parent; }
        throw new FileNotFoundException("The required ERA5 historical dataset is unavailable.");
    }
}
