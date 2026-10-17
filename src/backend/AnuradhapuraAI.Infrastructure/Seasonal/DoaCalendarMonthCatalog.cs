using System.Text.Json;
using AnuradhapuraAI.Application.Seasonal;
using AnuradhapuraAI.Domain.Common;

namespace AnuradhapuraAI.Infrastructure.Seasonal;

public sealed class DoaCalendarMonthCatalog
{
    private const string DatasetRelativePath = "docs/English calendar images/evidance json/DOA_SIX_CROP_CALENDAR_MONTH_EXTRACTION_REVIEW.json";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly Lazy<IReadOnlyDictionary<string, DoaCalendarCrop>> crops;

    public DoaCalendarMonthCatalog()
    {
        crops = new Lazy<IReadOnlyDictionary<string, DoaCalendarCrop>>(Load);
    }

    public DoaCalendarCrop GetRequiredCrop(string cropName) =>
        crops.Value.TryGetValue(cropName, out var crop)
            ? crop
            : throw new ArgumentException("Unsupported crop.");

    private static IReadOnlyDictionary<string, DoaCalendarCrop> Load()
    {
        var dataset = JsonSerializer.Deserialize<DoaCalendarDataset>(File.ReadAllText(FindDatasetPath()), JsonOptions)
            ?? throw new InvalidOperationException("The DoA calendar month evidence dataset could not be deserialized.");

        if (dataset.Crops.Count != ApprovedCropNames.All.Length ||
            !dataset.Crops.Select(crop => crop.CropName).ToHashSet(StringComparer.OrdinalIgnoreCase)
                .SetEquals(ApprovedCropNames.All))
        {
            throw new InvalidOperationException("The DoA calendar month evidence dataset must contain exactly the six approved crops.");
        }

        return dataset.Crops.ToDictionary(crop => crop.CropName, StringComparer.OrdinalIgnoreCase);
    }

    private static string FindDatasetPath()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, DatasetRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate)) return candidate;
        }

        throw new FileNotFoundException("The verified DoA calendar month evidence dataset is unavailable.");
    }
}

public sealed record DoaCalendarDataset(IReadOnlyList<DoaCalendarCrop> Crops);

public sealed record DoaCalendarCrop(
    string CropName,
    string Publisher,
    int PublicationYear,
    string SourceFile,
    string Precision,
    bool AgronomistValidated,
    bool ProductionApproved,
    IReadOnlyList<int> SourcePages,
    string Zone,
    string AnuradhapuraApplicability,
    IReadOnlyList<DoaCalendarPattern> Patterns,
    IReadOnlyList<string> Conditions,
    IReadOnlyList<string> Notes);

public sealed record DoaCalendarPattern(
    string Id,
    string Season,
    IReadOnlyList<int> LandPreparationMonths,
    IReadOnlyList<int> SowingOrTransplantingMonths,
    IReadOnlyList<int> HarvestMonths,
    string Interpretation,
    string EvidenceStatus,
    bool RequiresLocalReview,
    IReadOnlyList<int> SourcePages);
