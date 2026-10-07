using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnuradhapuraAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCropRequirementScoringMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AcceptableMaximumValue",
                table: "CropEnvironmentalRequirement",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AcceptableMinimumValue",
                table: "CropEnvironmentalRequirement",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompatibleWithSevenDayForecast",
                table: "CropEnvironmentalRequirement",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TimeBasis",
                table: "CropEnvironmentalRequirement",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_CropEnvironmentalRequirement_AcceptableOptimalOrdering",
                table: "CropEnvironmentalRequirement",
                sql: "[AcceptableMinimumValue] IS NULL OR ([AcceptableMinimumValue] <= [MinimumValue] AND [MaximumValue] <= [AcceptableMaximumValue])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CropEnvironmentalRequirement_TimeBasis",
                table: "CropEnvironmentalRequirement",
                sql: "[TimeBasis] IS NULL OR [TimeBasis] IN (N'Daily', N'SevenDay', N'GrowingPeriod', N'Seasonal', N'Annual')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CropEnvironmentalRequirement_AcceptableOptimalOrdering",
                table: "CropEnvironmentalRequirement");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CropEnvironmentalRequirement_TimeBasis",
                table: "CropEnvironmentalRequirement");

            migrationBuilder.DropColumn(
                name: "AcceptableMaximumValue",
                table: "CropEnvironmentalRequirement");

            migrationBuilder.DropColumn(
                name: "AcceptableMinimumValue",
                table: "CropEnvironmentalRequirement");

            migrationBuilder.DropColumn(
                name: "IsCompatibleWithSevenDayForecast",
                table: "CropEnvironmentalRequirement");

            migrationBuilder.DropColumn(
                name: "TimeBasis",
                table: "CropEnvironmentalRequirement");
        }
    }
}
