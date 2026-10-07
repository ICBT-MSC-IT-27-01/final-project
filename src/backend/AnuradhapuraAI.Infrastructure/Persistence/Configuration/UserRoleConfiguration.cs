using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnuradhapuraAI.Infrastructure.Persistence.Configuration;

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRole");

        builder.HasKey(role => role.Id);

        builder.Property(role => role.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(role => role.Name)
            .IsUnique();

        builder.HasData(
            new UserRole { Id = 1, Name = ApprovedRoleNames.RegisteredUser },
            new UserRole { Id = 2, Name = ApprovedRoleNames.AgriculturalOfficer },
            new UserRole { Id = 3, Name = ApprovedRoleNames.Administrator });
    }
}
