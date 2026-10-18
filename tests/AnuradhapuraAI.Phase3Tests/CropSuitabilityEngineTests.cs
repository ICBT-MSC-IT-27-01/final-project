using AnuradhapuraAI.Application.Suitability;
using AnuradhapuraAI.Domain.Common;

namespace AnuradhapuraAI.Phase3Tests;

public sealed class CropSuitabilityEngineTests
{
    private readonly CropSuitabilityEngine _engine = new();

    [Fact]
    public void RangeScoring_ReturnsOneHundredInsideOptimalRange()
    {
        var score = RangeSuitabilityScorer.Score(25m, 10m, 20m, 30m, 40m);

        Assert.Equal(100m, score);
    }

    [Fact]
    public void RangeScoring_ReturnsLinearLowerTransition()
    {
        var score = RangeSuitabilityScorer.Score(15m, 10m, 20m, 30m, 40m);

        Assert.Equal(50m, score);
    }

    [Fact]
    public void RangeScoring_ReturnsLinearUpperTransition()
    {
        var score = RangeSuitabilityScorer.Score(35m, 10m, 20m, 30m, 40m);

        Assert.Equal(50m, score);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(41)]
    public void RangeScoring_ReturnsZeroOutsideAcceptableRange(decimal value)
    {
        var score = RangeSuitabilityScorer.Score(value, 10m, 20m, 30m, 40m);

        Assert.Equal(0m, score);
    }

    [Theory]
    [InlineData(10, 0)]
    [InlineData(20, 100)]
    [InlineData(30, 100)]
    [InlineData(40, 0)]
    public void RangeScoring_HandlesExactBoundaries(decimal value, decimal expectedScore)
    {
        var score = RangeSuitabilityScorer.Score(value, 10m, 20m, 30m, 40m);

        Assert.Equal(expectedScore, score);
    }

    [Fact]
    public void RangeScoring_HandlesDegenerateBoundariesSafely()
    {
        var score = RangeSuitabilityScorer.Score(20m, 20m, 20m, 30m, 40m);

        Assert.Equal(100m, score);
    }

    [Theory]
    [InlineData(20, 0)]
    [InlineData(25, 100)]
    [InlineData(30, 100)]
    [InlineData(35, 0)]
    public void RangeScoring_StrictBandReturnsOneHundredOnlyInsideInclusiveBand(decimal value, decimal expectedScore)
    {
        var score = RangeSuitabilityScorer.Score(value, 25m, 25m, 30m, 30m);

        Assert.Equal(expectedScore, score);
    }

    [Fact]
    public void Evaluate_UsesSevenDayTemperatureMean()
    {
        var result = _engine.Evaluate(DefaultRequest());

        var paddy = AssertSuccess(result).Crops.Single(crop => crop.CropName == ApprovedCropNames.Paddy);
        Assert.Equal(26m, paddy.Temperature.AggregatedValue);
        Assert.Equal(100m, paddy.Temperature.Score);
    }

    [Fact]
    public void Evaluate_UsesSevenDayHumidityMeanWhenRequirementIsCompatible()
    {
        var result = _engine.Evaluate(DefaultRequest());

        var paddy = AssertSuccess(result).Crops.Single(crop => crop.CropName == ApprovedCropNames.Paddy);
        Assert.Equal(70m, paddy.Humidity.AggregatedValue);
        Assert.Equal(100m, paddy.Humidity.Score);
    }

    [Fact]
    public void Evaluate_DoesNotScoreRainfallWhenTimeBasisIsIncompatible()
    {
        var result = _engine.Evaluate(DefaultRequest(rainfallCompatible: false));

        var paddy = AssertSuccess(result).Crops.Single(crop => crop.CropName == ApprovedCropNames.Paddy);
        Assert.False(paddy.Rainfall.IsEvaluable);
        Assert.Null(paddy.Rainfall.Score);
        Assert.Contains("not compatible", paddy.Rainfall.Explanation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_AllFourFactorsEvaluable_UsesConfiguredWeights()
    {
        var result = _engine.Evaluate(DefaultRequest());

        var paddy = AssertSuccess(result).Crops.Single(crop => crop.CropName == ApprovedCropNames.Paddy);
        Assert.Equal(4, paddy.EvaluatedFactorCount);
        Assert.All(paddy.Factors, factor => Assert.Equal(0.25m, factor.EffectiveWeight));
        Assert.Equal(95m, paddy.OverallScore);
    }

    [Fact]
    public void Evaluate_OneUnavailableFactor_RenormalizesWeights()
    {
        var result = _engine.Evaluate(DefaultRequest(rainfallCompatible: false));

        var paddy = AssertSuccess(result).Crops.Single(crop => crop.CropName == ApprovedCropNames.Paddy);
        Assert.Equal(3, paddy.EvaluatedFactorCount);
        Assert.Null(paddy.Rainfall.EffectiveWeight);
        Assert.Equal(1m / 3m, paddy.Temperature.EffectiveWeight);
        Assert.Equal(93.33333333333333333333333332m, paddy.OverallScore);
    }

    [Fact]
    public void Evaluate_TwoUnavailableFactors_RenormalizesWeights()
    {
        var result = _engine.Evaluate(DefaultRequest(rainfallCompatible: false, omitHumidityRequirement: true));

        var paddy = AssertSuccess(result).Crops.Single(crop => crop.CropName == ApprovedCropNames.Paddy);
        Assert.Equal(2, paddy.EvaluatedFactorCount);
        Assert.Equal(0.5m, paddy.Temperature.EffectiveWeight);
        Assert.Equal(0.5m, paddy.Soil.EffectiveWeight);
    }

    [Fact]
    public void Evaluate_ThreeUnavailableFactors_RenormalizesToSingleEvaluableFactor()
    {
        var result = _engine.Evaluate(DefaultRequest(
            rainfallCompatible: false,
            omitHumidityRequirement: true,
            soilType: null));

        var paddy = AssertSuccess(result).Crops.Single(crop => crop.CropName == ApprovedCropNames.Paddy);
        Assert.Equal(1, paddy.EvaluatedFactorCount);
        Assert.Equal(1m, paddy.Temperature.EffectiveWeight);
        Assert.Equal(100m, paddy.OverallScore);
    }

    [Fact]
    public void Evaluate_NoEvaluableFactors_ReturnsInsufficientEvidence()
    {
        var request = DefaultRequest(
            rainfallCompatible: false,
            omitHumidityRequirement: true,
            soilType: null,
            omitTemperatureRequirement: true);

        var result = _engine.Evaluate(request);

        Assert.False(result.Succeeded);
        Assert.Equal(SuitabilityErrorCodes.InsufficientEvidence, result.ErrorCode);
    }

    [Fact]
    public void Evaluate_NonEqualConfiguredWeights_RenormalizesEvaluableWeights()
    {
        var result = _engine.Evaluate(DefaultRequest(
            rainfallCompatible: false,
            omitHumidityRequirement: true,
            weights: new SuitabilityFactorWeights(Rainfall: 10m, Temperature: 70m, Humidity: 10m, Soil: 20m)));

        var paddy = AssertSuccess(result).Crops.Single(crop => crop.CropName == ApprovedCropNames.Paddy);
        Assert.Equal(70m / 90m, paddy.Temperature.EffectiveWeight);
        Assert.Equal(20m / 90m, paddy.Soil.EffectiveWeight);
    }

    [Fact]
    public void Evaluate_Phase15ReferenceExample_MatchesIndependentWeightedCalculation()
    {
        var request = DefaultRequest(
            soilScore: 60m,
            temperatureRequirement: new SuitabilityRangeRequirement(24m, 28m, 20m, 32m, "deg C", "synthetic 7-day mean", true),
            humidityRequirement: new SuitabilityRangeRequirement(65m, 75m, 50m, 90m, "%", "synthetic 7-day mean", true),
            weights: new SuitabilityFactorWeights(Rainfall: 40m, Temperature: 30m, Humidity: 20m, Soil: 10m));

        var result = _engine.Evaluate(request);

        var paddy = AssertSuccess(result).Crops.Single(crop => crop.CropName == ApprovedCropNames.Paddy);
        var expectedRainfall = 100m; // 7-day total is 7 mm inside the synthetic optimal 5-8 mm range.
        var expectedTemperature = 100m; // 7-day mean is 26 deg C inside the synthetic optimal 24-28 deg C range.
        var expectedHumidity = 100m; // 7-day mean is 70% inside the synthetic optimal 65-75% range.
        var expectedSoil = 60m;
        var expectedOverall = (expectedRainfall * 0.4m) +
            (expectedTemperature * 0.3m) +
            (expectedHumidity * 0.2m) +
            (expectedSoil * 0.1m);

        Assert.Equal(expectedOverall, paddy.OverallScore);
        Assert.Equal(96m, paddy.OverallScore);
        Assert.Equal(ApprovedSuitabilityCategories.HighlySuitable, paddy.SuitabilityCategory);
        Assert.Equal(4, paddy.EvaluatedFactorCount);
    }

    [Fact]
    public void Evaluate_AnnualScientificContextValuesRemainUnscoredForSevenDayForecast()
    {
        var annualRainfallContext = new SuitabilityRangeRequirement(
            OptimalMinimum: 600m,
            OptimalMaximum: 1200m,
            AcceptableMinimum: 400m,
            AcceptableMaximum: 1800m,
            Unit: "mm/year",
            TimeBasis: ApprovedTimeBases.Annual,
            IsCompatibleWithSevenDayForecast: false);

        var result = _engine.Evaluate(DefaultRequest(
            profiles: DefaultProfiles(
                rainfallRequirement: annualRainfallContext,
                temperatureRequirement: null,
                humidityRequirement: null,
                soilCompatibilities: [],
                keepNullRequirements: true),
            soilType: null,
            allowInsufficientEvidenceResults: true));

        var response = AssertSuccess(result);
        Assert.All(response.Crops, crop =>
        {
            Assert.Null(crop.OverallScore);
            Assert.Null(crop.SuitabilityCategory);
            Assert.Equal(0, crop.EvaluatedFactorCount);
            Assert.False(crop.Rainfall.IsEvaluable);
            Assert.Contains("annual", crop.Rainfall.Explanation, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Evaluate_RepeatedScientificFixtureRunsAreDeterministic()
    {
        var request = DefaultRequest();

        var first = AssertSuccess(_engine.Evaluate(request));
        var second = AssertSuccess(_engine.Evaluate(request));

        Assert.Equal(
            first.Crops.Select(crop => (crop.CropName, crop.OverallScore, crop.SuitabilityCategory, crop.Explanation)).ToArray(),
            second.Crops.Select(crop => (crop.CropName, crop.OverallScore, crop.SuitabilityCategory, crop.Explanation)).ToArray());
    }

    [Fact]
    public void Evaluate_ControlledRecommendationBatchCompletesWithinPrototypeBudget()
    {
        var request = DefaultRequest();
        var startedAt = DateTimeOffset.UtcNow;

        for (var index = 0; index < 100; index++)
        {
            AssertSuccess(_engine.Evaluate(request));
        }

        var elapsed = DateTimeOffset.UtcNow - startedAt;
        Assert.True(elapsed < TimeSpan.FromSeconds(2), $"Suitability scoring took {elapsed.TotalMilliseconds:0.##} ms for 100 runs.");
    }

    [Theory]
    [InlineData(80, "Highly Suitable")]
    [InlineData(79.99, "Suitable")]
    [InlineData(60, "Suitable")]
    [InlineData(59.99, "Moderately Suitable")]
    [InlineData(40, "Moderately Suitable")]
    [InlineData(39.99, "Unsuitable")]
    public void Evaluate_MapsCategoryThresholdBoundaries(decimal soilScore, string expectedCategory)
    {
        var result = _engine.Evaluate(DefaultRequest(
            rainfallCompatible: false,
            omitHumidityRequirement: true,
            omitTemperatureRequirement: true,
            soilScore: soilScore));

        var paddy = AssertSuccess(result).Crops.Single(crop => crop.CropName == ApprovedCropNames.Paddy);
        Assert.Equal(expectedCategory, paddy.SuitabilityCategory);
    }

    [Fact]
    public void Evaluate_ScoresMatchingConfiguredSoil()
    {
        var result = _engine.Evaluate(DefaultRequest(soilScore: 75m));

        var paddy = AssertSuccess(result).Crops.Single(crop => crop.CropName == ApprovedCropNames.Paddy);
        Assert.True(paddy.Soil.IsEvaluable);
        Assert.Equal(75m, paddy.Soil.Score);
    }

    [Fact]
    public void Evaluate_MissingSoilCompatibilityIsNotEvaluable()
    {
        var result = _engine.Evaluate(DefaultRequest(soilType: "Unconfigured soil"));

        var paddy = AssertSuccess(result).Crops.Single(crop => crop.CropName == ApprovedCropNames.Paddy);
        Assert.False(paddy.Soil.IsEvaluable);
        Assert.Null(paddy.Soil.Score);
    }

    [Fact]
    public void Evaluate_InvalidCompatibilityScoreFailsControlledValidation()
    {
        var result = _engine.Evaluate(DefaultRequest(soilScore: 101m));

        Assert.False(result.Succeeded);
        Assert.Equal(SuitabilityErrorCodes.InvalidConfiguration, result.ErrorCode);
    }

    [Fact]
    public void Evaluate_InvalidWeightsFailValidation()
    {
        var result = _engine.Evaluate(DefaultRequest(
            weights: new SuitabilityFactorWeights(Rainfall: -1m, Temperature: 25m, Humidity: 25m, Soil: 25m)));

        Assert.False(result.Succeeded);
        Assert.Equal(SuitabilityErrorCodes.InvalidConfiguration, result.ErrorCode);
    }

    [Fact]
    public void Evaluate_IncorrectlyOrderedThresholdsFailValidation()
    {
        var result = _engine.Evaluate(DefaultRequest(
            thresholds: new SuitabilityCategoryThresholds(HighlySuitableMinimum: 60m, SuitableMinimum: 80m, ModeratelySuitableMinimum: 40m)));

        Assert.False(result.Succeeded);
        Assert.Equal(SuitabilityErrorCodes.InvalidConfiguration, result.ErrorCode);
    }

    [Fact]
    public void Evaluate_InvalidRequirementRangesFailValidation()
    {
        var result = _engine.Evaluate(DefaultRequest(
            temperatureRequirement: new SuitabilityRangeRequirement(30m, 20m, 10m, 40m, "test", "synthetic test", true)));

        Assert.False(result.Succeeded);
        Assert.Equal(SuitabilityErrorCodes.InvalidConfiguration, result.ErrorCode);
    }

    [Fact]
    public void Evaluate_ConflictingDuplicateSoilConfigurationFailsValidation()
    {
        var profiles = DefaultProfiles(soilCompatibilities: [
            new SuitabilitySoilCompatibility("Synthetic loam", 70m),
            new SuitabilitySoilCompatibility("Synthetic loam", 80m)
        ]);

        var result = _engine.Evaluate(DefaultRequest(profiles: profiles));

        Assert.False(result.Succeeded);
        Assert.Equal(SuitabilityErrorCodes.InvalidConfiguration, result.ErrorCode);
    }

    [Fact]
    public void Evaluate_ExplanationIdentifiesEvaluatedAndUnavailableFactors()
    {
        var result = _engine.Evaluate(DefaultRequest(rainfallCompatible: false, omitHumidityRequirement: true));

        var paddy = AssertSuccess(result).Crops.Single(crop => crop.CropName == ApprovedCropNames.Paddy);
        Assert.Contains("evaluated factors: Temperature, Soil", paddy.Explanation);
        Assert.Contains("Unavailable factors: Rainfall, Humidity", paddy.Explanation);
    }

    [Fact]
    public void Evaluate_ClimateRiskFeatureDisabledReturnsNoRisks()
    {
        var result = _engine.Evaluate(DefaultRequest(
            enableClimateRisks: false,
            climateRiskThresholds: new ClimateRiskThresholds(LowRainfallMaximum: 10m, HeavyRainfallMinimum: 20m, HighTemperatureMinimum: 25m)));

        var paddy = AssertSuccess(result).Crops.Single(crop => crop.CropName == ApprovedCropNames.Paddy);
        Assert.Empty(paddy.ClimateRisks);
    }

    [Fact]
    public void Evaluate_SupportedConfiguredRisksAreEmitted()
    {
        var result = _engine.Evaluate(DefaultRequest(
            climateRiskThresholds: new ClimateRiskThresholds(LowRainfallMaximum: 10m, HeavyRainfallMinimum: 6m, HighTemperatureMinimum: 25m)));

        var paddy = AssertSuccess(result).Crops.Single(crop => crop.CropName == ApprovedCropNames.Paddy);
        Assert.Contains(ApprovedClimateRisks.HeavyRainfall, paddy.ClimateRisks);
        Assert.Contains(ApprovedClimateRisks.HighTemperature, paddy.ClimateRisks);
    }

    [Fact]
    public void Evaluate_UnsupportedRisksAreNotFabricated()
    {
        var result = _engine.Evaluate(DefaultRequest(climateRiskThresholds: null));

        var paddy = AssertSuccess(result).Crops.Single(crop => crop.CropName == ApprovedCropNames.Paddy);
        Assert.Empty(paddy.ClimateRisks);
    }

    [Fact]
    public void Evaluate_RequiresExactlySixApprovedCrops()
    {
        var profiles = DefaultProfiles().Take(5).ToList();

        var result = _engine.Evaluate(DefaultRequest(profiles: profiles));

        Assert.False(result.Succeeded);
        Assert.Equal(SuitabilityErrorCodes.InvalidInput, result.ErrorCode);
    }

    [Fact]
    public void Evaluate_RejectsAccidentalScopeExpansion()
    {
        var profiles = DefaultProfiles().Append(DefaultProfile("Extra Crop")).ToList();

        var result = _engine.Evaluate(DefaultRequest(profiles: profiles));

        Assert.False(result.Succeeded);
        Assert.Equal(SuitabilityErrorCodes.InvalidInput, result.ErrorCode);
    }

    private static SuitabilityEvaluationResponse AssertSuccess(SuitabilityResult<SuitabilityEvaluationResponse> result)
    {
        Assert.True(result.Succeeded, result.Message);
        return result.Value!;
    }

    private static SuitabilityEvaluationRequest DefaultRequest(
        bool rainfallCompatible = true,
        string? soilType = "Synthetic loam",
        decimal soilScore = 80m,
        SuitabilityRangeRequirement? temperatureRequirement = null,
        SuitabilityRangeRequirement? humidityRequirement = null,
        bool omitTemperatureRequirement = false,
        bool omitHumidityRequirement = false,
        SuitabilityFactorWeights? weights = null,
        SuitabilityCategoryThresholds? thresholds = null,
        bool enableClimateRisks = true,
        ClimateRiskThresholds? climateRiskThresholds = null,
        bool allowInsufficientEvidenceResults = false,
        IReadOnlyList<CropSuitabilityProfile>? profiles = null)
    {
        temperatureRequirement = omitTemperatureRequirement
            ? null
            : temperatureRequirement ?? new SuitabilityRangeRequirement(20m, 30m, 10m, 40m, "synthetic-test-unit", "synthetic 7-day mean", true);
        humidityRequirement = omitHumidityRequirement
            ? null
            : humidityRequirement ?? new SuitabilityRangeRequirement(60m, 80m, 40m, 90m, "synthetic-test-unit", "synthetic 7-day mean", true);
        var rainfallRequirement = new SuitabilityRangeRequirement(5m, 8m, 0m, 15m, "synthetic-test-unit", rainfallCompatible ? "synthetic 7-day total" : "annual", rainfallCompatible);

        return new SuitabilityEvaluationRequest(
            Forecast: DefaultForecast(),
            SoilType: soilType,
            CropProfiles: profiles ?? DefaultProfiles(
                rainfallRequirement,
                temperatureRequirement,
                humidityRequirement,
                [new SuitabilitySoilCompatibility("Synthetic loam", soilScore)],
                keepNullRequirements: true),
            Weights: weights ?? new SuitabilityFactorWeights(),
            CategoryThresholds: thresholds ?? new SuitabilityCategoryThresholds(),
            EnableClimateRiskIndicators: enableClimateRisks,
            ClimateRiskThresholds: climateRiskThresholds,
            AllowInsufficientEvidenceResults: allowInsufficientEvidenceResults);
    }

    private static IReadOnlyList<SuitabilityForecastDay> DefaultForecast()
    {
        var startDate = new DateOnly(2025, 1, 1);
        return Enumerable.Range(0, 7)
            .Select(offset => new SuitabilityForecastDay(
                startDate.AddDays(offset),
                Rainfall: 1m,
                Temperature: 23m + offset,
                Humidity: 67m + offset))
            .ToList();
    }

    private static IReadOnlyList<CropSuitabilityProfile> DefaultProfiles(
        SuitabilityRangeRequirement? rainfallRequirement = null,
        SuitabilityRangeRequirement? temperatureRequirement = null,
        SuitabilityRangeRequirement? humidityRequirement = null,
        IReadOnlyList<SuitabilitySoilCompatibility>? soilCompatibilities = null,
        bool keepNullRequirements = false)
    {
        return ApprovedCropNames.All
            .Select(crop => DefaultProfile(
                crop,
                rainfallRequirement,
                temperatureRequirement,
                humidityRequirement,
                soilCompatibilities,
                keepNullRequirements))
            .ToList();
    }

    private static CropSuitabilityProfile DefaultProfile(
        string cropName,
        SuitabilityRangeRequirement? rainfallRequirement = null,
        SuitabilityRangeRequirement? temperatureRequirement = null,
        SuitabilityRangeRequirement? humidityRequirement = null,
        IReadOnlyList<SuitabilitySoilCompatibility>? soilCompatibilities = null,
        bool keepNullRequirements = false)
    {
        return new CropSuitabilityProfile(
            cropName,
            keepNullRequirements
                ? rainfallRequirement
                : rainfallRequirement ?? new SuitabilityRangeRequirement(5m, 8m, 0m, 15m, "synthetic-test-unit", "synthetic 7-day total", true),
            keepNullRequirements
                ? temperatureRequirement
                : temperatureRequirement ?? new SuitabilityRangeRequirement(20m, 30m, 10m, 40m, "synthetic-test-unit", "synthetic 7-day mean", true),
            keepNullRequirements
                ? humidityRequirement
                : humidityRequirement ?? new SuitabilityRangeRequirement(60m, 80m, 40m, 90m, "synthetic-test-unit", "synthetic 7-day mean", true),
            soilCompatibilities ?? [new SuitabilitySoilCompatibility("Synthetic loam", 80m)]);
    }
}
