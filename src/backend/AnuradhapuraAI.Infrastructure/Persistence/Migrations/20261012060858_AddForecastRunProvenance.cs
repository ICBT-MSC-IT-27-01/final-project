using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnuradhapuraAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddForecastRunProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ForecastRun",
                columns: table => new
                {
                    ForecastRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunPurpose = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ForecastOriginDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TargetStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TargetEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ModelVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourceProvider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourceProduct = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequestedLatitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    RequestedLongitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Timezone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ObservationStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ObservationEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SourceRetrievedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DataQualityStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastRun", x => x.ForecastRunId);
                    table.CheckConstraint("CK_ForecastRun_ForecastRunId_NotEmpty", "[ForecastRunId] <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("CK_ForecastRun_RunPurpose", "[RunPurpose] IN (N'Operational', N'HistoricalEvaluation', N'LegacyUnknown')");
                });

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM [ForecastRecord]
                    WHERE [ForecastRunId] = '00000000-0000-0000-0000-000000000000'
                )
                BEGIN
                    THROW 51001, 'AddForecastRunProvenance cannot backfill ForecastRun because ForecastRecord contains empty ForecastRunId values.', 1;
                END
                """);

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM [ForecastRecord]
                    WHERE [CreatedAt] IS NULL
                )
                BEGIN
                    THROW 51002, 'AddForecastRunProvenance cannot backfill ForecastRun because ForecastRecord contains null CreatedAt values.', 1;
                END
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO [ForecastRun] (
                    [ForecastRunId],
                    [RunPurpose],
                    [CreatedAt],
                    [ForecastOriginDate],
                    [TargetStartDate],
                    [TargetEndDate],
                    [ModelVersion]
                )
                SELECT
                    [LegacyRun].[ForecastRunId],
                    N'LegacyUnknown',
                    MIN([LegacyRun].[CreatedAt]),
                    CASE
                        WHEN COUNT(DISTINCT [LegacyRun].[ForecastDate]) = 1
                        THEN MIN([LegacyRun].[ForecastDate])
                        ELSE NULL
                    END,
                    MIN([LegacyRun].[TargetDate]),
                    MAX([LegacyRun].[TargetDate]),
                    CASE
                        WHEN COUNT(DISTINCT CASE
                            WHEN [LegacyRun].[ModelVersion] IS NULL THEN N'__ANURADHAPURA_AI_NULL_MODEL_VERSION__'
                            ELSE N'__ANURADHAPURA_AI_VALUE_MODEL_VERSION__' + [LegacyRun].[ModelVersion]
                        END) = 1
                        THEN MAX([LegacyRun].[ModelVersion])
                        ELSE NULL
                    END
                FROM [ForecastRecord] AS [LegacyRun]
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [ForecastRun] AS [ExistingRun]
                    WHERE [ExistingRun].[ForecastRunId] = [LegacyRun].[ForecastRunId]
                )
                GROUP BY [LegacyRun].[ForecastRunId];
                """);

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM [ForecastRecord] AS [Record]
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM [ForecastRun] AS [Run]
                        WHERE [Run].[ForecastRunId] = [Record].[ForecastRunId]
                    )
                )
                BEGIN
                    THROW 51003, 'AddForecastRunProvenance cannot add ForecastRecord foreign key because orphan ForecastRunId values remain after backfill.', 1;
                END
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_ForecastRecord_ForecastRun_ForecastRunId",
                table: "ForecastRecord",
                column: "ForecastRunId",
                principalTable: "ForecastRun",
                principalColumn: "ForecastRunId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ForecastRecord_ForecastRun_ForecastRunId",
                table: "ForecastRecord");

            migrationBuilder.DropTable(
                name: "ForecastRun");
        }
    }
}
