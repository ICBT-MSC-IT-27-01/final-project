using System.Data.Common;
using System.Text.Json;
using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Application.Recommendations;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace AnuradhapuraAI.Infrastructure.Recommendations;

public sealed class RecommendationHistoryService(
    AnuradhapuraAiDbContext dbContext,
    IOptions<WeatherModelFeatureOptions> featureOptions) : IRecommendationHistoryService
{
    private const int MaximumPageSize = 50;

    public async Task<RecommendationHistoryResult<RecommendationHistoryPageResponse>> ListHistoryAsync(
        RecommendationHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!featureOptions.Value.EnableRecommendationHistory)
        {
            return RecommendationHistoryResult<RecommendationHistoryPageResponse>.Failure(
                RecommendationHistoryErrorCodes.FeatureDisabled,
                "Recommendation history is disabled.");
        }

        var pagingError = ValidatePaging(query.Page, query.PageSize);
        if (pagingError is not null)
        {
            return RecommendationHistoryResult<RecommendationHistoryPageResponse>.Failure(
                RecommendationHistoryErrorCodes.InvalidPaging,
                pagingError);
        }

        if (!await IsActiveRegisteredUserAsync(query.TrustedRegisteredUserId, cancellationToken))
        {
            return RecommendationHistoryResult<RecommendationHistoryPageResponse>.Failure(
                RecommendationHistoryErrorCodes.InvalidUserContext,
                "Registered user context is invalid.");
        }

        try
        {
            var baseQuery = dbContext.Recommendations
                .AsNoTracking()
                .Where(recommendation => recommendation.UserId == query.TrustedRegisteredUserId);
            var totalCount = await baseQuery.CountAsync(cancellationToken);
            var recommendations = await baseQuery
                .Include(recommendation => recommendation.RecommendationCrops)
                    .ThenInclude(crop => crop.Crop)
                .OrderByDescending(recommendation => recommendation.CreatedAt)
                .ThenByDescending(recommendation => recommendation.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var items = recommendations.Select(ToSummary).ToList();
            return RecommendationHistoryResult<RecommendationHistoryPageResponse>.Success(
                new RecommendationHistoryPageResponse(items, query.Page, query.PageSize, totalCount));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsReadFailure(ex))
        {
            return RecommendationHistoryResult<RecommendationHistoryPageResponse>.Failure(
                RecommendationHistoryErrorCodes.DatabaseReadFailed,
                "Recommendation history is temporarily unavailable.");
        }
    }

    public async Task<RecommendationHistoryResult<RecommendationHistoryDetailResponse>> GetDetailAsync(
        int recommendationId,
        int trustedRegisteredUserId,
        CancellationToken cancellationToken = default)
    {
        if (!featureOptions.Value.EnableRecommendationHistory)
        {
            return RecommendationHistoryResult<RecommendationHistoryDetailResponse>.Failure(
                RecommendationHistoryErrorCodes.FeatureDisabled,
                "Recommendation history is disabled.");
        }

        if (!await IsActiveRegisteredUserAsync(trustedRegisteredUserId, cancellationToken))
        {
            return RecommendationHistoryResult<RecommendationHistoryDetailResponse>.Failure(
                RecommendationHistoryErrorCodes.InvalidUserContext,
                "Registered user context is invalid.");
        }

        try
        {
            var recommendation = await dbContext.Recommendations
                .AsNoTracking()
                .Include(item => item.RecommendationCrops)
                    .ThenInclude(crop => crop.Crop)
                .Where(item => item.Id == recommendationId && item.UserId == trustedRegisteredUserId)
                .SingleOrDefaultAsync(cancellationToken);

            return recommendation is null
                ? RecommendationHistoryResult<RecommendationHistoryDetailResponse>.Failure(
                    RecommendationHistoryErrorCodes.RecommendationNotFound,
                    "Recommendation was not found.")
                : RecommendationHistoryResult<RecommendationHistoryDetailResponse>.Success(ToDetail(recommendation));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsReadFailure(ex))
        {
            return RecommendationHistoryResult<RecommendationHistoryDetailResponse>.Failure(
                RecommendationHistoryErrorCodes.DatabaseReadFailed,
                "Recommendation history is temporarily unavailable.");
        }
    }

    private async Task<bool> IsActiveRegisteredUserAsync(int userId, CancellationToken cancellationToken) =>
        await dbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .AnyAsync(
                user => user.Id == userId &&
                    user.IsActive &&
                    user.Role != null &&
                    user.Role.Name == ApprovedRoleNames.RegisteredUser,
                cancellationToken);

    private static string? ValidatePaging(int page, int pageSize)
    {
        if (page < 1)
        {
            return "Page must be greater than zero.";
        }

        return pageSize is < 1 or > MaximumPageSize
            ? $"Page size must be between 1 and {MaximumPageSize}."
            : null;
    }

    private static RecommendationHistorySummaryResponse ToSummary(Recommendation recommendation)
    {
        var forecast = ReadForecastSnapshot(recommendation.EvidenceSnapshotJson, recommendation.ForecastRunId);
        var topCrop = recommendation.RecommendationCrops
            .Where(crop => crop.Rank.HasValue)
            .OrderBy(crop => crop.Rank)
            .ThenBy(crop => crop.CropId)
            .FirstOrDefault();

        return new RecommendationHistorySummaryResponse(
            recommendation.Id,
            recommendation.ForecastRunId,
            recommendation.SoilType,
            recommendation.CreatedAt,
            forecast.ForecastDate,
            forecast.TargetStartDate,
            forecast.TargetEndDate,
            forecast.ModelVersion,
            recommendation.RecommendationCrops.Count,
            topCrop?.Crop?.Name,
            topCrop?.OverallScore,
            topCrop?.SuitabilityCategory);
    }

    private static RecommendationHistoryDetailResponse ToDetail(Recommendation recommendation)
    {
        var forecast = ReadForecastSnapshot(recommendation.EvidenceSnapshotJson, recommendation.ForecastRunId);
        var crops = recommendation.RecommendationCrops
            .OrderBy(crop => crop.Rank ?? int.MaxValue)
            .ThenBy(crop => ApprovedCropOrder(crop.Crop?.Name ?? string.Empty))
            .ThenBy(crop => crop.CropId)
            .Select(ToCrop)
            .ToList();

        return new RecommendationHistoryDetailResponse(
            recommendation.Id,
            recommendation.ForecastRunId,
            recommendation.SoilType,
            recommendation.CreatedAt,
            forecast,
            recommendation.EvidenceSnapshotJson,
            crops,
            [
                "Historical results are returned from stored recommendation evidence and are not recalculated.",
                "Unavailable factor values remain unavailable and are not replaced with fabricated scores."
            ]);
    }

    private static RecommendationHistoryCropResponse ToCrop(RecommendationCrop crop)
    {
        var snapshot = ReadCropSnapshot(crop.EvidenceSnapshotJson);
        return new RecommendationHistoryCropResponse(
            crop.CropId,
            crop.Crop?.Name ?? snapshot.CropName ?? $"Crop {crop.CropId}",
            crop.EvaluationStatus,
            crop.RainfallScore,
            crop.TemperatureScore,
            crop.HumidityScore,
            crop.SoilScore,
            crop.OverallScore,
            crop.SuitabilityCategory,
            crop.Rank,
            snapshot.UnavailableFactors,
            snapshot.Factors,
            snapshot.ClimateRisks,
            crop.Explanation,
            crop.EvidenceSnapshotJson);
    }

    private static RecommendationHistoryForecastResponse ReadForecastSnapshot(string evidenceSnapshotJson, Guid fallbackForecastRunId)
    {
        try
        {
            using var document = JsonDocument.Parse(evidenceSnapshotJson);
            if (!document.RootElement.TryGetProperty("forecast", out var forecast))
            {
                return EmptyForecast(fallbackForecastRunId);
            }

            var forecastRunId = ReadGuid(forecast, "forecastRunId") ?? fallbackForecastRunId;
            return new RecommendationHistoryForecastResponse(
                forecastRunId,
                ReadDateOnly(forecast, "forecastDate"),
                ReadDateOnly(forecast, "targetStartDate"),
                ReadDateOnly(forecast, "targetEndDate"),
                ReadString(forecast, "modelVersion"),
                ReadDateTimeOffset(forecast, "createdAt"),
                ReadForecastDays(forecast));
        }
        catch (JsonException)
        {
            return EmptyForecast(fallbackForecastRunId);
        }
    }

    private static RecommendationHistoryForecastResponse EmptyForecast(Guid forecastRunId) =>
        new(forecastRunId, null, null, null, null, null, []);

    private static CropSnapshot ReadCropSnapshot(string evidenceSnapshotJson)
    {
        try
        {
            using var document = JsonDocument.Parse(evidenceSnapshotJson);
            var root = document.RootElement;
            var cropName = root.TryGetProperty("crop", out var cropElement)
                ? ReadString(cropElement, "name")
                : null;
            return new CropSnapshot(
                cropName,
                ReadStringArray(root, "unavailableFactors"),
                ReadFactors(root),
                ReadStringArray(root, "climateRisks"));
        }
        catch (JsonException)
        {
            return new CropSnapshot(null, [], [], []);
        }
    }

    private static IReadOnlyList<RecommendationHistoryForecastDayResponse> ReadForecastDays(JsonElement forecast)
    {
        if (!forecast.TryGetProperty("days", out var days) || days.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return days.EnumerateArray()
            .Select(day => new RecommendationHistoryForecastDayResponse(
                ReadDateOnly(day, "targetDate"),
                ReadNestedDecimal(day, "rainfall", "value"),
                ReadNestedDecimal(day, "temperature", "value"),
                ReadNestedDecimal(day, "humidity", "value")))
            .ToList();
    }

    private static IReadOnlyList<RecommendationHistoryFactorResponse> ReadFactors(JsonElement root)
    {
        if (!root.TryGetProperty("factors", out var factors) || factors.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return factors.EnumerateArray()
            .Select(factor => new RecommendationHistoryFactorResponse(
                ReadString(factor, "factor") ?? string.Empty,
                ReadBool(factor, "isEvaluable"),
                ReadDecimal(factor, "score"),
                ReadDecimal(factor, "aggregatedValue"),
                ReadDecimal(factor, "configuredWeight"),
                ReadDecimal(factor, "effectiveWeight"),
                ReadString(factor, "explanation")))
            .ToList();
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .ToList();
    }

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static bool ReadBool(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.True;

    private static decimal? ReadDecimal(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.TryGetDecimal(out var value)
            ? value
            : null;

    private static decimal? ReadNestedDecimal(JsonElement element, string propertyName, string nestedPropertyName) =>
        element.TryGetProperty(propertyName, out var property)
            ? ReadDecimal(property, nestedPropertyName)
            : null;

    private static DateOnly? ReadDateOnly(JsonElement element, string propertyName)
    {
        var value = ReadString(element, propertyName);
        return DateOnly.TryParse(value, out var date) ? date : null;
    }

    private static DateTimeOffset? ReadDateTimeOffset(JsonElement element, string propertyName)
    {
        var value = ReadString(element, propertyName);
        return DateTimeOffset.TryParse(value, out var date) ? date : null;
    }

    private static Guid? ReadGuid(JsonElement element, string propertyName)
    {
        var value = ReadString(element, propertyName);
        return Guid.TryParse(value, out var guid) ? guid : null;
    }

    private static int ApprovedCropOrder(string cropName)
    {
        var index = Array.FindIndex(ApprovedCropNames.All, approvedCropName =>
            string.Equals(approvedCropName, cropName, StringComparison.OrdinalIgnoreCase));
        return index < 0 ? int.MaxValue : index;
    }

    private static bool IsReadFailure(Exception exception) =>
        exception is DbUpdateException or DbException or TimeoutException or RetryLimitExceededException ||
        exception.InnerException is not null && IsReadFailure(exception.InnerException);

    private sealed record CropSnapshot(
        string? CropName,
        IReadOnlyList<string> UnavailableFactors,
        IReadOnlyList<RecommendationHistoryFactorResponse> Factors,
        IReadOnlyList<string> ClimateRisks);
}
