using AnuradhapuraAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnuradhapuraAI.Infrastructure.Persistence;

public sealed class AnuradhapuraAiDbContext(DbContextOptions<AnuradhapuraAiDbContext> options) : DbContext(options)
{
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Crop> Crops => Set<Crop>();

    public DbSet<CropEnvironmentalRequirement> CropEnvironmentalRequirements => Set<CropEnvironmentalRequirement>();

    public DbSet<SoilCompatibility> SoilCompatibilities => Set<SoilCompatibility>();

    public DbSet<SuitabilityConfiguration> SuitabilityConfigurations => Set<SuitabilityConfiguration>();

    public DbSet<ForecastRecord> ForecastRecords => Set<ForecastRecord>();

    public DbSet<Recommendation> Recommendations => Set<Recommendation>();

    public DbSet<RecommendationCrop> RecommendationCrops => Set<RecommendationCrop>();

    public DbSet<RecommendationValidation> RecommendationValidations => Set<RecommendationValidation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AnuradhapuraAiDbContext).Assembly);
    }
}
