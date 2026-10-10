using System.Net;
using System.Net.Http.Json;
using System.Text;
using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Application.Validations;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Persistence;
using AnuradhapuraAI.Infrastructure.Validations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class Phase9RecommendationValidationTests(Phase3WebApplicationFactory factory) : IClassFixture<Phase3WebApplicationFactory>
{
    [Fact]
    public async Task OfficerValidation_AnonymousRequest_IsRejected()
    {
        using var testFactory = CreateFactory(enableOfficerValidation: true);
        var client = testFactory.CreateClient();

        var response = await client.GetAsync("/api/validations/pending");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(ApprovedRoleNames.RegisteredUser)]
    [InlineData(ApprovedRoleNames.Administrator)]
    public async Task OfficerValidation_NonOfficerRole_IsForbidden(string roleName)
    {
        using var testFactory = CreateFactory(enableOfficerValidation: true);
        var client = testFactory.CreateClient();
        AddIdentity(client, userId: 1, roleName);

        var response = await client.GetAsync("/api/validations/pending");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OfficerValidation_DisabledFeature_ReturnsServiceUnavailable()
    {
        using var testFactory = CreateFactory(enableOfficerValidation: false);
        var recommendationId = await SeedScenarioAsync(testFactory, activeOfficer: true);
        var client = testFactory.CreateClient();
        AddIdentity(client, OfficerUserId, ApprovedRoleNames.AgriculturalOfficer);

        var response = await client.GetAsync($"/api/validations/recommendations/{recommendationId}");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task OfficerValidation_OfficerCanReviewRecommendationEvidence()
    {
        using var testFactory = CreateFactory(enableOfficerValidation: true);
        var recommendationId = await SeedScenarioAsync(testFactory, activeOfficer: true);
        var client = testFactory.CreateClient();
        AddIdentity(client, OfficerUserId, ApprovedRoleNames.AgriculturalOfficer);

        var response = await client.GetAsync($"/api/validations/recommendations/{recommendationId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RecommendationReviewResponse>();
        Assert.NotNull(body);
        Assert.Equal(recommendationId, body!.RecommendationId);
        Assert.Equal(ForecastRunId, body.ForecastRunId);
        Assert.Equal(6, body.Crops.Count);
        Assert.Equal(ApprovedCropNames.All, body.Crops.Select(crop => crop.CropName).ToArray());
        Assert.All(body.Crops, crop => Assert.False(string.IsNullOrWhiteSpace(crop.EvidenceSnapshotJson)));
        Assert.Contains("modelVersion", body.EvidenceSnapshotJson);
    }

    [Fact]
    public async Task OfficerValidation_SubmitValidation_UsesTrustedOfficerIdentityAndPersistsMetadataOnly()
    {
        using var testFactory = CreateFactory(enableOfficerValidation: true);
        var recommendationId = await SeedScenarioAsync(testFactory, activeOfficer: true);
        var before = await LoadRecommendationSnapshotAsync(testFactory, recommendationId);
        var client = testFactory.CreateClient();
        AddIdentity(client, OfficerUserId, ApprovedRoleNames.AgriculturalOfficer);

        var response = await client.PostAsJsonAsync(
            $"/api/validations/recommendations/{recommendationId}",
            new SubmitRecommendationValidationRequest
            {
                Status = ApprovedValidationStatuses.Validated,
                Comment = " Field evidence reviewed. "
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RecommendationValidationResponse>();
        Assert.NotNull(body);
        Assert.Equal(OfficerUserId, body!.OfficerUserId);
        Assert.Equal(ApprovedValidationStatuses.Validated, body.Status);
        Assert.Equal("Field evidence reviewed.", body.Comment);

        var after = await LoadRecommendationSnapshotAsync(testFactory, recommendationId);
        Assert.Equal(before.ForecastRunId, after.ForecastRunId);
        Assert.Equal(before.RecommendationEvidenceSnapshotJson, after.RecommendationEvidenceSnapshotJson);
        Assert.Equal(before.CropFingerprints, after.CropFingerprints);
        Assert.Single(after.ValidationFingerprints);
        Assert.Contains($"{OfficerUserId}|{ApprovedValidationStatuses.Validated}|Field evidence reviewed.", after.ValidationFingerprints);
    }

    [Fact]
    public async Task OfficerValidation_RequestBodyCannotSpoofOfficerIdentity()
    {
        using var testFactory = CreateFactory(enableOfficerValidation: true);
        var recommendationId = await SeedScenarioAsync(testFactory, activeOfficer: true);
        var client = testFactory.CreateClient();
        AddIdentity(client, OfficerUserId, ApprovedRoleNames.AgriculturalOfficer);
        using var content = new StringContent(
            $$"""{"status":"{{ApprovedValidationStatuses.NeedsReview}}","comment":"Reviewed","officerUserId":999}""",
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync($"/api/validations/recommendations/{recommendationId}", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var scope = testFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AnuradhapuraAiDbContext>();
        var validation = await dbContext.RecommendationValidations.SingleAsync();
        Assert.Equal(OfficerUserId, validation.OfficerUserId);
    }

    [Fact]
    public async Task OfficerValidation_InactiveOfficer_IsForbiddenWithoutPersistence()
    {
        using var testFactory = CreateFactory(enableOfficerValidation: true);
        var recommendationId = await SeedScenarioAsync(testFactory, activeOfficer: false);
        var client = testFactory.CreateClient();
        AddIdentity(client, OfficerUserId, ApprovedRoleNames.AgriculturalOfficer);

        var response = await client.PostAsJsonAsync(
            $"/api/validations/recommendations/{recommendationId}",
            new SubmitRecommendationValidationRequest { Status = ApprovedValidationStatuses.Validated });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(await LoadValidationsAsync(testFactory));
    }

    [Fact]
    public async Task OfficerValidation_InvalidStatusAndLongComment_AreRejectedSafely()
    {
        using var testFactory = CreateFactory(enableOfficerValidation: true);
        var recommendationId = await SeedScenarioAsync(testFactory, activeOfficer: true);
        var client = testFactory.CreateClient();
        AddIdentity(client, OfficerUserId, ApprovedRoleNames.AgriculturalOfficer);

        var invalidStatus = await client.PostAsJsonAsync(
            $"/api/validations/recommendations/{recommendationId}",
            new SubmitRecommendationValidationRequest { Status = "Approved" });
        var longComment = await client.PostAsJsonAsync(
            $"/api/validations/recommendations/{recommendationId}",
            new SubmitRecommendationValidationRequest
            {
                Status = ApprovedValidationStatuses.PendingValidation,
                Comment = new string('x', 1001)
            });
        var invalidBody = await invalidStatus.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, invalidStatus.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, longComment.StatusCode);
        Assert.DoesNotContain("ConnectionStrings", invalidBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UserSecretsId", invalidBody, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await LoadValidationsAsync(testFactory));
    }

    [Fact]
    public async Task OfficerValidation_MissingRecommendation_ReturnsNotFoundWithoutPersistence()
    {
        using var testFactory = CreateFactory(enableOfficerValidation: true);
        await SeedScenarioAsync(testFactory, activeOfficer: true);
        var client = testFactory.CreateClient();
        AddIdentity(client, OfficerUserId, ApprovedRoleNames.AgriculturalOfficer);

        var response = await client.PostAsJsonAsync(
            "/api/validations/recommendations/999999",
            new SubmitRecommendationValidationRequest { Status = ApprovedValidationStatuses.Validated });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await LoadValidationsAsync(testFactory));
    }

    [Fact]
    public async Task OfficerValidation_HistoryIsRetainedAndPendingListUsesLatestStatus()
    {
        using var testFactory = CreateFactory(enableOfficerValidation: true);
        var recommendationId = await SeedScenarioAsync(testFactory, activeOfficer: true);
        var client = testFactory.CreateClient();
        AddIdentity(client, OfficerUserId, ApprovedRoleNames.AgriculturalOfficer);

        var pending = await client.PostAsJsonAsync(
            $"/api/validations/recommendations/{recommendationId}",
            new SubmitRecommendationValidationRequest { Status = ApprovedValidationStatuses.PendingValidation });
        var validated = await client.PostAsJsonAsync(
            $"/api/validations/recommendations/{recommendationId}",
            new SubmitRecommendationValidationRequest { Status = ApprovedValidationStatuses.Validated });
        var pendingList = await client.GetFromJsonAsync<IReadOnlyList<RecommendationReviewSummaryResponse>>("/api/validations/pending");
        var review = await client.GetFromJsonAsync<RecommendationReviewResponse>($"/api/validations/recommendations/{recommendationId}");

        Assert.Equal(HttpStatusCode.Created, pending.StatusCode);
        Assert.Equal(HttpStatusCode.Created, validated.StatusCode);
        Assert.Empty(pendingList!);
        Assert.Equal(2, review!.ValidationHistory.Count);
        Assert.Equal(ApprovedValidationStatuses.Validated, review.ValidationHistory[0].Status);
        Assert.Equal(ApprovedValidationStatuses.PendingValidation, review.ValidationHistory[1].Status);
    }

    [Fact]
    public async Task OfficerValidation_CancellationPropagatesWithoutCreatingValidation()
    {
        using var testFactory = CreateFactory(enableOfficerValidation: true);
        var recommendationId = await SeedScenarioAsync(testFactory, activeOfficer: true);
        using var scope = testFactory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRecommendationValidationService>();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.SubmitValidationAsync(
                recommendationId,
                new RecommendationValidationSubmission(
                    ApprovedValidationStatuses.Validated,
                    null,
                    OfficerUserId),
                cancellation.Token));

        Assert.Empty(await LoadValidationsAsync(testFactory));
    }

    private Phase3WebApplicationFactory CreateFactory(bool enableOfficerValidation) =>
        factory.CreateIsolated(services =>
        {
            services.Configure<WeatherModelFeatureOptions>(options =>
                options.EnableOfficerValidation = enableOfficerValidation);
        });

    private static void AddIdentity(HttpClient client, int userId, string roleName)
    {
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, userId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.EmailHeader, $"{userId}@example.test");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, roleName);
    }

    private static async Task<int> SeedScenarioAsync(Phase3WebApplicationFactory testFactory, bool activeOfficer)
    {
        using var scope = testFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AnuradhapuraAiDbContext>();
        await SeedRolesAndUsersAsync(dbContext, activeOfficer);
        await SeedCropsAsync(dbContext);

        var recommendation = new Recommendation
        {
            ForecastRunId = ForecastRunId,
            SoilType = "Synthetic loam",
            EvidenceSnapshotJson = """{"schemaVersion":"phase9-test","modelVersion":"v1-phase9-test"}""",
            CreatedAt = DateTimeOffset.Parse("2026-10-10T00:00:00Z")
        };

        foreach (var crop in dbContext.Crops.OrderBy(crop => crop.Id))
        {
            var rank = crop.Id <= 4 ? crop.Id : null as int?;
            recommendation.RecommendationCrops.Add(new RecommendationCrop
            {
                CropId = crop.Id,
                TemperatureScore = rank.HasValue ? 80m - crop.Id : null,
                OverallScore = rank.HasValue ? 80m - crop.Id : null,
                SuitabilityCategory = rank.HasValue ? ApprovedSuitabilityCategories.Suitable : null,
                Rank = rank,
                EvaluationStatus = rank.HasValue
                    ? ApprovedRecommendationEvaluationStatuses.Partial
                    : ApprovedRecommendationEvaluationStatuses.InsufficientEvidence,
                Explanation = $"{crop.Name} synthetic Phase 9 recommendation evidence.",
                EvidenceSnapshotJson = $$"""{"schemaVersion":"phase9-test","cropName":"{{crop.Name}}"}"""
            });
        }

        dbContext.Recommendations.Add(recommendation);
        await dbContext.SaveChangesAsync();
        return recommendation.Id;
    }

    private static async Task SeedRolesAndUsersAsync(AnuradhapuraAiDbContext dbContext, bool activeOfficer)
    {
        dbContext.UserRoles.AddRange(
            new UserRole { Id = 1, Name = ApprovedRoleNames.RegisteredUser },
            new UserRole { Id = 2, Name = ApprovedRoleNames.AgriculturalOfficer },
            new UserRole { Id = 3, Name = ApprovedRoleNames.Administrator });
        dbContext.Users.AddRange(
            new User
            {
                Id = OfficerUserId,
                Name = "Phase 9 Officer",
                Email = "phase9-officer@example.test",
                PasswordHash = "test-only-hash",
                RoleId = 2,
                IsActive = activeOfficer,
                CreatedAt = DateTimeOffset.Parse("2026-10-10T00:00:00Z")
            },
            new User
            {
                Id = 20,
                Name = "Phase 9 Registered",
                Email = "phase9-registered@example.test",
                PasswordHash = "test-only-hash",
                RoleId = 1,
                IsActive = true,
                CreatedAt = DateTimeOffset.Parse("2026-10-10T00:00:00Z")
            },
            new User
            {
                Id = 30,
                Name = "Phase 9 Admin",
                Email = "phase9-admin@example.test",
                PasswordHash = "test-only-hash",
                RoleId = 3,
                IsActive = true,
                CreatedAt = DateTimeOffset.Parse("2026-10-10T00:00:00Z")
            });
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedCropsAsync(AnuradhapuraAiDbContext dbContext)
    {
        dbContext.Crops.AddRange(ApprovedCropNames.All.Select((name, index) => new Crop
        {
            Id = index + 1,
            Name = name,
            IsActive = true
        }));
        await dbContext.SaveChangesAsync();
    }

    private static async Task<RecommendationSnapshot> LoadRecommendationSnapshotAsync(
        Phase3WebApplicationFactory testFactory,
        int recommendationId)
    {
        using var scope = testFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AnuradhapuraAiDbContext>();
        var recommendation = await dbContext.Recommendations
            .AsNoTracking()
            .Include(item => item.RecommendationCrops)
            .Include(item => item.RecommendationValidations)
            .SingleAsync(item => item.Id == recommendationId);

        return new RecommendationSnapshot(
            recommendation.ForecastRunId,
            recommendation.EvidenceSnapshotJson,
            recommendation.RecommendationCrops
                .OrderBy(crop => crop.CropId)
                .Select(crop => $"{crop.CropId}|{crop.RainfallScore}|{crop.TemperatureScore}|{crop.HumidityScore}|{crop.SoilScore}|{crop.OverallScore}|{crop.SuitabilityCategory}|{crop.Rank}|{crop.EvaluationStatus}|{crop.Explanation}|{crop.EvidenceSnapshotJson}")
                .ToArray(),
            recommendation.RecommendationValidations
                .OrderBy(validation => validation.Id)
                .Select(validation => $"{validation.OfficerUserId}|{validation.Status}|{validation.Comment}")
                .ToArray());
    }

    private static async Task<IReadOnlyList<RecommendationValidation>> LoadValidationsAsync(
        Phase3WebApplicationFactory testFactory)
    {
        using var scope = testFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AnuradhapuraAiDbContext>();
        return await dbContext.RecommendationValidations.AsNoTracking().ToListAsync();
    }

    private static readonly Guid ForecastRunId = Guid.Parse("00000000-0000-0000-0000-000000009901");
    private const int OfficerUserId = 10;

    private sealed record RecommendationSnapshot(
        Guid ForecastRunId,
        string RecommendationEvidenceSnapshotJson,
        IReadOnlyList<string> CropFingerprints,
        IReadOnlyList<string> ValidationFingerprints);
}
