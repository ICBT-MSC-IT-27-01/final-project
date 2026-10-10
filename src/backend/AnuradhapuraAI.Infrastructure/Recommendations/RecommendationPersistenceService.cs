using System.Data.Common;
using System.Text.Json;
using AnuradhapuraAI.Application.Recommendations;
using AnuradhapuraAI.Application.Suitability;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AnuradhapuraAI.Infrastructure.Recommendations;

public sealed class RecommendationPersistenceService(
    AnuradhapuraAiDbContext dbContext,
    IRecommendationOrchestrationService orchestrationService,
    IRecommendationRankingService rankingService,
    TimeProvider timeProvider) : IRecommendationPersistenceService
{
    private const string EvidenceSchemaVersion = "phase8b2-evidence-v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<RecommendationOrchestrationResult<PersistedRecommendationResponse>> CreateRecommendationAsync(
        CreateRecommendationPersistenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var evaluationResult = await orchestrationService.EvaluateLatestForecastAsync(
            new RecommendationEvaluationRequest(request.SoilType),
            cancellationToken);

        if (!evaluationResult.Succeeded || evaluationResult.Value is null)
        {
            return RecommendationOrchestrationResult<PersistedRecommendationResponse>.Failure(
                evaluationResult.ErrorCode ?? RecommendationOrchestrationErrorCodes.InvalidConfiguration,
                evaluationResult.Message ?? "Recommendation evaluation failed.");
        }

        if (!HasApprovedSixCropScope(evaluationResult.Value.Crops))
        {
            return RecommendationOrchestrationResult<PersistedRecommendationResponse>.Failure(
                RecommendationOrchestrationErrorCodes.InvalidConfiguration,
                "Recommendation evaluation must contain exactly the six approved MSc crops.");
        }

        var evaluationValidationError = ValidateEvaluationConsistency(evaluationResult.Value);
        if (evaluationValidationError is not null)
        {
            return RecommendationOrchestrationResult<PersistedRecommendationResponse>.Failure(
                RecommendationOrchestrationErrorCodes.InvalidConfiguration,
                evaluationValidationError);
        }

        var rankedEvaluation = rankingService.Rank(evaluationResult.Value);

        try
        {
            if (request.TrustedRegisteredUserId.HasValue &&
                !await IsValidRegisteredUserAsync(request.TrustedRegisteredUserId.Value, cancellationToken))
            {
                return RecommendationOrchestrationResult<PersistedRecommendationResponse>.Failure(
                    RecommendationOrchestrationErrorCodes.InvalidUserContext,
                    "Registered user context is invalid or not authorized for recommendation ownership.");
            }

            var cropIdsByName = await LoadApprovedCropIdsAsync(cancellationToken);
            if (cropIdsByName.Count != ApprovedCropNames.All.Length)
            {
                return RecommendationOrchestrationResult<PersistedRecommendationResponse>.Failure(
                    RecommendationOrchestrationErrorCodes.InvalidConfiguration,
                    "The active crop catalog must contain exactly the six approved MSc crops.");
            }

            if (dbContext.ChangeTracker.HasChanges())
            {
                return RecommendationOrchestrationResult<PersistedRecommendationResponse>.Failure(
                    RecommendationOrchestrationErrorCodes.PersistenceFailed,
                    "Recommendation persistence requires an isolated DbContext without unrelated pending changes.");
            }

            var createdAt = timeProvider.GetUtcNow();
            var recommendationSnapshotJson = SerializeRecommendationSnapshot(rankedEvaluation, request.SoilType, createdAt);
            var cropSnapshots = rankedEvaluation.Crops.ToDictionary(
                crop => crop.CropName,
                crop => SerializeCropSnapshot(
                    crop,
                    cropIdsByName[crop.CropName],
                    rankedEvaluation.Forecast,
                    rankedEvaluation.AppliedConfiguration),
                StringComparer.OrdinalIgnoreCase);

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var recommendation = new Recommendation
            {
                UserId = request.TrustedRegisteredUserId,
                ForecastRunId = rankedEvaluation.Forecast.ForecastRunId,
                SoilType = request.SoilType,
                EvidenceSnapshotJson = recommendationSnapshotJson,
                CreatedAt = createdAt
            };

            foreach (var crop in rankedEvaluation.Crops.OrderBy(crop => ApprovedCropOrder(crop.CropName)))
            {
                recommendation.RecommendationCrops.Add(new RecommendationCrop
                {
                    CropId = cropIdsByName[crop.CropName],
                    RainfallScore = crop.RainfallScore,
                    TemperatureScore = crop.TemperatureScore,
                    HumidityScore = crop.HumidityScore,
                    SoilScore = crop.SoilScore,
                    OverallScore = crop.OverallScore,
                    SuitabilityCategory = crop.SuitabilityCategory,
                    Rank = crop.Rank,
                    EvaluationStatus = crop.EvaluationStatus,
                    Explanation = crop.Explanation,
                    EvidenceSnapshotJson = cropSnapshots[crop.CropName]
                });
            }

            dbContext.Recommendations.Add(recommendation);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return RecommendationOrchestrationResult<PersistedRecommendationResponse>.Success(
                new PersistedRecommendationResponse(
                    recommendation.Id,
                    recommendation.ForecastRunId,
                    rankedEvaluation.Forecast,
                    recommendation.UserId,
                    recommendation.SoilType,
                    recommendation.CreatedAt,
                    rankedEvaluation.Crops));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (JsonException)
        {
            return RecommendationOrchestrationResult<PersistedRecommendationResponse>.Failure(
                RecommendationOrchestrationErrorCodes.SnapshotSerializationFailed,
                "Recommendation evidence snapshot could not be serialized.");
        }
        catch (NotSupportedException)
        {
            return RecommendationOrchestrationResult<PersistedRecommendationResponse>.Failure(
                RecommendationOrchestrationErrorCodes.SnapshotSerializationFailed,
                "Recommendation evidence snapshot could not be serialized.");
        }
        catch (Exception ex) when (IsPersistenceFailure(ex))
        {
            return RecommendationOrchestrationResult<PersistedRecommendationResponse>.Failure(
                RecommendationOrchestrationErrorCodes.PersistenceFailed,
                "Recommendation could not be persisted.");
        }
    }

    private async Task<bool> IsValidRegisteredUserAsync(int userId, CancellationToken cancellationToken) =>
        await dbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .AnyAsync(
                user => user.Id == userId &&
                    user.IsActive &&
                    user.Role != null &&
                    user.Role.Name == ApprovedRoleNames.RegisteredUser,
                cancellationToken);

    private async Task<Dictionary<string, int>> LoadApprovedCropIdsAsync(CancellationToken cancellationToken)
    {
        var crops = await dbContext.Crops
            .AsNoTracking()
            .Where(crop => crop.IsActive && ApprovedCropNames.All.Contains(crop.Name))
            .Select(crop => new { crop.Name, crop.Id })
            .ToListAsync(cancellationToken);

        return crops.ToDictionary(crop => crop.Name, crop => crop.Id, StringComparer.OrdinalIgnoreCase);
    }

    private static string SerializeRecommendationSnapshot(
        RankedRecommendationEvaluationResponse recommendation,
        string? soilType,
        DateTimeOffset createdAt)
    {
        var snapshot = new
            {
                schemaVersion = EvidenceSchemaVersion,
                generatedAt = createdAt,
                appliedConfiguration = new
                {
                    recommendation.AppliedConfiguration?.Weights,
                    recommendation.AppliedConfiguration?.CategoryThresholds,
                    scoring = new
                    {
                        method = "Phase 7B deterministic piecewise-linear scoring",
                        missingFactorHandling = "Unavailable factors are excluded and weights are re-normalized across evaluable factors."
                    }
                },
                forecast = new
                {
                recommendation.Forecast.ForecastRunId,
                recommendation.Forecast.ForecastDate,
                recommendation.Forecast.TargetStartDate,
                recommendation.Forecast.TargetEndDate,
                recommendation.Forecast.ModelVersion,
                recommendation.Forecast.CreatedAt,
                days = recommendation.Forecast.ForecastDays.Select(day => new
                {
                    day.TargetDate,
                    rainfall = new { value = day.Rainfall, unit = "mm", timeBasis = ApprovedTimeBases.SevenDay },
                    temperature = new { value = day.Temperature, unit = "deg C", aggregation = "daily forecast value" },
                    humidity = new { value = day.Humidity, unit = "%", aggregation = "daily forecast value" }
                })
            },
            soilType,
            ranking = new
            {
                ruleVersion = "phase8a-approved-v1",
                primary = "OverallScore descending",
                secondary = "Evidence coverage descending",
                tertiary = "Fixed approved crop order",
                rankedCropCount = recommendation.Crops.Count(crop => crop.Rank.HasValue)
            },
            limitations = new[]
            {
                "Evidence coverage describes configured/evaluable factors and is not a scientific confidence percentage.",
                "Unavailable factors remain null and are not replaced with fabricated scores.",
                "Rainfall scoring uses only requirements explicitly compatible with seven-day forecast scoring."
            }
        };

        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    private static string SerializeCropSnapshot(
        RankedCropRecommendationEvaluation crop,
        int cropId,
        SelectedForecastRun forecast,
        AppliedRecommendationConfiguration? appliedConfiguration)
    {
        var cropConfiguration = appliedConfiguration?.Crops.SingleOrDefault(config =>
            string.Equals(config.CropName, crop.CropName, StringComparison.OrdinalIgnoreCase));
        var snapshot = new
        {
            schemaVersion = EvidenceSchemaVersion,
            crop = new
            {
                id = cropId,
                name = crop.CropName
            },
            forecast = new
            {
                forecast.ForecastRunId,
                forecast.TargetStartDate,
                forecast.TargetEndDate,
                rainfallTimeBasis = ApprovedTimeBases.SevenDay
            },
            crop.EvaluationStatus,
            crop.Rank,
            crop.OverallScore,
            crop.SuitabilityCategory,
            appliedCategoryThresholds = appliedConfiguration?.CategoryThresholds,
            factorScores = new
            {
                crop.RainfallScore,
                crop.TemperatureScore,
                crop.HumidityScore,
                crop.SoilScore
            },
            unavailableFactors = crop.UnavailableFactors,
            factors = crop.Factors.Select(factor => new
            {
                factor.Factor,
                factor.IsEvaluable,
                factor.Score,
                factor.AggregatedValue,
                factor.ConfiguredWeight,
                factor.EffectiveWeight,
                factor.Explanation
            }),
            appliedRequirements = new
            {
                rainfall = ToRequirementSnapshot(cropConfiguration?.RainfallRequirement),
                temperature = ToRequirementSnapshot(cropConfiguration?.TemperatureRequirement),
                humidity = ToRequirementSnapshot(cropConfiguration?.HumidityRequirement)
            },
            appliedSoilCompatibility = cropConfiguration is null
                ? null
                : new
                {
                    entries = cropConfiguration.SoilCompatibilities.Select(compatibility => new
                    {
                        compatibility.SoilType,
                        compatibility.CompatibilityScore,
                        wasEvaluated = crop.SoilScore.HasValue &&
                            crop.Factors.Any(factor =>
                                factor.Factor == SuitabilityFactors.Soil &&
                                factor.IsEvaluable &&
                                factor.Score == compatibility.CompatibilityScore)
                    })
                },
            evidenceCoverage = new
            {
                crop.EvidenceCoverage.EvaluatedFactorCount,
                crop.EvidenceCoverage.TotalFactorCount,
                crop.EvidenceCoverage.EvaluatedConfiguredWeightTotal,
                note = "Evidence coverage is not a scientific confidence percentage."
            },
            crop.ClimateRisks,
            crop.Explanation
        };

        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    private static int ApprovedCropOrder(string cropName) =>
        Array.FindIndex(ApprovedCropNames.All, approvedCropName =>
            string.Equals(approvedCropName, cropName, StringComparison.OrdinalIgnoreCase));

    private static string? ValidateEvaluationConsistency(RecommendationEvaluationResponse evaluation)
    {
        if (evaluation.AppliedConfiguration is not null &&
            !HasApprovedAppliedConfigurationScope(evaluation.AppliedConfiguration.Crops))
        {
            return "Applied recommendation configuration must contain exactly the six approved MSc crops.";
        }

        foreach (var crop in evaluation.Crops)
        {
            var factors = crop.Factors.ToList();
            if (!HasApprovedFactorScope(factors))
            {
                return $"Recommendation evaluation for {crop.CropName} must contain exactly the four approved suitability factors.";
            }

            var evaluableFactors = factors.Where(factor => factor.IsEvaluable).ToList();
            if (crop.EvidenceCoverage.EvaluatedFactorCount != evaluableFactors.Count)
            {
                return $"Evidence coverage does not match evaluable factor count for {crop.CropName}.";
            }

            if (crop.EvidenceCoverage.TotalFactorCount != SuitabilityFactors.All.Length)
            {
                return $"Evidence coverage total factor count is invalid for {crop.CropName}.";
            }

            foreach (var factor in factors)
            {
                if (factor.IsEvaluable)
                {
                    if (!IsScoreValid(factor.Score))
                    {
                        return $"Evaluable {factor.Factor} score is invalid for {crop.CropName}.";
                    }
                }
                else if (factor.Score.HasValue)
                {
                    return $"Unavailable {factor.Factor} score must remain null for {crop.CropName}.";
                }
            }

            if (!MatchesFactorScore(crop.RainfallScore, factors, SuitabilityFactors.Rainfall) ||
                !MatchesFactorScore(crop.TemperatureScore, factors, SuitabilityFactors.Temperature) ||
                !MatchesFactorScore(crop.HumidityScore, factors, SuitabilityFactors.Humidity) ||
                !MatchesFactorScore(crop.SoilScore, factors, SuitabilityFactors.Soil))
            {
                return $"Recommendation factor scores do not match factor evidence for {crop.CropName}.";
            }

            if (!IsNullableScoreValid(crop.OverallScore))
            {
                return $"Overall score is invalid for {crop.CropName}.";
            }

            if (crop.SuitabilityCategory is not null &&
                !ApprovedSuitabilityCategories.All.Contains(crop.SuitabilityCategory))
            {
                return $"Suitability category is invalid for {crop.CropName}.";
            }

            var statusError = ValidateStatusConsistency(crop, evaluableFactors.Count);
            if (statusError is not null)
            {
                return statusError;
            }
        }

        return null;
    }

    private static string? ValidateStatusConsistency(CropRecommendationEvaluation crop, int evaluableFactorCount)
    {
        return crop.EvaluationStatus switch
        {
            ApprovedRecommendationEvaluationStatuses.InsufficientEvidence =>
                evaluableFactorCount == 0 &&
                crop.OverallScore is null &&
                crop.SuitabilityCategory is null &&
                crop.RainfallScore is null &&
                crop.TemperatureScore is null &&
                crop.HumidityScore is null &&
                crop.SoilScore is null
                    ? null
                    : $"Insufficient evidence evaluation for {crop.CropName} must not contain scores or a category.",
            ApprovedRecommendationEvaluationStatuses.Partial =>
                evaluableFactorCount > 0 &&
                evaluableFactorCount < SuitabilityFactors.All.Length &&
                crop.OverallScore.HasValue &&
                crop.SuitabilityCategory is not null
                    ? null
                    : $"Partial evaluation for {crop.CropName} must contain one to three evaluable factors, an overall score, and a category.",
            ApprovedRecommendationEvaluationStatuses.Complete =>
                evaluableFactorCount == SuitabilityFactors.All.Length &&
                crop.OverallScore.HasValue &&
                crop.SuitabilityCategory is not null
                    ? null
                    : $"Complete evaluation for {crop.CropName} must contain all four evaluable factors, an overall score, and a category.",
            _ => $"Evaluation status is invalid for {crop.CropName}."
        };
    }

    private static object? ToRequirementSnapshot(AppliedRangeRequirement? requirement) =>
        requirement is null
            ? null
            : new
            {
                requirement.OptimalMinimum,
                requirement.OptimalMaximum,
                requirement.AcceptableMinimum,
                requirement.AcceptableMaximum,
                requirement.Unit,
                requirement.TimeBasis,
                requirement.IsCompatibleWithSevenDayForecast
            };

    private static bool HasApprovedSixCropScope(IReadOnlyCollection<CropRecommendationEvaluation> crops) =>
        crops.Count == ApprovedCropNames.All.Length &&
        crops.Select(crop => crop.CropName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == ApprovedCropNames.All.Length &&
        crops.All(crop => ApprovedCropNames.All.Contains(crop.CropName, StringComparer.OrdinalIgnoreCase));

    private static bool HasApprovedAppliedConfigurationScope(IReadOnlyCollection<AppliedCropConfiguration> crops) =>
        crops.Count == ApprovedCropNames.All.Length &&
        crops.Select(crop => crop.CropName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == ApprovedCropNames.All.Length &&
        crops.All(crop => ApprovedCropNames.All.Contains(crop.CropName, StringComparer.OrdinalIgnoreCase));

    private static bool HasApprovedFactorScope(IReadOnlyCollection<SuitabilityFactorResult> factors) =>
        factors.Count == SuitabilityFactors.All.Length &&
        factors.Select(factor => factor.Factor)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == SuitabilityFactors.All.Length &&
        factors.All(factor => SuitabilityFactors.All.Contains(factor.Factor, StringComparer.OrdinalIgnoreCase));

    private static bool MatchesFactorScore(
        decimal? topLevelScore,
        IReadOnlyCollection<SuitabilityFactorResult> factors,
        string factorName)
    {
        var factor = factors.Single(item => item.Factor == factorName);
        return topLevelScore == factor.Score;
    }

    private static bool IsScoreValid(decimal? score) =>
        score.HasValue &&
        IsNullableScoreValid(score);

    private static bool IsNullableScoreValid(decimal? score) =>
        score is null ||
        score.Value is >= 0m and <= 100m &&
        score.Value != decimal.MinValue &&
        score.Value != decimal.MaxValue;

    private static bool IsPersistenceFailure(Exception exception) =>
        exception is DbUpdateException or DbException or TimeoutException or RetryLimitExceededException ||
        exception.InnerException is not null && IsPersistenceFailure(exception.InnerException);
}
