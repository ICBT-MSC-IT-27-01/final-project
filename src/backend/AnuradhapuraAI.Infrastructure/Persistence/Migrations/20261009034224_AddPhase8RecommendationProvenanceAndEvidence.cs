using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnuradhapuraAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase8RecommendationProvenanceAndEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecommendationCrop_RecommendationId_Rank",
                table: "RecommendationCrop");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecommendationCrop_SuitabilityCategory",
                table: "RecommendationCrop");

            migrationBuilder.AlterColumn<decimal>(
                name: "TemperatureScore",
                table: "RecommendationCrop",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "SuitabilityCategory",
                table: "RecommendationCrop",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "SoilScore",
                table: "RecommendationCrop",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "Rank",
                table: "RecommendationCrop",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "RainfallScore",
                table: "RecommendationCrop",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "OverallScore",
                table: "RecommendationCrop",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "HumidityScore",
                table: "RecommendationCrop",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "EvaluationStatus",
                table: "RecommendationCrop",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceSnapshotJson",
                table: "RecommendationCrop",
                type: "nvarchar(max)",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceSnapshotJson",
                table: "Recommendation",
                type: "nvarchar(max)",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ForecastRunId",
                table: "Recommendation",
                type: "uniqueidentifier",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "SoilType",
                table: "Recommendation",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ForecastRunId",
                table: "ForecastRecord",
                type: "uniqueidentifier",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationCrop_RecommendationId_Rank",
                table: "RecommendationCrop",
                columns: new[] { "RecommendationId", "Rank" },
                unique: true,
                filter: "[Rank] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecommendationCrop_EvaluationStatus",
                table: "RecommendationCrop",
                sql: "[EvaluationStatus] IN (N'Complete', N'Partial', N'InsufficientEvidence')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecommendationCrop_EvidenceSnapshotJson_IsJson",
                table: "RecommendationCrop",
                sql: "ISJSON([EvidenceSnapshotJson]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecommendationCrop_Rank_Positive",
                table: "RecommendationCrop",
                sql: "[Rank] IS NULL OR [Rank] >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecommendationCrop_Scores_Range",
                table: "RecommendationCrop",
                sql: "([RainfallScore] IS NULL OR ([RainfallScore] >= 0 AND [RainfallScore] <= 100)) AND ([TemperatureScore] IS NULL OR ([TemperatureScore] >= 0 AND [TemperatureScore] <= 100)) AND ([HumidityScore] IS NULL OR ([HumidityScore] >= 0 AND [HumidityScore] <= 100)) AND ([SoilScore] IS NULL OR ([SoilScore] >= 0 AND [SoilScore] <= 100)) AND ([OverallScore] IS NULL OR ([OverallScore] >= 0 AND [OverallScore] <= 100))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecommendationCrop_SuitabilityCategory",
                table: "RecommendationCrop",
                sql: "[SuitabilityCategory] IS NULL OR [SuitabilityCategory] IN (N'Highly Suitable', N'Suitable', N'Moderately Suitable', N'Unsuitable')");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendation_ForecastRunId",
                table: "Recommendation",
                column: "ForecastRunId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Recommendation_EvidenceSnapshotJson_IsJson",
                table: "Recommendation",
                sql: "ISJSON([EvidenceSnapshotJson]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Recommendation_ForecastRunId_NotEmpty",
                table: "Recommendation",
                sql: "[ForecastRunId] <> '00000000-0000-0000-0000-000000000000'");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastRecord_ForecastRunId_TargetDate",
                table: "ForecastRecord",
                columns: new[] { "ForecastRunId", "TargetDate" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ForecastRecord_ForecastRunId_NotEmpty",
                table: "ForecastRecord",
                sql: "[ForecastRunId] <> '00000000-0000-0000-0000-000000000000'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecommendationCrop_RecommendationId_Rank",
                table: "RecommendationCrop");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecommendationCrop_EvaluationStatus",
                table: "RecommendationCrop");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecommendationCrop_EvidenceSnapshotJson_IsJson",
                table: "RecommendationCrop");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecommendationCrop_Rank_Positive",
                table: "RecommendationCrop");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecommendationCrop_Scores_Range",
                table: "RecommendationCrop");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecommendationCrop_SuitabilityCategory",
                table: "RecommendationCrop");

            migrationBuilder.DropIndex(
                name: "IX_Recommendation_ForecastRunId",
                table: "Recommendation");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Recommendation_EvidenceSnapshotJson_IsJson",
                table: "Recommendation");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Recommendation_ForecastRunId_NotEmpty",
                table: "Recommendation");

            migrationBuilder.DropIndex(
                name: "IX_ForecastRecord_ForecastRunId_TargetDate",
                table: "ForecastRecord");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ForecastRecord_ForecastRunId_NotEmpty",
                table: "ForecastRecord");

            migrationBuilder.DropColumn(
                name: "EvaluationStatus",
                table: "RecommendationCrop");

            migrationBuilder.DropColumn(
                name: "EvidenceSnapshotJson",
                table: "RecommendationCrop");

            migrationBuilder.DropColumn(
                name: "EvidenceSnapshotJson",
                table: "Recommendation");

            migrationBuilder.DropColumn(
                name: "ForecastRunId",
                table: "Recommendation");

            migrationBuilder.DropColumn(
                name: "SoilType",
                table: "Recommendation");

            migrationBuilder.DropColumn(
                name: "ForecastRunId",
                table: "ForecastRecord");

            migrationBuilder.AlterColumn<decimal>(
                name: "TemperatureScore",
                table: "RecommendationCrop",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SuitabilityCategory",
                table: "RecommendationCrop",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "SoilScore",
                table: "RecommendationCrop",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Rank",
                table: "RecommendationCrop",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "RainfallScore",
                table: "RecommendationCrop",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "OverallScore",
                table: "RecommendationCrop",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "HumidityScore",
                table: "RecommendationCrop",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationCrop_RecommendationId_Rank",
                table: "RecommendationCrop",
                columns: new[] { "RecommendationId", "Rank" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecommendationCrop_SuitabilityCategory",
                table: "RecommendationCrop",
                sql: "[SuitabilityCategory] IN (N'Highly Suitable', N'Suitable', N'Moderately Suitable', N'Unsuitable')");
        }
    }
}
