using System.Security.Claims;
using System.Text;
using AnuradhapuraAI.Application.Authentication;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Infrastructure;
using AnuradhapuraAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>() ?? new JwtSettings();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    context.Fail("Missing user identifier.");
                    return;
                }

                var dbContext = context.HttpContext.RequestServices.GetRequiredService<AnuradhapuraAiDbContext>();
                var userIsActive = await dbContext.Users
                    .AnyAsync(user => user.Id == userId && user.IsActive, context.HttpContext.RequestAborted);

                if (!userIsActive)
                {
                    context.Fail("Inactive or unknown user.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("AgriculturalOfficerOnly", policy => policy.RequireRole(ApprovedRoleNames.AgriculturalOfficer));
    options.AddPolicy("AdministratorOnly", policy => policy.RequireRole(ApprovedRoleNames.Administrator));
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapControllers();

app.Run();

public partial class Program;
