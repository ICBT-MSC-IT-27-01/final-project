using AnuradhapuraAI.Application.Forecasting;
using AnuradhapuraAI.Application.Recommendations;
using AnuradhapuraAI.Application.Suitability;
using AnuradhapuraAI.Domain.Common;
using AnuradhapuraAI.Domain.Entities;

namespace AnuradhapuraAI.Infrastructure.Forecasting;

internal static class OperationalForecastRunSelector
{
    private const int ForecastHorizonDays = 7;
    private const string DistrictName = "Anuradhapura";

    public static LatestForecastResponse? ToLatestForecastOrNull(ForecastRun run)
    {
        var selected = ToSelectedForecastRunOrNull(run);
        if (selected is null)
        {
            return null;
        }

        return new LatestForecastResponse(
            selected.ForecastRunId,
            selected.ModelVersion,
            selected.ForecastDate,
            selected.CreatedAt,
            selected.TargetStartDate,
            selected.TargetEndDate,
            DistrictName,
            selected.ForecastDays
                .Select(day => new LatestForecastDayResponse(
                    day.TargetDate,
                    day.Rainfall,
                    day.Temperature,
                    day.Humidity))
                .ToList(),
            "Operational",
            [
                "Forecast values are AI-predicted weather forecasts, not measured observations.",
                "Operational forecasts are limited to verified ForecastRun records with seven complete daily predictions.",
                "Open-Meteo Forecast API past-days input is gridded model data and must not be interpreted as measured district-average observations."
            ]);
    }

    public static SelectedForecastRun? ToSelectedForecastRunOrNull(ForecastRun run)
    {
        if (run.ForecastRunId == Guid.Empty ||
            !string.Equals(run.RunPurpose, ApprovedForecastRunPurposes.Operational, StringComparison.Ordinal))
        {
            return null;
        }

        var records = run.ForecastRecords
            .OrderBy(record => record.TargetDate)
            .ThenBy(record => record.Id)
            .ToList();

        if (records.Count != ForecastHorizonDays ||
            records.Any(record => record.ForecastRunId != run.ForecastRunId) ||
            records.Select(record => record.TargetDate).Distinct().Count() != ForecastHorizonDays)
        {
            return null;
        }

        for (var index = 1; index < records.Count; index++)
        {
            if (records[index].TargetDate != records[index - 1].TargetDate.AddDays(1))
            {
                return null;
            }
        }

        if (records.Select(record => record.ForecastDate).Distinct().Count() != 1 ||
            records.Select(record => record.ModelVersion ?? string.Empty).Distinct(StringComparer.Ordinal).Count() != 1)
        {
            return null;
        }

        if (run.ForecastOriginDate is null ||
            run.TargetStartDate is null ||
            run.TargetEndDate is null ||
            string.IsNullOrWhiteSpace(run.ModelVersion) ||
            records[0].ForecastDate != run.ForecastOriginDate.Value ||
            records[0].TargetDate != run.TargetStartDate.Value ||
            records[^1].TargetDate != run.TargetEndDate.Value ||
            !string.Equals(records[0].ModelVersion, run.ModelVersion, StringComparison.Ordinal) ||
            !string.Equals(run.SourceProvider, "Open-Meteo", StringComparison.Ordinal) ||
            !string.Equals(run.SourceProduct, OpenMeteoForecastPastDaysWeatherSource.ApprovedProduct, StringComparison.Ordinal) ||
            !string.Equals(run.Timezone, "Asia/Colombo", StringComparison.Ordinal) ||
            run.ObservationStartDate is null ||
            run.ObservationEndDate is null ||
            run.SourceRetrievedAt is null ||
            string.IsNullOrWhiteSpace(run.DataQualityStatus))
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
            run.ForecastRunId,
            records[0].ForecastDate,
            records[0].TargetDate,
            records[^1].TargetDate,
            records[0].ModelVersion,
            run.CreatedAt,
            forecastDays);
    }

    private static bool IsFinite(decimal value) =>
        value != decimal.MinValue && value != decimal.MaxValue;
}
