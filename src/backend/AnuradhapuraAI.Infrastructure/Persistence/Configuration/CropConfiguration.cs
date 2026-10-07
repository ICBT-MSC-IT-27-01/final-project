using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnuradhapuraAI.Infrastructure.Persistence.Configuration;

public sealed class CropConfiguration : IEntityTypeConfiguration<Crop>
{
    public void Configure(EntityTypeBuilder<Crop> builder)
    {
        builder.ToTable("Crop");

        builder.HasKey(crop => crop.Id);

        builder.Property(crop => crop.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(crop => crop.IsActive)
            .IsRequired();

        builder.HasIndex(crop => crop.Name)
            .IsUnique();

        builder.HasData(
            new Crop { Id = 1, Name = ApprovedCropNames.Paddy, IsActive = true },
            new Crop { Id = 2, Name = ApprovedCropNames.Maize, IsActive = true },
            new Crop { Id = 3, Name = ApprovedCropNames.GreenGram, IsActive = true },
            new Crop { Id = 4, Name = ApprovedCropNames.Cowpea, IsActive = true },
            new Crop { Id = 5, Name = ApprovedCropNames.Groundnut, IsActive = true },
            new Crop { Id = 6, Name = ApprovedCropNames.Chilli, IsActive = true });
    }
}
