using System.Text.Json;
using AnuradhapuraAI.Application.Suitability;
using AnuradhapuraAI.Domain.Common;

namespace AnuradhapuraAI.Infrastructure.Agriculture;

public sealed class CropEvidenceCatalog
{
    private const string DatasetRelativePath = "docs/agriculture/crop-evidence.json";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly Lazy<IReadOnlyDictionary<string, CatalogCropEvidence>> cropEvidence;

    public CropEvidenceCatalog()
    {
        cropEvidence = new Lazy<IReadOnlyDictionary<string, CatalogCropEvidence>>(Load);
    }

    public bool TryGetCrop(string cropName, out CatalogCropEvidence evidence) =>
        cropEvidence.Value.TryGetValue(cropName, out evidence!);

    private static IReadOnlyDictionary<string, CatalogCropEvidence> Load()
    {
        var path = FindDatasetPath();
        var document = JsonSerializer.Deserialize<CropEvidenceDocument>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException("Crop evidence catalog could not be deserialized.");

        if (document.Crops.Count != ApprovedCropNames.All.Length ||
            !document.Crops.Select(crop => crop.CropName).ToHashSet(StringComparer.OrdinalIgnoreCase)
                .SetEquals(ApprovedCropNames.All))
        {
            throw new InvalidOperationException("Crop evidence catalog must contain exactly the six approved crops.");
        }

        return document.Crops.ToDictionary(crop => crop.CropName, StringComparer.OrdinalIgnoreCase);
    }

    private static string FindDatasetPath()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, DatasetRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException($"Crop evidence catalog '{DatasetRelativePath}' was not found.");
    }
}

public sealed record CatalogCropEvidence(
    string CropName,
    string AssessmentBasis,
    bool SupportsFullSuitabilityAssessment,
    CatalogEvidenceSource CalendarEvidence,
    IReadOnlyList<CatalogRequirement> Requirements)
{
    public IReadOnlyList<string> ScientificLimitations =>
    [
        "This result is a short-horizon temperature compatibility indicator, not full crop suitability.",
        "Annual rainfall requirements are retained as evidence but are not compared with seven-day rainfall forecasts.",
        "No crop-specific relative-humidity threshold applicable to the seven-day forecast was verified.",
        "No Anuradhapura-wide soil property is assumed; soil remains unavailable until a validated selected-soil compatibility is configured.",
        "FAO ECOCROP values are global ecological evidence and have not been locally agronomically validated for Anuradhapura."
    ];
}

public sealed record CatalogRequirement(
    string Factor,
    decimal OptimalMinimum,
    decimal OptimalMaximum,
    decimal AcceptableMinimum,
    decimal AcceptableMaximum,
    string Unit,
    string TimeBasis,
    string AggregationMethod,
    bool IsCompatibleWithSevenDayForecast,
    bool SupportsFullSuitabilityAssessment,
    string EvidenceStatus,
    CatalogEvidenceSource Source)
{
    public SuitabilityRangeRequirement ToSuitabilityRequirement() =>
        new(
            OptimalMinimum,
            OptimalMaximum,
            AcceptableMinimum,
            AcceptableMaximum,
            Unit,
            TimeBasis,
            IsCompatibleWithSevenDayForecast)
        {
            SupportsFullSuitabilityAssessment = SupportsFullSuitabilityAssessment,
            EvidenceStatus = EvidenceStatus,
            AggregationMethod = AggregationMethod,
            Sources = [Source.ToSuitabilitySource()],
            Limitations = IsCompatibleWithSevenDayForecast
                ? ["The configured aggregation is only a short-horizon temperature compatibility indicator."]
                : ["This requirement is not evaluated because its temporal scale is incompatible with a seven-day forecast."]
        };
}

public sealed record CatalogEvidenceSource(
    string Title,
    string SourceUrl,
    string? PublicationDetails,
    string? Location,
    string EvidenceStatus)
{
    public SuitabilityEvidenceSource ToSuitabilitySource() =>
        new(Title, SourceUrl, PublicationDetails, Location, EvidenceStatus);
}

public sealed record CropEvidenceDocument(IReadOnlyList<CatalogCropEvidence> Crops);
