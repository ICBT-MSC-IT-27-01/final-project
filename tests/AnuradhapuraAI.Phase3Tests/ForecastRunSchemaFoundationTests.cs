using System.Reflection;
using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class ForecastRunSchemaFoundationTests
{
    [Fact]
    public void ApprovedForecastRunPurposes_DefineExactSafeValues()
    {
        Assert.Equal(
            ["Operational", "HistoricalEvaluation", "LegacyUnknown"],
            ApprovedForecastRunPurposes.All);
        Assert.NotEqual(ApprovedForecastRunPurposes.Operational, ApprovedForecastRunPurposes.LegacyUnknown);
        Assert.NotEqual(ApprovedForecastRunPurposes.Operational, ApprovedForecastRunPurposes.HistoricalEvaluation);
    }

    [Fact]
    public void ForecastRun_AllowsNullableUnknownProvenance()
    {
        var run = new ForecastRun
        {
            ForecastRunId = Guid.NewGuid(),
            RunPurpose = ApprovedForecastRunPurposes.LegacyUnknown,
            CreatedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z")
        };

        Assert.Null(run.SourceProvider);
        Assert.Null(run.SourceProduct);
        Assert.Null(run.RequestedLatitude);
        Assert.Null(run.RequestedLongitude);
        Assert.Null(run.Timezone);
        Assert.Null(run.ObservationStartDate);
        Assert.Null(run.ObservationEndDate);
        Assert.Null(run.SourceRetrievedAt);
        Assert.Null(run.DataQualityStatus);
    }

    [Fact]
    public void ForecastRun_ModelMetadata_DefinesKeyPurposeConstraintsAndNullableProvenance()
    {
        using var dbContext = CreateMetadataContext();
        var forecastRun = dbContext.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ForecastRun))!;

        Assert.Equal(nameof(ForecastRun.ForecastRunId), Assert.Single(forecastRun.FindPrimaryKey()!.Properties).Name);
        Assert.False(forecastRun.FindProperty(nameof(ForecastRun.RunPurpose))!.IsNullable);
        Assert.Equal(50, forecastRun.FindProperty(nameof(ForecastRun.RunPurpose))!.GetMaxLength());
        Assert.False(forecastRun.FindProperty(nameof(ForecastRun.CreatedAt))!.IsNullable);

        Assert.True(forecastRun.FindProperty(nameof(ForecastRun.SourceProvider))!.IsNullable);
        Assert.True(forecastRun.FindProperty(nameof(ForecastRun.SourceProduct))!.IsNullable);
        Assert.True(forecastRun.FindProperty(nameof(ForecastRun.RequestedLatitude))!.IsNullable);
        Assert.True(forecastRun.FindProperty(nameof(ForecastRun.RequestedLongitude))!.IsNullable);
        Assert.True(forecastRun.FindProperty(nameof(ForecastRun.Timezone))!.IsNullable);
        Assert.True(forecastRun.FindProperty(nameof(ForecastRun.ObservationStartDate))!.IsNullable);
        Assert.True(forecastRun.FindProperty(nameof(ForecastRun.ObservationEndDate))!.IsNullable);
        Assert.True(forecastRun.FindProperty(nameof(ForecastRun.SourceRetrievedAt))!.IsNullable);
        Assert.True(forecastRun.FindProperty(nameof(ForecastRun.DataQualityStatus))!.IsNullable);

        Assert.Null(forecastRun.FindProperty("OperationalEligibilityStatus"));
        Assert.Contains(forecastRun.GetCheckConstraints(), constraint =>
            constraint.Name == "CK_ForecastRun_RunPurpose" &&
            constraint.Sql!.Contains("Operational", StringComparison.Ordinal) &&
            constraint.Sql.Contains("HistoricalEvaluation", StringComparison.Ordinal) &&
            constraint.Sql.Contains("LegacyUnknown", StringComparison.Ordinal));
    }

    [Fact]
    public void ForecastRecord_ModelMetadata_DefinesRequiredForecastRunRelationshipAndPreservesIndexes()
    {
        using var dbContext = CreateMetadataContext();
        var forecastRecord = dbContext.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ForecastRecord))!;

        var foreignKey = Assert.Single(forecastRecord.GetForeignKeys(), key =>
            key.PrincipalEntityType.ClrType == typeof(ForecastRun));
        Assert.Equal(nameof(ForecastRecord.ForecastRunId), Assert.Single(foreignKey.Properties).Name);
        Assert.Equal(nameof(ForecastRun.ForecastRunId), Assert.Single(foreignKey.PrincipalKey.Properties).Name);
        Assert.True(foreignKey.IsRequired);
        Assert.True(foreignKey.DeleteBehavior is DeleteBehavior.Restrict or DeleteBehavior.NoAction);

        var forecastRunTargetIndex = forecastRecord.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(ForecastRecord.ForecastRunId), nameof(ForecastRecord.TargetDate)]));
        Assert.True(forecastRunTargetIndex.IsUnique);

        Assert.Contains(forecastRecord.GetCheckConstraints(), constraint =>
            constraint.Name == "CK_ForecastRecord_ForecastRunId_NotEmpty");
    }

    [Fact]
    public void ExistingContracts_RemainUnchangedForForecastRequestAndRecommendationEvidence()
    {
        using var dbContext = CreateMetadataContext();
        var constructor = typeof(CreateForecastRequest).GetConstructors().Single();
        var parameter = Assert.Single(constructor.GetParameters());
        Assert.Equal("Observations", parameter.Name);
        Assert.Equal(typeof(IReadOnlyList<ForecastObservationRequest>), parameter.ParameterType);

        Assert.Null(typeof(ForecastRun).GetProperty("OperationalEligibilityStatus", BindingFlags.Public | BindingFlags.Instance));

        var recommendation = dbContext.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Recommendation))!;
        Assert.False(recommendation.FindProperty(nameof(Recommendation.EvidenceSnapshotJson))!.IsNullable);
        Assert.NotNull(recommendation.FindProperty(nameof(Recommendation.ForecastRunId)));
        Assert.DoesNotContain(
            recommendation.GetForeignKeys(),
            key => key.PrincipalEntityType.ClrType == typeof(ForecastRun));
    }

    private static AnuradhapuraAiDbContext CreateMetadataContext()
    {
        var options = new DbContextOptionsBuilder<AnuradhapuraAiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AnuradhapuraAiDbContext(options);
    }
}
