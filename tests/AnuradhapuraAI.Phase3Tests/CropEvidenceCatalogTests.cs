using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Infrastructure.Agriculture;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class CropEvidenceCatalogTests
{
    [Theory]
    [InlineData("Paddy", 35, 50, 25, 35, 70, 80)]
    [InlineData("Maize", 30, 45, 25, 32, 50, 70)]
    [InlineData("Green Gram", 20, 35, 28, 30, 60, 70)]
    [InlineData("Cowpea", 20, 35, 27, 35, 50, 70)]
    [InlineData("Groundnut", 25, 40, 24, 33, 50, 70)]
    [InlineData("Chilli", 25, 40, 20, 30, 50, 65)]
    public void Catalog_LoadsApprovedStrictSevenDayBands(
        string cropName,
        decimal rainfallMinimum,
        decimal rainfallMaximum,
        decimal temperatureMinimum,
        decimal temperatureMaximum,
        decimal humidityMinimum,
        decimal humidityMaximum)
    {
        var catalog = new CropEvidenceCatalog();

        Assert.True(catalog.TryGetCrop(cropName, out var crop));
        Assert.True(crop.SupportsFullSuitabilityAssessment);
        Assert.Equal(3, crop.Requirements.Count);

        AssertStrictRequirement(crop.Requirements, ApprovedVariableTypes.Rainfall, rainfallMinimum, rainfallMaximum, "mm");
        AssertStrictRequirement(crop.Requirements, ApprovedVariableTypes.Temperature, temperatureMinimum, temperatureMaximum, "deg C");
        AssertStrictRequirement(crop.Requirements, ApprovedVariableTypes.Humidity, humidityMinimum, humidityMaximum, "%");
    }

    private static void AssertStrictRequirement(
        IReadOnlyList<CatalogRequirement> requirements,
        string factor,
        decimal minimum,
        decimal maximum,
        string unit)
    {
        var requirement = Assert.Single(requirements, item => item.Factor == factor);

        Assert.True(requirement.IsCompatibleWithSevenDayForecast);
        Assert.True(requirement.SupportsFullSuitabilityAssessment);
        Assert.Equal(minimum, requirement.OptimalMinimum);
        Assert.Equal(maximum, requirement.OptimalMaximum);
        Assert.Equal(minimum, requirement.AcceptableMinimum);
        Assert.Equal(maximum, requirement.AcceptableMaximum);
        Assert.Equal(unit, requirement.Unit);
        Assert.Equal("SevenDay", requirement.TimeBasis);
        Assert.Equal("PROJECT_OWNER_APPROVED_CONFIGURATION", requirement.EvidenceStatus);
    }
}
