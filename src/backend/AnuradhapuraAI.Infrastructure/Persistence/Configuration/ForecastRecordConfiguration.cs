using AnuradhapuraAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnuradhapuraAI.Infrastructure.Persistence.Configuration;

public sealed class ForecastRecordConfiguration : IEntityTypeConfiguration<ForecastRecord>
{
    public void Configure(EntityTypeBuilder<ForecastRecord> builder)
    {
        builder.ToTable("ForecastRecord", table =>
        {
            table.HasCheckConstraint(
                "CK_ForecastRecord_ForecastRunId_NotEmpty",
                "[ForecastRunId] <> '00000000-0000-0000-0000-000000000000'");
        });

        builder.HasKey(forecast => forecast.Id);

        builder.Property(forecast => forecast.ForecastRunId)
            .IsRequired();

        builder.Property(forecast => forecast.ForecastDate)
            .IsRequired();

        builder.Property(forecast => forecast.TargetDate)
            .IsRequired();

        builder.Property(forecast => forecast.Rainfall)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(forecast => forecast.Temperature)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(forecast => forecast.Humidity)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(forecast => forecast.ModelVersion)
            .HasMaxLength(100);

        builder.Property(forecast => forecast.CreatedAt)
            .IsRequired();

        builder.HasIndex(forecast => new { forecast.ForecastDate, forecast.TargetDate });

        builder.HasIndex(forecast => new { forecast.ForecastRunId, forecast.TargetDate })
            .IsUnique();
    }
}
