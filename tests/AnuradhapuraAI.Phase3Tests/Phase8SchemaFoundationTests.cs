using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class Phase8SchemaFoundationTests
{
    [Fact]
    public async Task RecommendationCrop_PersistsNullableScoresRankAndEvidenceSnapshot()
    {
        var options = new DbContextOptionsBuilder<AnuradhapuraAiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var dbContext = new AnuradhapuraAiDbContext(options);
        var recommendation = new Recommendation
        {
            ForecastRunId = Guid.NewGuid(),
            SoilType = null,
            EvidenceSnapshotJson = """{"schemaVersion":1,"source":"synthetic-test"}""",
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.Recommendations.Add(recommendation);
        await dbContext.SaveChangesAsync();

        dbContext.RecommendationCrops.Add(new RecommendationCrop
        {
            RecommendationId = recommendation.Id,
            CropId = 1,
            RainfallScore = null,
            TemperatureScore = null,
            HumidityScore = null,
            SoilScore = null,
            OverallScore = null,
            SuitabilityCategory = null,
            Explanation = "Synthetic insufficient evidence.",
            Rank = null,
            EvaluationStatus = ApprovedRecommendationEvaluationStatuses.InsufficientEvidence,
            EvidenceSnapshotJson = """{"schemaVersion":1,"status":"InsufficientEvidence"}"""
        });
        await dbContext.SaveChangesAsync();

        var crop = await dbContext.RecommendationCrops.SingleAsync();
        Assert.Null(crop.RainfallScore);
        Assert.Null(crop.TemperatureScore);
        Assert.Null(crop.HumidityScore);
        Assert.Null(crop.SoilScore);
        Assert.Null(crop.OverallScore);
        Assert.Null(crop.SuitabilityCategory);
        Assert.Null(crop.Rank);
        Assert.Equal(ApprovedRecommendationEvaluationStatuses.InsufficientEvidence, crop.EvaluationStatus);
        Assert.Contains("schemaVersion", crop.EvidenceSnapshotJson);
    }

    [Fact]
    public void Model_DefinesApprovedPhase8IndexesAndNullability()
    {
        var options = new DbContextOptionsBuilder<AnuradhapuraAiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        using var dbContext = new AnuradhapuraAiDbContext(options);

        var forecast = dbContext.Model.FindEntityType(typeof(ForecastRecord))!;
        var forecastRunTargetIndex = forecast.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(ForecastRecord.ForecastRunId), nameof(ForecastRecord.TargetDate)]));
        Assert.True(forecastRunTargetIndex.IsUnique);

        var recommendationCrop = dbContext.Model.FindEntityType(typeof(RecommendationCrop))!;
        Assert.True(recommendationCrop.FindProperty(nameof(RecommendationCrop.RainfallScore))!.IsNullable);
        Assert.True(recommendationCrop.FindProperty(nameof(RecommendationCrop.TemperatureScore))!.IsNullable);
        Assert.True(recommendationCrop.FindProperty(nameof(RecommendationCrop.HumidityScore))!.IsNullable);
        Assert.True(recommendationCrop.FindProperty(nameof(RecommendationCrop.SoilScore))!.IsNullable);
        Assert.True(recommendationCrop.FindProperty(nameof(RecommendationCrop.OverallScore))!.IsNullable);
        Assert.True(recommendationCrop.FindProperty(nameof(RecommendationCrop.SuitabilityCategory))!.IsNullable);
        Assert.True(recommendationCrop.FindProperty(nameof(RecommendationCrop.Rank))!.IsNullable);

        var rankIndex = recommendationCrop.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(RecommendationCrop.RecommendationId), nameof(RecommendationCrop.Rank)]));
        Assert.True(rankIndex.IsUnique);
        Assert.Equal("[Rank] IS NOT NULL", rankIndex.GetFilter());

        var cropIndex = recommendationCrop.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(RecommendationCrop.RecommendationId), nameof(RecommendationCrop.CropId)]));
        Assert.True(cropIndex.IsUnique);
    }
}
