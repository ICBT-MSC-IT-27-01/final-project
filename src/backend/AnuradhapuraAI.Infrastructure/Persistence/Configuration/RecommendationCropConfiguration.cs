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
                CheckConstraintSql.In(nameof(RecommendationCrop.SuitabilityCategory), ApprovedSuitabilityCategories.All));
        });

        builder.HasKey(recommendationCrop => recommendationCrop.Id);

        builder.Property(recommendationCrop => recommendationCrop.RainfallScore)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(recommendationCrop => recommendationCrop.TemperatureScore)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(recommendationCrop => recommendationCrop.HumidityScore)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(recommendationCrop => recommendationCrop.SoilScore)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(recommendationCrop => recommendationCrop.OverallScore)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(recommendationCrop => recommendationCrop.SuitabilityCategory)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(recommendationCrop => recommendationCrop.Explanation)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(recommendationCrop => recommendationCrop.Rank)
            .IsRequired();

        builder.HasIndex(recommendationCrop => new { recommendationCrop.RecommendationId, recommendationCrop.CropId })
            .IsUnique();

        builder.HasIndex(recommendationCrop => new { recommendationCrop.RecommendationId, recommendationCrop.Rank })
            .IsUnique();

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
