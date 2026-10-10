using AnuradhapuraAI.Application.Authentication;
using AnuradhapuraAI.Application.Admin;
using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Application.Recommendations;
using AnuradhapuraAI.Application.Suitability;
using AnuradhapuraAI.Application.Validations;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Admin;
using AnuradhapuraAI.Infrastructure.Authentication;
using AnuradhapuraAI.Infrastructure.Forecasting;
using AnuradhapuraAI.Infrastructure.Persistence;
using AnuradhapuraAI.Infrastructure.Recommendations;
using AnuradhapuraAI.Infrastructure.Suitability;
using AnuradhapuraAI.Infrastructure.Validations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnuradhapuraAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? string.Empty;

        services.AddDbContext<AnuradhapuraAiDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAdminManagementService, AdminManagementService>();
        services.AddScoped<IForecastService, ForecastService>();
        services.AddScoped<IRecommendationOrchestrationService, RecommendationOrchestrationService>();
        services.AddScoped<IRecommendationRankingService, RecommendationRankingService>();
        services.AddScoped<IRecommendationPersistenceService, RecommendationPersistenceService>();
        services.AddScoped<IRecommendationValidationService, RecommendationValidationService>();
        services.AddScoped<ICropSuitabilityEngine, CropSuitabilityEngine>();
        services.AddScoped<ISuitabilityConfigurationProvider, SuitabilityConfigurationProvider>();
        services.Configure<ForecastingServiceOptions>(
            configuration.GetSection(ForecastingServiceOptions.SectionName));
        services.Configure<WeatherModelFeatureOptions>(
            configuration.GetSection(WeatherModelFeatureOptions.SectionName));
        services.AddHttpClient<IWeatherForecastingClient, PythonForecastingClient>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<ForecastingServiceOptions>>()
                .Value;

            if (Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri))
            {
                client.BaseAddress = baseUri;
            }

            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
