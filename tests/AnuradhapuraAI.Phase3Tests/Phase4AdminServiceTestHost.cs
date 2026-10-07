using AnuradhapuraAI.Application.Admin;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Admin;
using AnuradhapuraAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class Phase4AdminServiceTestHost : IAsyncDisposable
{
    private Phase4AdminServiceTestHost(AnuradhapuraAiDbContext dbContext, IAdminManagementService adminService)
    {
        DbContext = dbContext;
        AdminService = adminService;
    }

    public AnuradhapuraAiDbContext DbContext { get; }

    public IAdminManagementService AdminService { get; }

    public static async Task<Phase4AdminServiceTestHost> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<AnuradhapuraAiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var dbContext = new AnuradhapuraAiDbContext(options);
        var adminService = new AdminManagementService(dbContext, TimeProvider.System);
        var host = new Phase4AdminServiceTestHost(dbContext, adminService);
        await host.SeedReferenceDataAsync();
        return host;
    }

    public async Task<User> SeedUserAsync(string email, string roleName, bool isActive = true)
    {
        var role = await DbContext.UserRoles.SingleAsync(candidate => candidate.Name == roleName);
        var user = new User
        {
            Name = email,
            Email = email,
            PasswordHash = "test-only-hash",
            RoleId = role.Id,
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();
        return user;
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
    }

    private async Task SeedReferenceDataAsync()
    {
        DbContext.UserRoles.AddRange(
            new UserRole { Id = 1, Name = ApprovedRoleNames.RegisteredUser },
            new UserRole { Id = 2, Name = ApprovedRoleNames.AgriculturalOfficer },
            new UserRole { Id = 3, Name = ApprovedRoleNames.Administrator });
        DbContext.Crops.AddRange(
            new Crop { Id = 1, Name = ApprovedCropNames.Paddy, IsActive = true },
            new Crop { Id = 2, Name = ApprovedCropNames.Maize, IsActive = true });

        await DbContext.SaveChangesAsync();
    }
}
