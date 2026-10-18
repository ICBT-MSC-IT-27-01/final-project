using System.Data.Common;
using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Application.Recommendations;
using AnuradhapuraAI.Application.Suitability;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Forecasting;
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
            new RecommendationEvaluationResponse(selectedForecast, crops)
            {
                AppliedConfiguration = ToAppliedConfiguration(configuration)
            });
    }

    private async Task<SelectedForecastRun?> SelectLatestValidForecastRunAsync(CancellationToken cancellationToken)
    {
        var runs = await dbContext.ForecastRuns
            .AsNoTracking()
            .Include(run => run.ForecastRecords)
            .Where(run => run.RunPurpose == ApprovedForecastRunPurposes.Operational)
            .ToListAsync(cancellationToken);

        return runs
            .Select(OperationalForecastRunSelector.ToSelectedForecastRunOrNull)
            .Where(run => run is not null)
            .OrderByDescending(run => run!.CreatedAt)
            .ThenBy(run => run!.ForecastRunId.ToString("D"), StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static CropRecommendationEvaluation ToRecommendationEvaluation(CropSuitabilityResult crop)
    {
        var unavailableFactors = crop.Factors
            .Where(factor => !factor.IsEvaluable)
            .Select(factor => factor.Factor)
            .ToList();

        var hasAllMandatoryWeatherFactors = HasAllMandatoryWeatherFactors(crop);
        var status = crop.EvaluatedFactorCount switch
        {
            0 => ApprovedRecommendationEvaluationStatuses.InsufficientEvidence,
            _ when hasAllMandatoryWeatherFactors => ApprovedRecommendationEvaluationStatuses.Complete,
            _ => ApprovedRecommendationEvaluationStatuses.Partial
        };

        var assessmentState = crop.EvaluatedFactorCount switch
        {
            0 => RecommendationAssessmentStates.InsufficientAgronomicEvidence,
            _ when !crop.SupportsFullSuitabilityAssessment => RecommendationAssessmentStates.PartiallyAssessed,
            _ when hasAllMandatoryWeatherFactors => RecommendationAssessmentStates.SuitabilityAssessed,
            _ => RecommendationAssessmentStates.PartiallyAssessed
        };

        var explanation = assessmentState == RecommendationAssessmentStates.PartiallyAssessed
            ? BuildPartialAssessmentExplanation(crop, unavailableFactors)
            : crop.Explanation;

        var supportsOverallSuitabilityAssessment =
            assessmentState == RecommendationAssessmentStates.SuitabilityAssessed;

        return new CropRecommendationEvaluation(
            crop.CropName,
            status,
            crop.Rainfall.Score,
            crop.Temperature.Score,
            crop.Humidity.Score,
            crop.Soil.Score,
            supportsOverallSuitabilityAssessment ? crop.OverallScore : null,
            supportsOverallSuitabilityAssessment ? crop.SuitabilityCategory : null,
            unavailableFactors,
            crop.Factors,
            crop.ClimateRisks,
            explanation,
            new EvidenceCoverage(
                crop.EvaluatedFactorCount,
                SuitabilityFactors.All.Length,
                crop.Factors
                    .Where(factor => factor.IsEvaluable)
                    .Sum(factor => factor.ConfiguredWeight)))
        {
            AssessmentState = assessmentState,
            AssessmentBasis = crop.AssessmentBasis,
            EvidenceSources = crop.EvidenceSources
                .Select(source => new RecommendationEvidenceSource(
                    source.Title,
                    source.SourceUrl,
                    source.PublicationDetails,
                    source.Location,
                    source.EvidenceStatus))
                .ToList(),
            ScientificLimitations = crop.ScientificLimitations
        };
    }

    private static bool HasAllMandatoryWeatherFactors(CropSuitabilityResult crop) =>
        crop.Rainfall.IsEvaluable &&
        crop.Temperature.IsEvaluable &&
        crop.Humidity.IsEvaluable;

    private static string BuildPartialAssessmentExplanation(
        CropSuitabilityResult crop,
        IReadOnlyList<string> unavailableFactors)
    {
        var evaluatedFactors = crop.Factors
            .Where(factor => factor.IsEvaluable)
            .Select(factor => factor.Factor)
            .ToList();

        return $"{crop.CropName}: {crop.AssessmentBasis}. Evaluated factor(s): {string.Join(", ", evaluatedFactors)}. " +
               $"Unavailable factor(s): {string.Join(", ", unavailableFactors)}. " +
               "Factor-level numeric values are short-horizon indicators for the stated basis only; no overall crop-suitability score, category, or rank is assigned.";
    }

    private static AppliedRecommendationConfiguration ToAppliedConfiguration(
        SuitabilityConfigurationSnapshot configuration) =>
        new(
            configuration.Weights,
            configuration.CategoryThresholds,
            configuration.CropProfiles
                .OrderBy(profile => Array.IndexOf(ApprovedCropNames.All, profile.CropName))
                .Select(ToAppliedCropConfiguration)
                .ToList());

    private static AppliedCropConfiguration ToAppliedCropConfiguration(CropSuitabilityProfile profile) =>
        new(
            profile.CropName,
            ToAppliedRangeRequirement(profile.RainfallRequirement),
            ToAppliedRangeRequirement(profile.TemperatureRequirement),
            ToAppliedRangeRequirement(profile.HumidityRequirement),
            profile.SoilCompatibilities
                .Select(compatibility => new AppliedSoilCompatibility(
                    compatibility.SoilType,
                    compatibility.CompatibilityScore))
                .ToList());

    private static AppliedRangeRequirement? ToAppliedRangeRequirement(SuitabilityRangeRequirement? requirement) =>
        requirement is null
            ? null
            : new AppliedRangeRequirement(
                requirement.OptimalMinimum,
                requirement.OptimalMaximum,
                requirement.AcceptableMinimum,
                requirement.AcceptableMaximum,
                requirement.Unit,
                requirement.TimeBasis,
                requirement.IsCompatibleWithSevenDayForecast);

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

    private static bool IsDatabaseReadFailure(Exception exception) =>
        exception is DbException or TimeoutException or RetryLimitExceededException ||
        exception.InnerException is not null && IsDatabaseReadFailure(exception.InnerException);
}
