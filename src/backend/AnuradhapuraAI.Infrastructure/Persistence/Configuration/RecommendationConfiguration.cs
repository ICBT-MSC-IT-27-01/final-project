using AnuradhapuraAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnuradhapuraAI.Infrastructure.Persistence.Configuration;

public sealed class RecommendationConfiguration : IEntityTypeConfiguration<Recommendation>
{
    public void Configure(EntityTypeBuilder<Recommendation> builder)
    {
        builder.ToTable("Recommendation", table =>
        {
            table.HasCheckConstraint(
                "CK_Recommendation_ForecastRunId_NotEmpty",
                "[ForecastRunId] <> '00000000-0000-0000-0000-000000000000'");
            table.HasCheckConstraint(
                "CK_Recommendation_EvidenceSnapshotJson_IsJson",
                "ISJSON([EvidenceSnapshotJson]) = 1");
        });

        builder.HasKey(recommendation => recommendation.Id);

        builder.Property(recommendation => recommendation.ForecastRunId)
            .IsRequired();

        builder.Property(recommendation => recommendation.SoilType)
            .HasMaxLength(100);

        builder.Property(recommendation => recommendation.EvidenceSnapshotJson)
            .IsRequired();

        builder.Property(recommendation => recommendation.CreatedAt)
            .IsRequired();

        builder.HasIndex(recommendation => recommendation.ForecastRunId);

        builder.HasOne(recommendation => recommendation.User)
            .WithMany(user => user.Recommendations)
            .HasForeignKey(recommendation => recommendation.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
