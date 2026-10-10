using System.Net;
using System.Net.Http.Json;
using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Application.Recommendations;
using AnuradhapuraAI.Application.Suitability;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class RecommendationHistoryEndpointTests(Phase3WebApplicationFactory factory) : IClassFixture<Phase3WebApplicationFactory>
{
    [Fact]
    public async Task History_RegisteredUserCanReadOwnHistoryNewestFirst()
    {
        using var testFactory = CreateFactory(enableHistory: true);
        await SeedScenarioAsync(testFactory);
        var client = CreateClient(testFactory, RegisteredUserId, ApprovedRoleNames.RegisteredUser);

        var response = await client.GetAsync("/api/recommendations/history?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RecommendationHistoryPageResponse>();
        Assert.NotNull(body);
        Assert.Equal(2, body!.TotalCount);
        Assert.Equal([RegisteredRecommendationId2, RegisteredRecommendationId1], body.Items.Select(item => item.RecommendationId).ToArray());
        Assert.All(body.Items, item =>
        {
            Assert.Equal(ForecastRunId, item.ForecastRunId);
            Assert.Equal(6, item.CropResultCount);
            Assert.NotNull(item.TargetStartDate);
        });
    }

    [Fact]
    public async Task History_DetailReturnsStoredEvidenceAndDoesNotExposePublicRecommendation()
    {
        using var testFactory = CreateFactory(enableHistory: true);
        await SeedScenarioAsync(testFactory);
        var client = CreateClient(testFactory, RegisteredUserId, ApprovedRoleNames.RegisteredUser);

        var own = await client.GetAsync($"/api/recommendations/{RegisteredRecommendationId1}");
        var publicRecommendation = await client.GetAsync($"/api/recommendations/{PublicRecommendationId}");

        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, publicRecommendation.StatusCode);
        var body = await own.Content.ReadFromJsonAsync<RecommendationHistoryDetailResponse>();
        Assert.NotNull(body);
        Assert.Equal(RegisteredRecommendationId1, body!.RecommendationId);
        Assert.Contains("phase11-test", body.EvidenceSnapshotJson);
        Assert.Equal(6, body.Crops.Count);
        Assert.Contains(body.Crops, crop =>
            crop.EvaluationStatus == ApprovedRecommendationEvaluationStatuses.InsufficientEvidence &&
            crop.Rank is null &&
            crop.OverallScore is null &&
            crop.UnavailableFactors.Contains(SuitabilityFactors.Soil));
        Assert.Contains(body.Crops, crop => crop.Factors.Count > 0);
    }

    [Fact]
    public async Task History_AnotherUsersRecommendationIsNotFound()
    {
        using var testFactory = CreateFactory(enableHistory: true);
        await SeedScenarioAsync(testFactory);
        var client = CreateClient(testFactory, OtherRegisteredUserId, ApprovedRoleNames.RegisteredUser);

        var response = await client.GetAsync($"/api/recommendations/{RegisteredRecommendationId1}");
        var history = await client.GetFromJsonAsync<RecommendationHistoryPageResponse>("/api/recommendations/history");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(history);
        Assert.Empty(history!.Items);
    }

    [Theory]
    [InlineData(ApprovedRoleNames.AgriculturalOfficer)]
    [InlineData(ApprovedRoleNames.Administrator)]
    public async Task History_PrivilegedRolesAreForbidden(string roleName)
    {
        using var testFactory = CreateFactory(enableHistory: true);
        await SeedScenarioAsync(testFactory);
        var client = CreateClient(testFactory, OfficerUserId, roleName);

        var response = await client.GetAsync("/api/recommendations/history");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task History_FeatureDisabled_ReturnsServiceUnavailable()
    {
        using var testFactory = CreateFactory(enableHistory: false);
        await SeedScenarioAsync(testFactory);
        var client = CreateClient(testFactory, RegisteredUserId, ApprovedRoleNames.RegisteredUser);

        var response = await client.GetAsync("/api/recommendations/history");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    public async Task History_InvalidPaging_ReturnsBadRequest(int page, int pageSize)
    {
        using var testFactory = CreateFactory(enableHistory: true);
        await SeedScenarioAsync(testFactory);
        var client = CreateClient(testFactory, RegisteredUserId, ApprovedRoleNames.RegisteredUser);

        var response = await client.GetAsync($"/api/recommendations/history?page={page}&pageSize={pageSize}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task History_InactiveRegisteredUser_IsForbidden()
    {
        using var testFactory = CreateFactory(enableHistory: true);
        await SeedScenarioAsync(testFactory, activeRegisteredUser: false);
        var client = CreateClient(testFactory, RegisteredUserId, ApprovedRoleNames.RegisteredUser);

        var response = await client.GetAsync("/api/recommendations/history");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task History_ServiceCancellationPropagates()
    {
        using var testFactory = CreateFactory(enableHistory: true);
        await SeedScenarioAsync(testFactory);
        using var scope = testFactory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRecommendationHistoryService>();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.GetDetailAsync(RegisteredRecommendationId1, RegisteredUserId, cancellation.Token));
    }

    private Phase3WebApplicationFactory CreateFactory(bool enableHistory) =>
        factory.CreateIsolated(services =>
        {
            services.Configure<WeatherModelFeatureOptions>(options =>
                options.EnableRecommendationHistory = enableHistory);
        });

    private static HttpClient CreateClient(Phase3WebApplicationFactory testFactory, int userId, string roleName)
    {
        var client = testFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, userId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.EmailHeader, $"{userId}@example.test");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, roleName);
        return client;
    }

    private static async Task SeedScenarioAsync(
        Phase3WebApplicationFactory testFactory,
        bool activeRegisteredUser = true)
    {
        using var scope = testFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AnuradhapuraAiDbContext>();
        dbContext.UserRoles.AddRange(
            new UserRole { Id = 1, Name = ApprovedRoleNames.RegisteredUser },
            new UserRole { Id = 2, Name = ApprovedRoleNames.AgriculturalOfficer },
            new UserRole { Id = 3, Name = ApprovedRoleNames.Administrator });
        dbContext.Users.AddRange(
            User(RegisteredUserId, 1, activeRegisteredUser),
            User(OtherRegisteredUserId, 1, true),
            User(OfficerUserId, 2, true),
            User(AdministratorUserId, 3, true));
        dbContext.Crops.AddRange(ApprovedCropNames.All.Select((name, index) => new Crop
        {
            Id = index + 1,
            Name = name,
            IsActive = true
        }));
        dbContext.Recommendations.AddRange(
            Recommendation(RegisteredRecommendationId1, RegisteredUserId, DateTimeOffset.Parse("2026-10-10T00:00:00Z")),
            Recommendation(RegisteredRecommendationId2, RegisteredUserId, DateTimeOffset.Parse("2026-10-11T00:00:00Z")),
            Recommendation(PublicRecommendationId, null, DateTimeOffset.Parse("2026-10-12T00:00:00Z")));
        await dbContext.SaveChangesAsync();
    }

    private static User User(int id, int roleId, bool isActive) => new()
    {
        Id = id,
        Name = $"History User {id}",
        Email = $"history-{id}@example.test",
        PasswordHash = "test-only-hash",
        RoleId = roleId,
        IsActive = isActive,
        CreatedAt = DateTimeOffset.Parse("2026-10-10T00:00:00Z")
    };

    private static Recommendation Recommendation(int id, int? userId, DateTimeOffset createdAt)
    {
        var recommendation = new Recommendation
        {
            Id = id,
            UserId = userId,
            ForecastRunId = ForecastRunId,
            SoilType = "Synthetic loam",
            EvidenceSnapshotJson = RecommendationSnapshotJson,
            CreatedAt = createdAt
        };

        foreach (var cropName in ApprovedCropNames.All)
        {
            var cropId = Array.IndexOf(ApprovedCropNames.All, cropName) + 1;
            var rank = cropId <= 4 ? cropId : null as int?;
            recommendation.RecommendationCrops.Add(new RecommendationCrop
            {
                CropId = cropId,
                TemperatureScore = rank.HasValue ? 80m - cropId : null,
                OverallScore = rank.HasValue ? 80m - cropId : null,
                SuitabilityCategory = rank.HasValue ? ApprovedSuitabilityCategories.Suitable : null,
                Rank = rank,
                EvaluationStatus = rank.HasValue
                    ? ApprovedRecommendationEvaluationStatuses.Partial
                    : ApprovedRecommendationEvaluationStatuses.InsufficientEvidence,
                Explanation = $"{cropName} stored Phase 11 history evidence.",
                EvidenceSnapshotJson = CropSnapshotJson(cropName)
            });
        }

        return recommendation;
    }

    private static string CropSnapshotJson(string cropName) =>
        $$"""
        {
          "schemaVersion": "phase11-test",
          "crop": { "name": "{{cropName}}" },
          "unavailableFactors": ["Rainfall", "Humidity", "Soil"],
          "factors": [
            {
              "factor": "Temperature",
              "isEvaluable": true,
              "score": 75,
              "aggregatedValue": 27,
              "configuredWeight": 25,
              "effectiveWeight": 1,
              "explanation": "Stored temperature factor evidence."
            }
          ],
          "climateRisks": ["Low rainfall"]
        }
        """;

    private const int RegisteredUserId = 201;
    private const int OtherRegisteredUserId = 202;
    private const int OfficerUserId = 203;
    private const int AdministratorUserId = 204;
    private const int RegisteredRecommendationId1 = 301;
    private const int RegisteredRecommendationId2 = 302;
    private const int PublicRecommendationId = 303;
    private static readonly Guid ForecastRunId = Guid.Parse("00000000-0000-0000-0000-000000011001");
    private const string RecommendationSnapshotJson = """
    {
      "schemaVersion": "phase11-test",
      "forecast": {
        "forecastRunId": "00000000-0000-0000-0000-000000011001",
        "forecastDate": "2026-10-10",
        "targetStartDate": "2026-10-11",
        "targetEndDate": "2026-10-17",
        "modelVersion": "v1-phase11-test",
        "createdAt": "2026-10-10T00:00:00Z",
        "days": [
          {
            "targetDate": "2026-10-11",
            "rainfall": { "value": 1 },
            "temperature": { "value": 27 },
            "humidity": { "value": 70 }
          }
        ]
      }
    }
    """;
}
