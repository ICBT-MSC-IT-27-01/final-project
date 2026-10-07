using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnuradhapuraAI.Infrastructure.Persistence.Configuration;

public sealed class CropEnvironmentalRequirementConfiguration : IEntityTypeConfiguration<CropEnvironmentalRequirement>
{
    public void Configure(EntityTypeBuilder<CropEnvironmentalRequirement> builder)
    {
        builder.ToTable("CropEnvironmentalRequirement", table =>
        {
            table.HasCheckConstraint(
                "CK_CropEnvironmentalRequirement_VariableType",
                CheckConstraintSql.In(nameof(CropEnvironmentalRequirement.VariableType), ApprovedVariableTypes.All));
            table.HasCheckConstraint(
                "CK_CropEnvironmentalRequirement_MinimumMaximum",
                "[MinimumValue] <= [MaximumValue]");
            table.HasCheckConstraint(
                "CK_CropEnvironmentalRequirement_TimeBasis",
                $"[TimeBasis] IS NULL OR {CheckConstraintSql.In(nameof(CropEnvironmentalRequirement.TimeBasis), ApprovedTimeBases.All)}");
            table.HasCheckConstraint(
                "CK_CropEnvironmentalRequirement_AcceptableOptimalOrdering",
                "[AcceptableMinimumValue] IS NULL OR ([AcceptableMinimumValue] <= [MinimumValue] AND [MaximumValue] <= [AcceptableMaximumValue])");
        });

        builder.HasKey(requirement => requirement.Id);

        builder.Property(requirement => requirement.VariableType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(requirement => requirement.MinimumValue)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(requirement => requirement.MaximumValue)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(requirement => requirement.AcceptableMinimumValue)
            .HasPrecision(10, 2);

        builder.Property(requirement => requirement.AcceptableMaximumValue)
            .HasPrecision(10, 2);

        builder.Property(requirement => requirement.Unit)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(requirement => requirement.TimeBasis)
            .HasMaxLength(50);

        builder.Property(requirement => requirement.IsCompatibleWithSevenDayForecast)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(requirement => requirement.IsActive)
            .IsRequired();

        builder.HasIndex(requirement => new { requirement.CropId, requirement.VariableType })
            .IsUnique();

        builder.HasOne(requirement => requirement.Crop)
            .WithMany(crop => crop.EnvironmentalRequirements)
            .HasForeignKey(requirement => requirement.CropId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
