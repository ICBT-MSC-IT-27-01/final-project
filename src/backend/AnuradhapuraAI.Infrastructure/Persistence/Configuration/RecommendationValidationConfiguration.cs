using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnuradhapuraAI.Infrastructure.Persistence.Configuration;

public sealed class RecommendationValidationConfiguration : IEntityTypeConfiguration<RecommendationValidation>
{
    public void Configure(EntityTypeBuilder<RecommendationValidation> builder)
    {
        builder.ToTable("RecommendationValidation", table =>
        {
            table.HasCheckConstraint(
                "CK_RecommendationValidation_Status",
                CheckConstraintSql.In(nameof(RecommendationValidation.Status), ApprovedValidationStatuses.All));
        });

        builder.HasKey(validation => validation.Id);

        builder.Property(validation => validation.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(validation => validation.Comment)
            .HasMaxLength(1000);

        builder.Property(validation => validation.CreatedAt)
            .IsRequired();

        builder.HasOne(validation => validation.Recommendation)
            .WithMany(recommendation => recommendation.RecommendationValidations)
            .HasForeignKey(validation => validation.RecommendationId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(validation => validation.OfficerUser)
            .WithMany(user => user.OfficerRecommendationValidations)
            .HasForeignKey(validation => validation.OfficerUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
