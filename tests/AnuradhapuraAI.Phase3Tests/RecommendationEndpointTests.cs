using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AnuradhapuraAI.Application.Recommendations;
using AnuradhapuraAI.Application.Suitability;
using AnuradhapuraAI.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class RecommendationEndpointTests(Phase3WebApplicationFactory factory) : IClassFixture<Phase3WebApplicationFactory>
{
    [Fact]
    public async Task CreateRecommendation_PublicRequest_ReturnsRecommendationWithoutUserAssociation()
    {
        var fakeService = new FakeRecommendationPersistenceService();
        using var testFactory = CreateFactory(fakeService);
        var client = testFactory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/recommendations",
            new CreateRecommendationRequest { District = "Anuradhapura", SoilType = " Synthetic loam " });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RecommendationResponse>();
        Assert.NotNull(body);
        Assert.Null(body!.UserId);
        Assert.Equal("Synthetic loam", fakeService.LastRequest!.SoilType);
        Assert.Null(fakeService.LastRequest.TrustedRegisteredUserId);
        Assert.Equal(fakeService.ForecastRunId, body.ForecastRunId);
        Assert.Equal(fakeService.ForecastRunId, body.Forecast.ForecastRunId);
        Assert.Equal(7, body.Forecast.Days.Count);
        Assert.Equal(new DateOnly(2026, 1, 2), body.Forecast.TargetStartDate);
        Assert.Equal(new DateOnly(2026, 1, 8), body.Forecast.TargetEndDate);
        Assert.Equal(6, body.Crops.Count);
        Assert.Equal(ApprovedCropNames.All, body.Crops.Select(crop => crop.CropName).ToArray());
        Assert.Equal([1, 2, 3, 4], body.Crops
            .Where(crop => crop.Rank.HasValue)
            .Select(crop => crop.Rank!.Value)
            .Order()
            .ToArray());
        Assert.Contains(body.Crops, crop =>
            crop.EvaluationStatus == ApprovedRecommendationEvaluationStatuses.InsufficientEvidence &&
            crop.Rank is null &&
            crop.OverallScore is null &&
            crop.SuitabilityCategory is null);
        Assert.All(body.Crops, crop =>
        {
            Assert.Equal(SuitabilityFactors.All.Length, crop.EvidenceCoverage.TotalFactorCount);
            Assert.NotEmpty(crop.Factors);
            Assert.False(string.IsNullOrWhiteSpace(crop.Explanation));
        });
    }

    [Fact]
    public async Task CreateRecommendation_RegisteredUserRequest_PassesTrustedUserId()
    {
        var fakeService = new FakeRecommendationPersistenceService();
        using var testFactory = CreateFactory(fakeService);
        var client = testFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.EmailHeader, "registered@example.test");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, ApprovedRoleNames.RegisteredUser);

        var response = await client.PostAsJsonAsync(
            "/api/recommendations",
            new CreateRecommendationRequest { District = "Anuradhapura District" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(42, fakeService.LastRequest!.TrustedRegisteredUserId);
    }

    [Fact]
    public async Task CreateRecommendation_UserIdInRequestBody_IsIgnored()
    {
        var fakeService = new FakeRecommendationPersistenceService();
        using var testFactory = CreateFactory(fakeService);
        var client = testFactory.CreateClient();

        using var content = new StringContent(
            """{"district":"Anuradhapura","soilType":"Synthetic loam","userId":999}""",
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/recommendations", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(fakeService.LastRequest!.TrustedRegisteredUserId);
    }

    [Theory]
    [InlineData(ApprovedRoleNames.AgriculturalOfficer)]
    [InlineData(ApprovedRoleNames.Administrator)]
    public async Task CreateRecommendation_PrivilegedAuthenticatedUser_ReturnsForbidden(string roleName)
    {
        var fakeService = new FakeRecommendationPersistenceService();
        using var testFactory = CreateFactory(fakeService);
        var client = testFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "2");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.EmailHeader, "privileged@example.test");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, roleName);

        var response = await client.PostAsJsonAsync(
            "/api/recommendations",
            new CreateRecommendationRequest { District = "Anuradhapura" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(fakeService.LastRequest);
    }

    [Fact]
    public async Task CreateRecommendation_InvalidAuthenticatedIdentity_ReturnsUnauthorized()
    {
        var fakeService = new FakeRecommendationPersistenceService();
        using var testFactory = CreateFactory(fakeService);
        var client = testFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "not-an-id");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.EmailHeader, "registered@example.test");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, ApprovedRoleNames.RegisteredUser);

        var response = await client.PostAsJsonAsync(
            "/api/recommendations",
            new CreateRecommendationRequest { District = "Anuradhapura" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(fakeService.LastRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Colombo")]
    public async Task CreateRecommendation_InvalidDistrict_ReturnsBadRequest(string district)
    {
        var fakeService = new FakeRecommendationPersistenceService();
        using var testFactory = CreateFactory(fakeService);
        var client = testFactory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/recommendations",
            new CreateRecommendationRequest { District = district });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(fakeService.LastRequest);
    }

    [Fact]
    public async Task CreateRecommendation_OversizedSoilType_ReturnsBadRequest()
    {
        var fakeService = new FakeRecommendationPersistenceService();
        using var testFactory = CreateFactory(fakeService);
        var client = testFactory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/recommendations",
            new CreateRecommendationRequest { District = "Anuradhapura", SoilType = new string('x', 101) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(fakeService.LastRequest);
    }

    [Fact]
    public async Task CreateRecommendation_EmptyBody_ReturnsBadRequest()
    {
        var fakeService = new FakeRecommendationPersistenceService();
        using var testFactory = CreateFactory(fakeService);
        var client = testFactory.CreateClient();
        using var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/recommendations", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(fakeService.LastRequest);
    }

    [Fact]
    public async Task CreateRecommendation_MalformedJson_ReturnsBadRequestWithoutInternalDetails()
    {
        var fakeService = new FakeRecommendationPersistenceService();
        using var testFactory = CreateFactory(fakeService);
        var client = testFactory.CreateClient();
        using var content = new StringContent("""{"district":""", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/recommendations", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("ConnectionStrings", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UserSecretsId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Sql", body, StringComparison.OrdinalIgnoreCase);
        Assert.Null(fakeService.LastRequest);
    }

    [Fact]
    public async Task CreateRecommendation_ForecastUnavailable_ReturnsServiceUnavailable()
    {
        var fakeService = new FakeRecommendationPersistenceService(
            RecommendationOrchestrationResult<PersistedRecommendationResponse>.Failure(
                RecommendationOrchestrationErrorCodes.ForecastUnavailable,
                "No complete forecast is available."));
        using var testFactory = CreateFactory(fakeService);
        var client = testFactory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/recommendations",
            new CreateRecommendationRequest { District = "Anuradhapura" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Theory]
    [InlineData(RecommendationOrchestrationErrorCodes.InvalidConfiguration, HttpStatusCode.InternalServerError)]
    [InlineData(RecommendationOrchestrationErrorCodes.DatabaseReadFailed, HttpStatusCode.ServiceUnavailable)]
    [InlineData(RecommendationOrchestrationErrorCodes.PersistenceFailed, HttpStatusCode.InternalServerError)]
    [InlineData(RecommendationOrchestrationErrorCodes.SnapshotSerializationFailed, HttpStatusCode.InternalServerError)]
    [InlineData(RecommendationOrchestrationErrorCodes.InvalidUserContext, HttpStatusCode.Forbidden)]
    public async Task CreateRecommendation_ControlledFailures_MapToSafeStatusCodes(
        string errorCode,
        HttpStatusCode expectedStatusCode)
    {
        var fakeService = new FakeRecommendationPersistenceService(
            RecommendationOrchestrationResult<PersistedRecommendationResponse>.Failure(
                errorCode,
                "Synthetic failure detail that should not expose internals."));
        using var testFactory = CreateFactory(fakeService);
        var client = testFactory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/recommendations",
            new CreateRecommendationRequest { District = "Anuradhapura" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.DoesNotContain("ConnectionStrings", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UserSecretsId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Server=", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecommendationHistoryRoute_AnonymousRequest_IsRejected()
    {
        using var testFactory = CreateFactory(new FakeRecommendationPersistenceService());
        var client = testFactory.CreateClient();

        var response = await client.GetAsync("/api/recommendations/history");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private Phase3WebApplicationFactory CreateFactory(FakeRecommendationPersistenceService fakeService) =>
        factory.CreateIsolated(services =>
        {
            services.RemoveAll<IRecommendationPersistenceService>();
            services.AddSingleton<IRecommendationPersistenceService>(fakeService);
        });

    private sealed class FakeRecommendationPersistenceService : IRecommendationPersistenceService
    {
        private readonly RecommendationOrchestrationResult<PersistedRecommendationResponse> result;

        public Guid ForecastRunId { get; } = Guid.Parse("00000000-0000-0000-0000-000000008901");

        public FakeRecommendationPersistenceService()
            : this(RecommendationOrchestrationResult<PersistedRecommendationResponse>.Success(CreateResponse(null)))
        {
        }

        public FakeRecommendationPersistenceService(RecommendationOrchestrationResult<PersistedRecommendationResponse> result)
        {
            this.result = result;
        }

        public CreateRecommendationPersistenceRequest? LastRequest { get; private set; }

        public Task<RecommendationOrchestrationResult<PersistedRecommendationResponse>> CreateRecommendationAsync(
            CreateRecommendationPersistenceRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastRequest = request;

            var response = result.Succeeded
                ? RecommendationOrchestrationResult<PersistedRecommendationResponse>.Success(CreateResponse(request.TrustedRegisteredUserId))
                : result;

            return Task.FromResult(response);
        }

        private static PersistedRecommendationResponse CreateResponse(int? userId)
        {
            var forecastRunId = Guid.Parse("00000000-0000-0000-0000-000000008901");
            return new PersistedRecommendationResponse(
                1001,
                forecastRunId,
                new SelectedForecastRun(
                    forecastRunId,
                    new DateOnly(2026, 1, 1),
                    new DateOnly(2026, 1, 2),
                    new DateOnly(2026, 1, 8),
                    "v1-api-test",
                    DateTimeOffset.Parse("2026-10-09T00:00:00Z"),
                    Enumerable.Range(1, 7)
                        .Select(day => new SuitabilityForecastDay(
                            new DateOnly(2026, 1, 1).AddDays(day),
                            Rainfall: day,
                            Temperature: 26m,
                            Humidity: 70m))
                        .ToList()),
                userId,
                "Synthetic loam",
                DateTimeOffset.Parse("2026-10-09T00:00:00Z"),
                CreateCrops());
        }

        private static IReadOnlyList<RankedCropRecommendationEvaluation> CreateCrops() =>
            ApprovedCropNames.All
                .Select((cropName, index) => CreateCrop(
                    cropName,
                    index switch
                    {
                        0 => 92m,
                        1 => 85m,
                        2 => null,
                        3 => 72m,
                        4 => 61m,
                        _ => null
                    },
                    index switch
                    {
                        0 => 1,
                        1 => 2,
                        3 => 3,
                        4 => 4,
                        _ => null
                    },
                    index is 2 or 5
                        ? ApprovedRecommendationEvaluationStatuses.InsufficientEvidence
                        : ApprovedRecommendationEvaluationStatuses.Partial))
                .ToList();

        private static RankedCropRecommendationEvaluation CreateCrop(
            string cropName,
            decimal? overallScore,
            int? rank,
            string evaluationStatus)
        {
            var factors = SuitabilityFactors.All.Select(factor =>
            {
                var isEvaluable = overallScore.HasValue && factor == SuitabilityFactors.Temperature;
                return new SuitabilityFactorResult(
                    factor,
                    isEvaluable,
                    isEvaluable ? overallScore : null,
                    isEvaluable ? 26m : null,
                    ConfiguredWeight: 25m,
                    EffectiveWeight: isEvaluable ? 1m : null,
                    Explanation: isEvaluable
                        ? "Synthetic API endpoint factor was evaluated."
                        : "Synthetic API endpoint factor was unavailable.");
            }).ToList();

            return new RankedCropRecommendationEvaluation(
                cropName,
                evaluationStatus,
                RainfallScore: null,
                TemperatureScore: overallScore,
                HumidityScore: null,
                SoilScore: null,
                OverallScore: overallScore,
                SuitabilityCategory: overallScore.HasValue ? ApprovedSuitabilityCategories.Suitable : null,
                Rank: rank,
                UnavailableFactors: [SuitabilityFactors.Rainfall, SuitabilityFactors.Humidity, SuitabilityFactors.Soil],
                Factors: factors,
                ClimateRisks: [],
                Explanation: $"{cropName} synthetic API endpoint recommendation.",
                new EvidenceCoverage(overallScore.HasValue ? 1 : 0, SuitabilityFactors.All.Length, overallScore.HasValue ? 25m : 0m));
        }
    }
}
