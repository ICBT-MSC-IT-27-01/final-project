using AnuradhapuraAI.Application.Admin;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class Phase4AdminServiceTests
{
    [Fact]
    public async Task Crop_CreateUpdateListAndDeactivate_Works()
    {
        await using var host = await Phase4AdminServiceTestHost.CreateAsync();

        var create = await host.AdminService.CreateCropAsync(new CreateCropRequest { Name = "Test Crop" });
        var duplicate = await host.AdminService.CreateCropAsync(new CreateCropRequest { Name = "Test Crop" });
        var update = await host.AdminService.UpdateCropAsync(create.Value!.Id, new UpdateCropRequest { Name = "Updated Crop", IsActive = true });
        var deactivate = await host.AdminService.SetCropActiveStatusAsync(create.Value!.Id, false);
        var list = await host.AdminService.ListCropsAsync(isActive: false, page: 1, pageSize: 20);

        Assert.True(create.Succeeded);
        Assert.Equal(AdminErrorCodes.Duplicate, duplicate.ErrorCode);
        Assert.Equal("Updated Crop", update.Value!.Name);
        Assert.False(deactivate.Value!.IsActive);
        Assert.Contains(list.Items, crop => crop.Id == create.Value.Id);
    }

    [Fact]
    public async Task CropRequirement_ValidatesVariableRangeAndDuplicate()
    {
        await using var host = await Phase4AdminServiceTestHost.CreateAsync();

        var valid = await host.AdminService.CreateCropRequirementAsync(new CreateCropRequirementRequest
        {
            CropId = 1,
            VariableType = ApprovedVariableTypes.Rainfall,
            MinimumValue = 1,
            MaximumValue = 2,
            AcceptableMinimumValue = 0,
            AcceptableMaximumValue = 3,
            Unit = "mm"
        });
        var invalidVariable = await host.AdminService.CreateCropRequirementAsync(new CreateCropRequirementRequest { CropId = 1, VariableType = "Wind", MinimumValue = 1, MaximumValue = 2, Unit = "m/s" });
        var invalidRange = await host.AdminService.CreateCropRequirementAsync(new CreateCropRequirementRequest { CropId = 1, VariableType = ApprovedVariableTypes.Temperature, MinimumValue = 3, MaximumValue = 2, Unit = "C" });
        var duplicate = await host.AdminService.CreateCropRequirementAsync(new CreateCropRequirementRequest { CropId = 1, VariableType = ApprovedVariableTypes.Rainfall, MinimumValue = 1, MaximumValue = 2, Unit = "mm" });

        Assert.True(valid.Succeeded);
        Assert.Equal(AdminErrorCodes.InvalidVariableType, invalidVariable.ErrorCode);
        Assert.Equal(AdminErrorCodes.InvalidRange, invalidRange.ErrorCode);
        Assert.Equal(AdminErrorCodes.Duplicate, duplicate.ErrorCode);
    }

    [Fact]
    public async Task CropRequirement_CreateUpdateAndRead_PreservesScoringMetadata()
    {
        await using var host = await Phase4AdminServiceTestHost.CreateAsync();

        var create = await host.AdminService.CreateCropRequirementAsync(new CreateCropRequirementRequest
        {
            CropId = 1,
            VariableType = ApprovedVariableTypes.Temperature,
            MinimumValue = 20,
            MaximumValue = 30,
            AcceptableMinimumValue = 10,
            AcceptableMaximumValue = 40,
            Unit = "synthetic-test-unit",
            TimeBasis = ApprovedTimeBases.SevenDay,
            IsCompatibleWithSevenDayForecast = true
        });
        var update = await host.AdminService.UpdateCropRequirementAsync(create.Value!.Id, new UpdateCropRequirementRequest
        {
            CropId = 1,
            VariableType = ApprovedVariableTypes.Temperature,
            MinimumValue = 21,
            MaximumValue = 29,
            AcceptableMinimumValue = 11,
            AcceptableMaximumValue = 39,
            Unit = "synthetic-updated-unit",
            TimeBasis = ApprovedTimeBases.Daily,
            IsCompatibleWithSevenDayForecast = false,
            IsActive = true
        });
        var read = await host.AdminService.GetCropRequirementAsync(create.Value.Id);

        Assert.True(create.Succeeded);
        Assert.True(update.Succeeded);
        Assert.True(read.Succeeded);
        Assert.Equal(21, read.Value!.MinimumValue);
        Assert.Equal(29, read.Value.MaximumValue);
        Assert.Equal(11, read.Value.AcceptableMinimumValue);
        Assert.Equal(39, read.Value.AcceptableMaximumValue);
        Assert.Equal("synthetic-updated-unit", read.Value.Unit);
        Assert.Equal(ApprovedTimeBases.Daily, read.Value.TimeBasis);
        Assert.False(read.Value.IsCompatibleWithSevenDayForecast);
    }

    [Fact]
    public async Task CropRequirement_AllowsIncompleteAcceptableRangeButRejectsInvalidOrderingAndInvalidTimeBasis()
    {
        await using var host = await Phase4AdminServiceTestHost.CreateAsync();

        var incomplete = await host.AdminService.CreateCropRequirementAsync(new CreateCropRequirementRequest
        {
            CropId = 1,
            VariableType = ApprovedVariableTypes.Temperature,
            MinimumValue = 20,
            MaximumValue = 30,
            AcceptableMinimumValue = 10,
            Unit = "synthetic-test-unit",
            TimeBasis = ApprovedTimeBases.SevenDay
        });
        var invalidOrdering = await host.AdminService.CreateCropRequirementAsync(new CreateCropRequirementRequest
        {
            CropId = 1,
            VariableType = ApprovedVariableTypes.Humidity,
            MinimumValue = 20,
            MaximumValue = 30,
            AcceptableMinimumValue = 25,
            AcceptableMaximumValue = 40,
            Unit = "synthetic-test-unit",
            TimeBasis = ApprovedTimeBases.SevenDay
        });
        var invalidTimeBasis = await host.AdminService.CreateCropRequirementAsync(new CreateCropRequirementRequest
        {
            CropId = 1,
            VariableType = ApprovedVariableTypes.Rainfall,
            MinimumValue = 20,
            MaximumValue = 30,
            AcceptableMinimumValue = 10,
            AcceptableMaximumValue = 40,
            Unit = "synthetic-test-unit",
            TimeBasis = "Monthly"
        });

        Assert.True(incomplete.Succeeded);
        Assert.Equal(10, incomplete.Value!.AcceptableMinimumValue);
        Assert.Null(incomplete.Value.AcceptableMaximumValue);
        Assert.Equal(AdminErrorCodes.InvalidRange, invalidOrdering.ErrorCode);
        Assert.Equal(AdminErrorCodes.InvalidTimeBasis, invalidTimeBasis.ErrorCode);
    }

    [Fact]
    public async Task SoilCompatibility_DuplicateUpdateAndDeactivate_Work()
    {
        await using var host = await Phase4AdminServiceTestHost.CreateAsync();

        var create = await host.AdminService.CreateSoilCompatibilityAsync(new CreateSoilCompatibilityRequest { CropId = 1, SoilType = "Test Soil", CompatibilityScore = 1 });
        var duplicate = await host.AdminService.CreateSoilCompatibilityAsync(new CreateSoilCompatibilityRequest { CropId = 1, SoilType = "Test Soil", CompatibilityScore = 2 });
        var update = await host.AdminService.UpdateSoilCompatibilityAsync(create.Value!.Id, new UpdateSoilCompatibilityRequest { CropId = 1, SoilType = "Updated Soil", CompatibilityScore = 3, IsActive = true });
        var deactivate = await host.AdminService.SetSoilCompatibilityActiveStatusAsync(create.Value.Id, false);

        Assert.True(create.Succeeded);
        Assert.Equal(AdminErrorCodes.Duplicate, duplicate.ErrorCode);
        Assert.Equal("Updated Soil", update.Value!.SoilType);
        Assert.False(deactivate.Value!.IsActive);
    }

    [Fact]
    public async Task SuitabilityConfiguration_ValidatesApprovedValuesDuplicateAndAuditFields()
    {
        await using var host = await Phase4AdminServiceTestHost.CreateAsync();
        var admin = await host.SeedUserAsync("admin@example.com", ApprovedRoleNames.Administrator);

        var create = await host.AdminService.CreateSuitabilityConfigurationAsync(new CreateSuitabilityConfigurationRequest
        {
            ConfigurationType = ApprovedSuitabilityConfiguration.Types.FactorWeight,
            ConfigurationKey = ApprovedSuitabilityConfiguration.Keys.Rainfall,
            Value = 1
        }, admin.Id);
        var invalidType = await host.AdminService.CreateSuitabilityConfigurationAsync(new CreateSuitabilityConfigurationRequest { ConfigurationType = "Other", ConfigurationKey = ApprovedSuitabilityConfiguration.Keys.Rainfall, Value = 1 }, admin.Id);
        var invalidKey = await host.AdminService.CreateSuitabilityConfigurationAsync(new CreateSuitabilityConfigurationRequest { ConfigurationType = ApprovedSuitabilityConfiguration.Types.FactorWeight, ConfigurationKey = "Other", Value = 1 }, admin.Id);
        var duplicate = await host.AdminService.CreateSuitabilityConfigurationAsync(new CreateSuitabilityConfigurationRequest { ConfigurationType = ApprovedSuitabilityConfiguration.Types.FactorWeight, ConfigurationKey = ApprovedSuitabilityConfiguration.Keys.Rainfall, Value = 2 }, admin.Id);

        Assert.True(create.Succeeded);
        Assert.Equal(admin.Id, create.Value!.UpdatedByUserId);
        Assert.NotEqual(default, create.Value.UpdatedAt);
        Assert.Equal(AdminErrorCodes.InvalidConfigurationType, invalidType.ErrorCode);
        Assert.Equal(AdminErrorCodes.InvalidConfigurationKey, invalidKey.ErrorCode);
        Assert.Equal(AdminErrorCodes.Duplicate, duplicate.ErrorCode);
    }

    [Fact]
    public async Task UserManagement_AssignsApprovedRoleRejectsInvalidAndDoesNotExposePasswordHash()
    {
        await using var host = await Phase4AdminServiceTestHost.CreateAsync();
        var user = await host.SeedUserAsync("user@example.com", ApprovedRoleNames.RegisteredUser);

        var roleUpdate = await host.AdminService.UpdateUserRoleAsync(user.Id, new UpdateUserRoleRequest { Role = ApprovedRoleNames.AgriculturalOfficer });
        var invalidRole = await host.AdminService.UpdateUserRoleAsync(user.Id, new UpdateUserRoleRequest { Role = "Public User" });
        var statusUpdate = await host.AdminService.SetUserActiveStatusAsync(user.Id, false);

        Assert.True(roleUpdate.Succeeded);
        Assert.Equal(ApprovedRoleNames.AgriculturalOfficer, roleUpdate.Value!.Role);
        Assert.Equal(AdminErrorCodes.InvalidRole, invalidRole.ErrorCode);
        Assert.False(statusUpdate.Value!.IsActive);
        Assert.DoesNotContain("PasswordHash", string.Join(',', typeof(AdminUserResponse).GetProperties().Select(property => property.Name)));
    }

    [Fact]
    public async Task RecommendationAndValidationRecords_AreViewableReadOnly()
    {
        await using var host = await Phase4AdminServiceTestHost.CreateAsync();
        var officer = await host.SeedUserAsync("officer@example.com", ApprovedRoleNames.AgriculturalOfficer);
        var recommendation = new Recommendation { CreatedAt = DateTimeOffset.UtcNow };
        host.DbContext.Recommendations.Add(recommendation);
        await host.DbContext.SaveChangesAsync();
        host.DbContext.RecommendationCrops.Add(new RecommendationCrop
        {
            RecommendationId = recommendation.Id,
            CropId = 1,
            RainfallScore = 1,
            TemperatureScore = 1,
            HumidityScore = 1,
            SoilScore = 1,
            OverallScore = 1,
            SuitabilityCategory = ApprovedSuitabilityCategories.Suitable,
            Explanation = "Test explanation",
            Rank = 1
        });
        host.DbContext.RecommendationValidations.Add(new RecommendationValidation
        {
            RecommendationId = recommendation.Id,
            OfficerUserId = officer.Id,
            Status = ApprovedValidationStatuses.PendingValidation,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await host.DbContext.SaveChangesAsync();

        var recommendations = await host.AdminService.ListRecommendationsAsync(null, null, null, 1, 20);
        var recommendationDetail = await host.AdminService.GetRecommendationAsync(recommendation.Id);
        var validations = await host.AdminService.ListValidationsAsync(ApprovedValidationStatuses.PendingValidation, officer.Id, 1, 20);

        Assert.Single(recommendations.Items);
        Assert.True(recommendationDetail.Succeeded);
        Assert.Single(recommendationDetail.Value!.Crops);
        Assert.Single(validations.Items);
    }
}
