using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnuradhapuraAI.Infrastructure.Persistence.Configuration;

public sealed class ForecastRunConfiguration : IEntityTypeConfiguration<ForecastRun>
{
    public void Configure(EntityTypeBuilder<ForecastRun> builder)
    {
        builder.ToTable("ForecastRun", table =>
        {
            table.HasCheckConstraint(
                "CK_ForecastRun_ForecastRunId_NotEmpty",
                "[ForecastRunId] <> '00000000-0000-0000-0000-000000000000'");
            table.HasCheckConstraint(
                "CK_ForecastRun_RunPurpose",
                CheckConstraintSql.In(nameof(ForecastRun.RunPurpose), ApprovedForecastRunPurposes.All));
        });

        builder.HasKey(run => run.ForecastRunId);

        builder.Property(run => run.RunPurpose)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(run => run.CreatedAt)
            .IsRequired();

        builder.Property(run => run.ModelVersion)
            .HasMaxLength(100);

        builder.Property(run => run.SourceProvider)
            .HasMaxLength(100);

        builder.Property(run => run.SourceProduct)
            .HasMaxLength(100);

        builder.Property(run => run.RequestedLatitude)
            .HasPrecision(9, 6);

        builder.Property(run => run.RequestedLongitude)
            .HasPrecision(9, 6);

        builder.Property(run => run.Timezone)
            .HasMaxLength(100);

        builder.Property(run => run.DataQualityStatus)
            .HasMaxLength(50);
    }
}
