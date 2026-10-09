using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnuradhapuraAI.Infrastructure.Persistence.Configuration;

public sealed class RecommendationCropConfiguration : IEntityTypeConfiguration<RecommendationCrop>
{
    public void Configure(EntityTypeBuilder<RecommendationCrop> builder)
    {
        builder.ToTable("RecommendationCrop", table =>
        {
            table.HasCheckConstraint(
                "CK_RecommendationCrop_SuitabilityCategory",
                $"[{nameof(RecommendationCrop.SuitabilityCategory)}] IS NULL OR {CheckConstraintSql.In(nameof(RecommendationCrop.SuitabilityCategory), ApprovedSuitabilityCategories.All)}");
            table.HasCheckConstraint(
                "CK_RecommendationCrop_EvaluationStatus",
                CheckConstraintSql.In(nameof(RecommendationCrop.EvaluationStatus), ApprovedRecommendationEvaluationStatuses.All));
            table.HasCheckConstraint(
                "CK_RecommendationCrop_EvidenceSnapshotJson_IsJson",
                "ISJSON([EvidenceSnapshotJson]) = 1");
            table.HasCheckConstraint(
                "CK_RecommendationCrop_Rank_Positive",
                "[Rank] IS NULL OR [Rank] >= 1");
            table.HasCheckConstraint(
                "CK_RecommendationCrop_Scores_Range",
                "([RainfallScore] IS NULL OR ([RainfallScore] >= 0 AND [RainfallScore] <= 100)) AND " +
                "([TemperatureScore] IS NULL OR ([TemperatureScore] >= 0 AND [TemperatureScore] <= 100)) AND " +
                "([HumidityScore] IS NULL OR ([HumidityScore] >= 0 AND [HumidityScore] <= 100)) AND " +
                "([SoilScore] IS NULL OR ([SoilScore] >= 0 AND [SoilScore] <= 100)) AND " +
                "([OverallScore] IS NULL OR ([OverallScore] >= 0 AND [OverallScore] <= 100))");
        });

        builder.HasKey(recommendationCrop => recommendationCrop.Id);

        builder.Property(recommendationCrop => recommendationCrop.RainfallScore)
            .HasPrecision(5, 2);

        builder.Property(recommendationCrop => recommendationCrop.TemperatureScore)
            .HasPrecision(5, 2);

        builder.Property(recommendationCrop => recommendationCrop.HumidityScore)
            .HasPrecision(5, 2);

        builder.Property(recommendationCrop => recommendationCrop.SoilScore)
            .HasPrecision(5, 2);

        builder.Property(recommendationCrop => recommendationCrop.OverallScore)
            .HasPrecision(5, 2);

        builder.Property(recommendationCrop => recommendationCrop.SuitabilityCategory)
            .HasMaxLength(50);

        builder.Property(recommendationCrop => recommendationCrop.Explanation)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(recommendationCrop => recommendationCrop.EvaluationStatus)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(recommendationCrop => recommendationCrop.EvidenceSnapshotJson)
            .IsRequired();

        builder.HasIndex(recommendationCrop => new { recommendationCrop.RecommendationId, recommendationCrop.CropId })
            .IsUnique();

        builder.HasIndex(recommendationCrop => new { recommendationCrop.RecommendationId, recommendationCrop.Rank })
            .IsUnique()
            .HasFilter("[Rank] IS NOT NULL");

        builder.HasOne(recommendationCrop => recommendationCrop.Recommendation)
            .WithMany(recommendation => recommendation.RecommendationCrops)
            .HasForeignKey(recommendationCrop => recommendationCrop.RecommendationId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(recommendationCrop => recommendationCrop.Crop)
            .WithMany(crop => crop.RecommendationCrops)
            .HasForeignKey(recommendationCrop => recommendationCrop.CropId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
