using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnuradhapuraAI.Infrastructure.Persistence.Configuration;

public sealed class SuitabilityConfigurationConfiguration : IEntityTypeConfiguration<SuitabilityConfiguration>
{
    public void Configure(EntityTypeBuilder<SuitabilityConfiguration> builder)
    {
        builder.ToTable("SuitabilityConfiguration", table =>
        {
            table.HasCheckConstraint(
                "CK_SuitabilityConfiguration_ConfigurationType",
                CheckConstraintSql.In(nameof(SuitabilityConfiguration.ConfigurationType), ApprovedSuitabilityConfiguration.Types.All));
            table.HasCheckConstraint(
                "CK_SuitabilityConfiguration_ConfigurationKey",
                CheckConstraintSql.In(nameof(SuitabilityConfiguration.ConfigurationKey), ApprovedSuitabilityConfiguration.Keys.All));
        });

        builder.HasKey(configuration => configuration.Id);

        builder.Property(configuration => configuration.ConfigurationType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(configuration => configuration.ConfigurationKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(configuration => configuration.Value)
            .HasPrecision(9, 4)
            .IsRequired();

        builder.Property(configuration => configuration.IsActive)
            .IsRequired();

        builder.Property(configuration => configuration.UpdatedAt)
            .IsRequired();

        builder.HasIndex(configuration => new { configuration.ConfigurationType, configuration.ConfigurationKey })
            .IsUnique();

        builder.HasOne(configuration => configuration.UpdatedByUser)
            .WithMany(user => user.UpdatedSuitabilityConfigurations)
            .HasForeignKey(configuration => configuration.UpdatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
