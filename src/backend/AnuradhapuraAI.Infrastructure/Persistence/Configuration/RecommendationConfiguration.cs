using AnuradhapuraAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnuradhapuraAI.Infrastructure.Persistence.Configuration;

public sealed class RecommendationConfiguration : IEntityTypeConfiguration<Recommendation>
{
    public void Configure(EntityTypeBuilder<Recommendation> builder)
    {
        builder.ToTable("Recommendation");

        builder.HasKey(recommendation => recommendation.Id);

        builder.Property(recommendation => recommendation.CreatedAt)
            .IsRequired();

        builder.HasOne(recommendation => recommendation.User)
            .WithMany(user => user.Recommendations)
            .HasForeignKey(recommendation => recommendation.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
