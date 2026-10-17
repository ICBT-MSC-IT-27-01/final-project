using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Infrastructure.Agriculture;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class CropEvidenceCatalogTests
{
    [Fact]
    public void Catalog_LoadsEveryApprovedCropWithOnlyTemperatureMarkedCompatible()
    {
        var catalog = new CropEvidenceCatalog();

        foreach (var cropName in ApprovedCropNames.All)
        {
            Assert.True(catalog.TryGetCrop(cropName, out var crop));
            Assert.False(crop.SupportsFullSuitabilityAssessment);

            var temperature = Assert.Single(crop.Requirements, requirement => requirement.Factor == ApprovedVariableTypes.Temperature);
            var rainfall = Assert.Single(crop.Requirements, requirement => requirement.Factor == ApprovedVariableTypes.Rainfall);

            Assert.True(temperature.IsCompatibleWithSevenDayForecast);
            Assert.False(temperature.SupportsFullSuitabilityAssessment);
            Assert.False(rainfall.IsCompatibleWithSevenDayForecast);
            Assert.Equal("annual rainfall", rainfall.TimeBasis);
            Assert.StartsWith("https://", crop.CalendarEvidence.SourceUrl, StringComparison.Ordinal);
            Assert.StartsWith("https://", temperature.Source.SourceUrl, StringComparison.Ordinal);
        }
    }
}
