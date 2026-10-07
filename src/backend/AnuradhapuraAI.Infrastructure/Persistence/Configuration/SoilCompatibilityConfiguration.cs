using AnuradhapuraAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnuradhapuraAI.Infrastructure.Persistence.Configuration;

public sealed class SoilCompatibilityConfiguration : IEntityTypeConfiguration<SoilCompatibility>
{
    public void Configure(EntityTypeBuilder<SoilCompatibility> builder)
    {
        builder.ToTable("SoilCompatibility");

        builder.HasKey(soilCompatibility => soilCompatibility.Id);

        builder.Property(soilCompatibility => soilCompatibility.SoilType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(soilCompatibility => soilCompatibility.CompatibilityScore)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(soilCompatibility => soilCompatibility.IsActive)
            .IsRequired();

        builder.HasIndex(soilCompatibility => new { soilCompatibility.CropId, soilCompatibility.SoilType })
            .IsUnique();

        builder.HasOne(soilCompatibility => soilCompatibility.Crop)
            .WithMany(crop => crop.SoilCompatibilities)
            .HasForeignKey(soilCompatibility => soilCompatibility.CropId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
