using System.Data.Common;
using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Application.Recommendations;
using AnuradhapuraAI.Application.Suitability;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace AnuradhapuraAI.Infrastructure.Recommendations;

public sealed class RecommendationOrchestrationService(
    AnuradhapuraAiDbContext dbContext,
    ISuitabilityConfigurationProvider configurationProvider,
    ICropSuitabilityEngine suitabilityEngine,
    IOptions<WeatherModelFeatureOptions> featureOptions) : IRecommendationOrchestrationService
{
    private const int ForecastHorizonDays = 7;
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

    public async Task<RecommendationOrchestrationResult<RecommendationEvaluationResponse>> EvaluateLatestForecastAsync(
        RecommendationEvaluationRequest request,
        CancellationToken cancellationToken = default)
    {
        SelectedForecastRun? selectedForecast;
        SuitabilityConfigurationSnapshot configuration;

        try
        {
            selectedForecast = await SelectLatestValidForecastRunAsync(cancellationToken);
            if (selectedForecast is null)
            {
                return RecommendationOrchestrationResult<RecommendationEvaluationResponse>.Failure(
                    RecommendationOrchestrationErrorCodes.ForecastUnavailable,
                    "No complete valid seven-day persisted forecast run is available.");
            }

            configuration = await configurationProvider.GetActiveConfigurationAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return RecommendationOrchestrationResult<RecommendationEvaluationResponse>.Failure(
                RecommendationOrchestrationErrorCodes.InvalidConfiguration,
                ex.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsDatabaseReadFailure(ex))
        {
            return RecommendationOrchestrationResult<RecommendationEvaluationResponse>.Failure(
                RecommendationOrchestrationErrorCodes.DatabaseReadFailed,
                "Forecast or suitability configuration could not be read.");
        }

        var cropScopeError = ValidateApprovedCropScope(configuration.CropProfiles);
        if (cropScopeError is not null)
        {
            return RecommendationOrchestrationResult<RecommendationEvaluationResponse>.Failure(
                RecommendationOrchestrationErrorCodes.InvalidConfiguration,
                cropScopeError);
        }

        var suitabilityResult = suitabilityEngine.Evaluate(new SuitabilityEvaluationRequest(
            Forecast: selectedForecast.ForecastDays,
            SoilType: request.SoilType,
            CropProfiles: configuration.CropProfiles,
            Weights: configuration.Weights,
            CategoryThresholds: configuration.CategoryThresholds,
            EnableClimateRiskIndicators: featureOptions.Value.EnableClimateRiskIndicators,
            ClimateRiskThresholds: null,
            AllowInsufficientEvidenceResults: true));

        if (!suitabilityResult.Succeeded || suitabilityResult.Value is null)
        {
            return RecommendationOrchestrationResult<RecommendationEvaluationResponse>.Failure(
                RecommendationOrchestrationErrorCodes.InvalidConfiguration,
                suitabilityResult.Message ?? "Suitability configuration is invalid.");
        }

        var crops = suitabilityResult.Value.Crops
            .OrderBy(crop => Array.IndexOf(ApprovedCropNames.All, crop.CropName))
            .Select(ToRecommendationEvaluation)
            .ToList();

        return RecommendationOrchestrationResult<RecommendationEvaluationResponse>.Success(
            new RecommendationEvaluationResponse(selectedForecast, crops));
    }

    private async Task<SelectedForecastRun?> SelectLatestValidForecastRunAsync(CancellationToken cancellationToken)
    {
        var records = await dbContext.ForecastRecords
            .AsNoTracking()
            .Where(record => record.ForecastRunId != Guid.Empty)
            .ToListAsync(cancellationToken);

        return records
            .GroupBy(record => record.ForecastRunId)
            .Select(ToValidForecastRunOrNull)
            .Where(run => run is not null)
            .OrderByDescending(run => run!.CreatedAt)
            .ThenBy(run => run!.ForecastRunId.ToString("D"), StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static SelectedForecastRun? ToValidForecastRunOrNull(IGrouping<Guid, ForecastRecord> runGroup)
    {
        var records = runGroup
            .OrderBy(record => record.TargetDate)
            .ToList();

        if (runGroup.Key == Guid.Empty || records.Count != ForecastHorizonDays)
        {
            return null;
        }

        if (records.Select(record => record.TargetDate).Distinct().Count() != ForecastHorizonDays)
        {
            return null;
        }

        for (var i = 1; i < records.Count; i++)
        {
            if (records[i].TargetDate != records[i - 1].TargetDate.AddDays(1))
            {
                return null;
            }
        }

        if (records.Select(record => record.ForecastDate).Distinct().Count() != 1 ||
            records.Select(record => record.ModelVersion ?? string.Empty).Distinct(StringComparer.Ordinal).Count() != 1 ||
            records.Select(record => record.CreatedAt).Distinct().Count() != 1)
        {
            return null;
        }

        if (records.Any(record =>
            record.Rainfall < 0m ||
            record.Humidity < 0m ||
            record.Humidity > 100m ||
            !IsFinite(record.Rainfall) ||
            !IsFinite(record.Temperature) ||
            !IsFinite(record.Humidity)))
        {
            return null;
        }

        var forecastDays = records
            .Select(record => new SuitabilityForecastDay(
                record.TargetDate,
                record.Rainfall,
                record.Temperature,
                record.Humidity))
            .ToList();

        return new SelectedForecastRun(
            runGroup.Key,
            records[0].ForecastDate,
            records[0].TargetDate,
            records[^1].TargetDate,
            records[0].ModelVersion,
            records[0].CreatedAt,
            forecastDays);
    }

    private static CropRecommendationEvaluation ToRecommendationEvaluation(CropSuitabilityResult crop)
    {
        var unavailableFactors = crop.Factors
            .Where(factor => !factor.IsEvaluable)
            .Select(factor => factor.Factor)
            .ToList();

        var status = crop.EvaluatedFactorCount switch
        {
            0 => ApprovedRecommendationEvaluationStatuses.InsufficientEvidence,
            var count when count == SuitabilityFactors.All.Length => ApprovedRecommendationEvaluationStatuses.Complete,
            _ => ApprovedRecommendationEvaluationStatuses.Partial
        };

        return new CropRecommendationEvaluation(
            crop.CropName,
            status,
            crop.Rainfall.Score,
            crop.Temperature.Score,
            crop.Humidity.Score,
            crop.Soil.Score,
            crop.OverallScore,
            crop.SuitabilityCategory,
            unavailableFactors,
            crop.Factors,
            crop.ClimateRisks,
            crop.Explanation,
            new EvidenceCoverage(
                crop.EvaluatedFactorCount,
                SuitabilityFactors.All.Length,
                crop.Factors
                    .Where(factor => factor.IsEvaluable)
                    .Sum(factor => factor.ConfiguredWeight)));
    }

    private static string? ValidateApprovedCropScope(IReadOnlyList<CropSuitabilityProfile> profiles)
    {
        if (profiles.Count != ApprovedCropNames.All.Length)
        {
            return "The active crop catalog must contain exactly the six approved MSc crops.";
        }

        var duplicate = profiles
            .GroupBy(profile => profile.CropName.Trim(), NameComparer)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            return $"Duplicate approved crop configuration exists for '{duplicate.Key}'.";
        }

        var supplied = profiles.Select(profile => profile.CropName.Trim()).ToHashSet(NameComparer);
        var approved = ApprovedCropNames.All.ToHashSet(NameComparer);
        return supplied.SetEquals(approved)
            ? null
            : "The active crop catalog must match the approved MSc crop scope exactly.";
    }

    private static bool IsFinite(decimal value) =>
        value != decimal.MinValue && value != decimal.MaxValue;

    private static bool IsDatabaseReadFailure(Exception exception) =>
        exception is DbException or TimeoutException or RetryLimitExceededException ||
        exception.InnerException is not null && IsDatabaseReadFailure(exception.InnerException);
}
