using System.Data.Common;
using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Application.Validations;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;
using AnuradhapuraAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace AnuradhapuraAI.Infrastructure.Validations;

public sealed class RecommendationValidationService(
    AnuradhapuraAiDbContext dbContext,
    IOptions<WeatherModelFeatureOptions> featureOptions,
    TimeProvider timeProvider) : IRecommendationValidationService
{
    private const int MaximumCommentLength = 1000;

    public async Task<RecommendationValidationResult<IReadOnlyList<RecommendationReviewSummaryResponse>>> ListPendingReviewsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!featureOptions.Value.EnableOfficerValidation)
        {
            return Failure<IReadOnlyList<RecommendationReviewSummaryResponse>>(
                RecommendationValidationErrorCodes.FeatureDisabled,
                "Agricultural Officer validation is disabled.");
        }

        try
        {
            var recommendations = await dbContext.Recommendations
                .AsNoTracking()
                .Include(recommendation => recommendation.RecommendationValidations)
                .ToListAsync(cancellationToken);

            return RecommendationValidationResult<IReadOnlyList<RecommendationReviewSummaryResponse>>.Success(
                recommendations
                    .Select(recommendation => new
                    {
                        Recommendation = recommendation,
                        LatestValidation = recommendation.RecommendationValidations
                            .OrderByDescending(validation => validation.CreatedAt)
                            .ThenByDescending(validation => validation.Id)
                            .FirstOrDefault()
                    })
                    .Where(item => item.LatestValidation is null ||
                        item.LatestValidation.Status == ApprovedValidationStatuses.PendingValidation)
                    .OrderByDescending(item => item.Recommendation.CreatedAt)
                    .Select(item => ToSummaryResponse(item.Recommendation, item.LatestValidation))
                    .ToList());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            return Failure<IReadOnlyList<RecommendationReviewSummaryResponse>>(
                RecommendationValidationErrorCodes.DatabaseReadFailed,
                "Recommendation validation records could not be read.");
        }
    }

    public async Task<RecommendationValidationResult<RecommendationReviewResponse>> GetReviewAsync(
        int recommendationId,
        CancellationToken cancellationToken = default)
    {
        if (!featureOptions.Value.EnableOfficerValidation)
        {
            return Failure<RecommendationReviewResponse>(
                RecommendationValidationErrorCodes.FeatureDisabled,
                "Agricultural Officer validation is disabled.");
        }

        try
        {
            var recommendation = await dbContext.Recommendations
                .AsNoTracking()
                .Include(item => item.RecommendationCrops)
                    .ThenInclude(crop => crop.Crop)
                .Include(item => item.RecommendationValidations)
                .SingleOrDefaultAsync(item => item.Id == recommendationId, cancellationToken);

            return recommendation is null
                ? Failure<RecommendationReviewResponse>(
                    RecommendationValidationErrorCodes.RecommendationNotFound,
                    "Recommendation was not found.")
                : RecommendationValidationResult<RecommendationReviewResponse>.Success(ToReviewResponse(recommendation));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            return Failure<RecommendationReviewResponse>(
                RecommendationValidationErrorCodes.DatabaseReadFailed,
                "Recommendation validation records could not be read.");
        }
    }

    public async Task<RecommendationValidationResult<RecommendationValidationResponse>> SubmitValidationAsync(
        int recommendationId,
        RecommendationValidationSubmission submission,
        CancellationToken cancellationToken = default)
    {
        if (!featureOptions.Value.EnableOfficerValidation)
        {
            return Failure<RecommendationValidationResponse>(
                RecommendationValidationErrorCodes.FeatureDisabled,
                "Agricultural Officer validation is disabled.");
        }

        var status = submission.Status.Trim();
        if (!ApprovedValidationStatuses.All.Contains(status, StringComparer.Ordinal))
        {
            return Failure<RecommendationValidationResponse>(
                RecommendationValidationErrorCodes.InvalidStatus,
                "Validation status is invalid.");
        }

        var comment = string.IsNullOrWhiteSpace(submission.Comment)
            ? null
            : submission.Comment.Trim();
        if (comment is { Length: > MaximumCommentLength })
        {
            return Failure<RecommendationValidationResponse>(
                RecommendationValidationErrorCodes.InvalidComment,
                "Validation comment is too long.");
        }

        try
        {
            var reviewerIsValid = await dbContext.Users
                .AsNoTracking()
                .Include(user => user.Role)
                .AnyAsync(
                    user => user.Id == submission.TrustedOfficerUserId &&
                        user.IsActive &&
                        user.Role != null &&
                        user.Role.Name == ApprovedRoleNames.AgriculturalOfficer,
                    cancellationToken);

            if (!reviewerIsValid)
            {
                return Failure<RecommendationValidationResponse>(
                    RecommendationValidationErrorCodes.InvalidReviewer,
                    "Agricultural Officer identity is invalid.");
            }

            var recommendationExists = await dbContext.Recommendations
                .AsNoTracking()
                .AnyAsync(recommendation => recommendation.Id == recommendationId, cancellationToken);

            if (!recommendationExists)
            {
                return Failure<RecommendationValidationResponse>(
                    RecommendationValidationErrorCodes.RecommendationNotFound,
                    "Recommendation was not found.");
            }

            var validation = new RecommendationValidation
            {
                RecommendationId = recommendationId,
                OfficerUserId = submission.TrustedOfficerUserId,
                Status = status,
                Comment = comment,
                CreatedAt = timeProvider.GetUtcNow()
            };

            if (dbContext.Database.IsRelational())
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                dbContext.RecommendationValidations.Add(validation);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                dbContext.RecommendationValidations.Add(validation);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return RecommendationValidationResult<RecommendationValidationResponse>.Success(ToValidationResponse(validation));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            return Failure<RecommendationValidationResponse>(
                RecommendationValidationErrorCodes.PersistenceFailed,
                "Recommendation validation could not be saved.");
        }
    }

    private static RecommendationReviewSummaryResponse ToSummaryResponse(
        Recommendation recommendation,
        RecommendationValidation? latestValidation) =>
        new(
            recommendation.Id,
            recommendation.ForecastRunId,
            recommendation.UserId,
            recommendation.SoilType,
            recommendation.CreatedAt,
            latestValidation?.Status,
            latestValidation?.CreatedAt);

    private static RecommendationReviewResponse ToReviewResponse(Recommendation recommendation) =>
        new(
            recommendation.Id,
            recommendation.ForecastRunId,
            recommendation.UserId,
            recommendation.SoilType,
            recommendation.CreatedAt,
            recommendation.EvidenceSnapshotJson,
            recommendation.RecommendationCrops
                .OrderBy(crop => crop.Rank ?? int.MaxValue)
                .ThenBy(crop => crop.CropId)
                .Select(crop => new RecommendationReviewCropResponse(
                    crop.CropId,
                    crop.Crop?.Name ?? string.Empty,
                    crop.RainfallScore,
                    crop.TemperatureScore,
                    crop.HumidityScore,
                    crop.SoilScore,
                    crop.OverallScore,
                    crop.SuitabilityCategory,
                    crop.Rank,
                    crop.EvaluationStatus,
                    crop.Explanation,
                    crop.EvidenceSnapshotJson))
                .ToList(),
            recommendation.RecommendationValidations
                .OrderByDescending(validation => validation.CreatedAt)
                .ThenByDescending(validation => validation.Id)
                .Select(ToValidationResponse)
                .ToList());

    private static RecommendationValidationResponse ToValidationResponse(RecommendationValidation validation) =>
        new(
            validation.Id,
            validation.RecommendationId,
            validation.OfficerUserId,
            validation.Status,
            validation.Comment,
            validation.CreatedAt);

    private static RecommendationValidationResult<T> Failure<T>(string errorCode, string message) =>
        RecommendationValidationResult<T>.Failure(errorCode, message);

    private static bool IsDatabaseFailure(Exception exception) =>
        exception is DbUpdateException or DbException or TimeoutException or RetryLimitExceededException ||
        exception.InnerException is not null && IsDatabaseFailure(exception.InnerException);
}
