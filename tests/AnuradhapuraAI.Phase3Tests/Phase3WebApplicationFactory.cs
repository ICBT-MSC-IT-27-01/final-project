using AnuradhapuraAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class Phase3WebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName;

    public Phase3WebApplicationFactory()
        : this(Guid.NewGuid().ToString("N"))
    {
    }

    private Phase3WebApplicationFactory(string databaseName)
    {
        this.databaseName = databaseName;
    }

    public Phase3WebApplicationFactory CreateIsolated()
    {
        return new Phase3WebApplicationFactory(Guid.NewGuid().ToString("N"));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<DbContextOptions<AnuradhapuraAiDbContext>>();
            services.AddDbContext<AnuradhapuraAiDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthenticationHandler.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.AuthenticationScheme,
                _ => { });
        });
    }
}
