using AnuradhapuraAI.Infrastructure.Seasonal;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class DoaCalendarMonthCatalogTests
{
    [Theory]
    [InlineData("Paddy", 1, 10, 11, 4, 5)]
    [InlineData("Maize", 5, 10, 4, 5)]
    [InlineData("Green Gram", 8, 11, 4)]
    [InlineData("Cowpea", 19, 11, 4)]
    [InlineData("Groundnut", 7, 10, 11, 4, 5)]
    [InlineData("Chilli", 15, 10, 11, 4, 5)]
    public void Catalog_LoadsVerifiedMahaAndYalaEstablishmentMonths(
        string cropName,
        int sourcePage,
        params int[] expectedMonths)
    {
        var crop = new DoaCalendarMonthCatalog().GetRequiredCrop(cropName);
        var actualMonths = crop.Patterns
            .SelectMany(pattern => pattern.SowingOrTransplantingMonths)
            .OrderBy(month => month)
            .ToArray();

        Assert.Contains(sourcePage, crop.SourcePages);
        Assert.False(crop.AgronomistValidated);
        Assert.False(crop.ProductionApproved);
        Assert.Equal(expectedMonths.OrderBy(month => month), actualMonths);
        Assert.All(crop.Patterns, pattern => Assert.True(pattern.RequiresLocalReview));
    }
}
