using AnuradhapuraAI.Application.Authentication;
using AnuradhapuraAI.Application.Admin;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Admin;
using AnuradhapuraAI.Infrastructure.Authentication;
using AnuradhapuraAI.Infrastructure.Persistence;
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
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
