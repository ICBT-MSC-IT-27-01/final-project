using AnuradhapuraAI.Domain.Common;

namespace AnuradhapuraAI.Application.Suitability;

public sealed class CropSuitabilityEngine : ICropSuitabilityEngine
{
    private const int ForecastHorizonDays = 7;
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

    public SuitabilityResult<SuitabilityEvaluationResponse> Evaluate(SuitabilityEvaluationRequest request)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return SuitabilityResult<SuitabilityEvaluationResponse>.Failure(
                validationError.Value.ErrorCode,
                validationError.Value.Message);
        }

        var results = new List<CropSuitabilityResult>();
        foreach (var profile in request.CropProfiles.OrderBy(profile => Array.IndexOf(ApprovedCropNames.All, profile.CropName)))
        {
            var cropResult = EvaluateCrop(profile, request);
            if (!cropResult.Succeeded)
            {
                return SuitabilityResult<SuitabilityEvaluationResponse>.Failure(
                    cropResult.ErrorCode!,
                    cropResult.Message!);
            }

            results.Add(cropResult.Value!);
        }

        return SuitabilityResult<SuitabilityEvaluationResponse>.Success(new SuitabilityEvaluationResponse(results));
    }

    private static SuitabilityResult<CropSuitabilityResult> EvaluateCrop(
        CropSuitabilityProfile profile,
        SuitabilityEvaluationRequest request)
    {
        var factors = new List<SuitabilityFactorResult>
        {
            EvaluateRainfall(profile.RainfallRequirement, request.Forecast, request.Weights.Rainfall),
            EvaluateWeatherFactor(
                SuitabilityFactors.Temperature,
                profile.TemperatureRequirement,
                request.Forecast.Average(day => day.Temperature),
                request.Weights.Temperature,
                "7-day mean temperature"),
            EvaluateWeatherFactor(
                SuitabilityFactors.Humidity,
                profile.HumidityRequirement,
                request.Forecast.Average(day => day.Humidity),
                request.Weights.Humidity,
                "7-day mean relative humidity"),
            EvaluateSoil(profile, request.SoilType, request.Weights.Soil)
        };

        var evaluableFactors = factors.Where(factor => factor.IsEvaluable).ToList();
        if (evaluableFactors.Count == 0)
        {
            return SuitabilityResult<CropSuitabilityResult>.Failure(
                SuitabilityErrorCodes.InsufficientEvidence,
                $"No suitability factor can be evaluated for {profile.CropName}.");
        }

        var evaluableWeightTotal = evaluableFactors.Sum(factor => factor.ConfiguredWeight);
        if (evaluableWeightTotal <= 0m)
        {
            return SuitabilityResult<CropSuitabilityResult>.Failure(
                SuitabilityErrorCodes.InvalidConfiguration,
                $"Evaluable factor weight total must be greater than zero for {profile.CropName}.");
        }

        var weightedFactors = factors
            .Select(factor => factor.IsEvaluable
                ? factor with { EffectiveWeight = factor.ConfiguredWeight / evaluableWeightTotal }
                : factor)
            .ToList();

        var overallScore = weightedFactors
            .Where(factor => factor.IsEvaluable)
            .Sum(factor => factor.Score!.Value * factor.EffectiveWeight!.Value);

        overallScore = Math.Clamp(overallScore, 0m, 100m);
        var category = MapCategory(overallScore, request.CategoryThresholds);
        var risks = GetClimateRisks(request, weightedFactors);
        var explanation = BuildExplanation(profile.CropName, weightedFactors, overallScore, category, risks);

        return SuitabilityResult<CropSuitabilityResult>.Success(new CropSuitabilityResult(
            profile.CropName,
            overallScore,
            category,
            weightedFactors,
            risks,
            explanation,
            evaluableFactors.Count));
    }

    private static SuitabilityFactorResult EvaluateRainfall(
        SuitabilityRangeRequirement? requirement,
        IReadOnlyList<SuitabilityForecastDay> forecast,
        decimal weight)
    {
        if (requirement is null)
        {
            return NotEvaluable(
                SuitabilityFactors.Rainfall,
                weight,
                "Rainfall was not scored because no approved rainfall requirement was supplied.");
        }

        if (!requirement.IsCompatibleWithSevenDayForecast)
        {
            return NotEvaluable(
                SuitabilityFactors.Rainfall,
                weight,
                $"Rainfall was not scored because the configured time basis '{requirement.TimeBasis}' is not compatible with the 7-day forecast.");
        }

        var totalRainfall = forecast.Sum(day => day.Rainfall);
        return ScoreRange(
            SuitabilityFactors.Rainfall,
            requirement,
            totalRainfall,
            weight,
            "7-day total rainfall");
    }

    private static SuitabilityFactorResult EvaluateWeatherFactor(
        string factor,
        SuitabilityRangeRequirement? requirement,
        decimal aggregatedValue,
        decimal weight,
        string aggregationDescription)
    {
        if (requirement is null)
        {
            return NotEvaluable(
                factor,
                weight,
                $"{factor} was not scored because no approved numeric requirement was supplied.");
        }

        if (!requirement.IsCompatibleWithSevenDayForecast)
        {
            return NotEvaluable(
                factor,
                weight,
                $"{factor} was not scored because the configured time basis '{requirement.TimeBasis}' is not compatible with the 7-day forecast aggregation.");
        }

        return ScoreRange(factor, requirement, aggregatedValue, weight, aggregationDescription);
    }

    private static SuitabilityFactorResult EvaluateSoil(
        CropSuitabilityProfile profile,
        string? selectedSoilType,
        decimal weight)
    {
        if (string.IsNullOrWhiteSpace(selectedSoilType))
        {
            return NotEvaluable(
                SuitabilityFactors.Soil,
                weight,
                "Soil was not scored because no soil type was supplied.");
        }

        var matches = profile.SoilCompatibilities
            .Where(compatibility => string.Equals(
                compatibility.SoilType.Trim(),
                selectedSoilType.Trim(),
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            return NotEvaluable(
                SuitabilityFactors.Soil,
                weight,
                $"Soil was not scored because no configured compatibility exists for '{selectedSoilType}'.");
        }

        if (matches.Count > 1)
        {
            throw new InvalidOperationException($"Duplicate soil compatibility entries exist for {profile.CropName} and '{selectedSoilType}'.");
        }

        var compatibilityScore = matches[0].CompatibilityScore;
        if (compatibilityScore < 0m || compatibilityScore > 100m)
        {
            throw new InvalidOperationException($"Soil compatibility score for {profile.CropName} and '{selectedSoilType}' must be between 0 and 100.");
        }

        return new SuitabilityFactorResult(
            SuitabilityFactors.Soil,
            true,
            compatibilityScore,
            null,
            weight,
            null,
            $"Soil scored {compatibilityScore:0.##} using configured compatibility for '{selectedSoilType}'.");
    }

    private static SuitabilityFactorResult ScoreRange(
        string factor,
        SuitabilityRangeRequirement requirement,
        decimal aggregatedValue,
        decimal weight,
        string aggregationDescription)
    {
        var score = RangeSuitabilityScorer.Score(
            aggregatedValue,
            requirement.AcceptableMinimum,
            requirement.OptimalMinimum,
            requirement.OptimalMaximum,
            requirement.AcceptableMaximum);

        return new SuitabilityFactorResult(
            factor,
            true,
            score,
            aggregatedValue,
            weight,
            null,
            $"{factor} scored {score:0.##} from {aggregationDescription} {aggregatedValue:0.##} {requirement.Unit} against the configured {requirement.TimeBasis} range.");
    }

    private static SuitabilityFactorResult NotEvaluable(string factor, decimal weight, string explanation) =>
        new(factor, false, null, null, weight, null, explanation);

    private static string MapCategory(decimal overallScore, SuitabilityCategoryThresholds thresholds)
    {
        if (overallScore >= thresholds.HighlySuitableMinimum)
        {
            return ApprovedSuitabilityCategories.HighlySuitable;
        }

        if (overallScore >= thresholds.SuitableMinimum)
        {
            return ApprovedSuitabilityCategories.Suitable;
        }

        if (overallScore >= thresholds.ModeratelySuitableMinimum)
        {
            return ApprovedSuitabilityCategories.ModeratelySuitable;
        }

        return ApprovedSuitabilityCategories.Unsuitable;
    }

    private static IReadOnlyList<string> GetClimateRisks(
        SuitabilityEvaluationRequest request,
        IReadOnlyList<SuitabilityFactorResult> factors)
    {
        if (!request.EnableClimateRiskIndicators || request.ClimateRiskThresholds is null)
        {
            return [];
        }

        var risks = new List<string>();
        var rainfall = factors.Single(factor => factor.Factor == SuitabilityFactors.Rainfall);
        var temperature = factors.Single(factor => factor.Factor == SuitabilityFactors.Temperature);

        if (rainfall.IsEvaluable &&
            request.ClimateRiskThresholds.LowRainfallMaximum is { } lowRainfallMaximum &&
            rainfall.AggregatedValue <= lowRainfallMaximum)
        {
            risks.Add(ApprovedClimateRisks.LowRainfall);
        }

        if (rainfall.IsEvaluable &&
            request.ClimateRiskThresholds.HeavyRainfallMinimum is { } heavyRainfallMinimum &&
            rainfall.AggregatedValue >= heavyRainfallMinimum)
        {
            risks.Add(ApprovedClimateRisks.HeavyRainfall);
        }

        if (temperature.IsEvaluable &&
            request.ClimateRiskThresholds.HighTemperatureMinimum is { } highTemperatureMinimum &&
            temperature.AggregatedValue >= highTemperatureMinimum)
        {
            risks.Add(ApprovedClimateRisks.HighTemperature);
        }

        return risks;
    }

    private static string BuildExplanation(
        string cropName,
        IReadOnlyList<SuitabilityFactorResult> factors,
        decimal overallScore,
        string category,
        IReadOnlyList<string> risks)
    {
        var evaluated = factors.Where(factor => factor.IsEvaluable).Select(factor => factor.Factor);
        var unavailable = factors.Where(factor => !factor.IsEvaluable).Select(factor => factor.Factor);
        var risksText = risks.Count == 0 ? "No supported climate risks were emitted." : $"Climate risks: {string.Join(", ", risks)}.";

        return $"{cropName}: evaluated factors: {string.Join(", ", evaluated)}. " +
            $"Unavailable factors: {string.Join(", ", unavailable.DefaultIfEmpty("None"))}. " +
            $"Weights were re-normalized across evaluable factors. Overall score {overallScore:0.##}; category {category}. " +
            risksText;
    }

    private static (string ErrorCode, string Message)? ValidateRequest(SuitabilityEvaluationRequest request)
    {
        if (request.Forecast.Count != ForecastHorizonDays)
        {
            return (SuitabilityErrorCodes.InvalidInput, "Suitability evaluation requires exactly seven forecast days.");
        }

        var orderedDates = request.Forecast.Select(day => day.TargetDate).ToList();
        if (orderedDates.Distinct().Count() != ForecastHorizonDays)
        {
            return (SuitabilityErrorCodes.InvalidInput, "Forecast target dates must not contain duplicates.");
        }

        for (var i = 1; i < orderedDates.Count; i++)
        {
            if (orderedDates[i] != orderedDates[i - 1].AddDays(1))
            {
                return (SuitabilityErrorCodes.InvalidInput, "Forecast target dates must be chronological and daily without gaps.");
            }
        }

        if (request.Forecast.Any(day =>
            !IsFinite(day.Rainfall) ||
            !IsFinite(day.Temperature) ||
            !IsFinite(day.Humidity)))
        {
            return (SuitabilityErrorCodes.InvalidInput, "Forecast weather values must be finite numeric values.");
        }

        if (!ValidateCropScope(request.CropProfiles, out var cropScopeError))
        {
            return (SuitabilityErrorCodes.InvalidInput, cropScopeError);
        }

        if (request.Weights.Rainfall < 0m ||
            request.Weights.Temperature < 0m ||
            request.Weights.Humidity < 0m ||
            request.Weights.Soil < 0m)
        {
            return (SuitabilityErrorCodes.InvalidConfiguration, "Suitability factor weights must be non-negative.");
        }

        if (request.CategoryThresholds.HighlySuitableMinimum < request.CategoryThresholds.SuitableMinimum ||
            request.CategoryThresholds.SuitableMinimum < request.CategoryThresholds.ModeratelySuitableMinimum)
        {
            return (SuitabilityErrorCodes.InvalidConfiguration, "Suitability category thresholds must be ordered Highly >= Suitable >= Moderately.");
        }

        foreach (var profile in request.CropProfiles)
        {
            var profileError = ValidateProfile(profile);
            if (profileError is not null)
            {
                return profileError.Value;
            }
        }

        return null;
    }

    private static bool ValidateCropScope(
        IReadOnlyList<CropSuitabilityProfile> profiles,
        out string error)
    {
        error = string.Empty;

        if (profiles.Count != ApprovedCropNames.All.Length)
        {
            error = "Suitability evaluation requires exactly the six approved MSc crops.";
            return false;
        }

        var duplicate = profiles
            .GroupBy(profile => profile.CropName.Trim(), NameComparer)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            error = $"Duplicate crop profile supplied for '{duplicate.Key}'.";
            return false;
        }

        var supplied = profiles.Select(profile => profile.CropName.Trim()).ToHashSet(NameComparer);
        var approved = ApprovedCropNames.All.ToHashSet(NameComparer);
        if (!supplied.SetEquals(approved))
        {
            error = "Crop profiles must match the approved MSc crop scope exactly.";
            return false;
        }

        return true;
    }

    private static (string ErrorCode, string Message)? ValidateProfile(CropSuitabilityProfile profile)
    {
        foreach (var requirement in new[]
                 {
                     profile.RainfallRequirement,
                     profile.TemperatureRequirement,
                     profile.HumidityRequirement
                 }.Where(requirement => requirement is not null))
        {
            if (requirement!.AcceptableMinimum > requirement.AcceptableMaximum ||
                requirement.OptimalMinimum > requirement.OptimalMaximum)
            {
                return (SuitabilityErrorCodes.InvalidConfiguration, $"{profile.CropName} requirement minimum values must be less than or equal to maximum values.");
            }

            if (requirement.AcceptableMinimum > requirement.OptimalMinimum ||
                requirement.OptimalMaximum > requirement.AcceptableMaximum)
            {
                return (SuitabilityErrorCodes.InvalidConfiguration, $"{profile.CropName} requirement boundaries must satisfy acceptableMin <= optimalMin <= optimalMax <= acceptableMax.");
            }
        }

        var duplicateSoil = profile.SoilCompatibilities
            .GroupBy(compatibility => compatibility.SoilType.Trim(), NameComparer)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateSoil is not null)
        {
            return (SuitabilityErrorCodes.InvalidConfiguration, $"Duplicate soil compatibility configuration exists for {profile.CropName} and '{duplicateSoil.Key}'.");
        }

        if (profile.SoilCompatibilities.Any(compatibility =>
            compatibility.CompatibilityScore < 0m ||
            compatibility.CompatibilityScore > 100m))
        {
            return (SuitabilityErrorCodes.InvalidConfiguration, $"Soil compatibility scores for {profile.CropName} must be between 0 and 100.");
        }

        return null;
    }

    private static bool IsFinite(decimal value) =>
        value != decimal.MinValue && value != decimal.MaxValue;
}
