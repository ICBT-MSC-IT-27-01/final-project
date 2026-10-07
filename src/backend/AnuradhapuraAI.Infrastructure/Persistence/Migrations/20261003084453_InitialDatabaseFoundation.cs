using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AnuradhapuraAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialDatabaseFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Crop",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Crop", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ForecastRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ForecastDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Rainfall = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Temperature = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Humidity = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastRecord", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRole",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRole", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CropEnvironmentalRequirement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CropId = table.Column<int>(type: "int", nullable: false),
                    VariableType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MinimumValue = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    MaximumValue = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CropEnvironmentalRequirement", x => x.Id);
                    table.CheckConstraint("CK_CropEnvironmentalRequirement_MinimumMaximum", "[MinimumValue] <= [MaximumValue]");
                    table.CheckConstraint("CK_CropEnvironmentalRequirement_VariableType", "[VariableType] IN (N'Rainfall', N'Temperature', N'Humidity')");
                    table.ForeignKey(
                        name: "FK_CropEnvironmentalRequirement_Crop_CropId",
                        column: x => x.CropId,
                        principalTable: "Crop",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SoilCompatibility",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CropId = table.Column<int>(type: "int", nullable: false),
                    SoilType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CompatibilityScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoilCompatibility", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoilCompatibility_Crop_CropId",
                        column: x => x.CropId,
                        principalTable: "Crop",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_UserRole_RoleId",
                        column: x => x.RoleId,
                        principalTable: "UserRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Recommendation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recommendation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recommendation_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SuitabilityConfiguration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConfigurationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConfigurationKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuitabilityConfiguration", x => x.Id);
                    table.CheckConstraint("CK_SuitabilityConfiguration_ConfigurationKey", "[ConfigurationKey] IN (N'Rainfall', N'Temperature', N'Humidity', N'SoilCompatibility', N'Highly Suitable', N'Suitable', N'Moderately Suitable', N'Unsuitable')");
                    table.CheckConstraint("CK_SuitabilityConfiguration_ConfigurationType", "[ConfigurationType] IN (N'FactorWeight', N'CategoryThreshold')");
                    table.ForeignKey(
                        name: "FK_SuitabilityConfiguration_User_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationCrop",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecommendationId = table.Column<int>(type: "int", nullable: false),
                    CropId = table.Column<int>(type: "int", nullable: false),
                    RainfallScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TemperatureScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    HumidityScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    SoilScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    OverallScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    SuitabilityCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationCrop", x => x.Id);
                    table.CheckConstraint("CK_RecommendationCrop_SuitabilityCategory", "[SuitabilityCategory] IN (N'Highly Suitable', N'Suitable', N'Moderately Suitable', N'Unsuitable')");
                    table.ForeignKey(
                        name: "FK_RecommendationCrop_Crop_CropId",
                        column: x => x.CropId,
                        principalTable: "Crop",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecommendationCrop_Recommendation_RecommendationId",
                        column: x => x.RecommendationId,
                        principalTable: "Recommendation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationValidation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecommendationId = table.Column<int>(type: "int", nullable: false),
                    OfficerUserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationValidation", x => x.Id);
                    table.CheckConstraint("CK_RecommendationValidation_Status", "[Status] IN (N'Pending Validation', N'Validated', N'Needs Review')");
                    table.ForeignKey(
                        name: "FK_RecommendationValidation_Recommendation_RecommendationId",
                        column: x => x.RecommendationId,
                        principalTable: "Recommendation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecommendationValidation_User_OfficerUserId",
                        column: x => x.OfficerUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Crop",
                columns: new[] { "Id", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, true, "Paddy" },
                    { 2, true, "Maize" },
                    { 3, true, "Green Gram" },
                    { 4, true, "Cowpea" },
                    { 5, true, "Groundnut" },
                    { 6, true, "Chilli" }
                });

            migrationBuilder.InsertData(
                table: "UserRole",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Registered User" },
                    { 2, "Agricultural Officer" },
                    { 3, "Administrator" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Crop_Name",
                table: "Crop",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CropEnvironmentalRequirement_CropId_VariableType",
                table: "CropEnvironmentalRequirement",
                columns: new[] { "CropId", "VariableType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ForecastRecord_ForecastDate_TargetDate",
                table: "ForecastRecord",
                columns: new[] { "ForecastDate", "TargetDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Recommendation_UserId",
                table: "Recommendation",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationCrop_CropId",
                table: "RecommendationCrop",
                column: "CropId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationCrop_RecommendationId_CropId",
                table: "RecommendationCrop",
                columns: new[] { "RecommendationId", "CropId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationCrop_RecommendationId_Rank",
                table: "RecommendationCrop",
                columns: new[] { "RecommendationId", "Rank" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationValidation_OfficerUserId",
                table: "RecommendationValidation",
                column: "OfficerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationValidation_RecommendationId",
                table: "RecommendationValidation",
                column: "RecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_SoilCompatibility_CropId_SoilType",
                table: "SoilCompatibility",
                columns: new[] { "CropId", "SoilType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SuitabilityConfiguration_ConfigurationType_ConfigurationKey",
                table: "SuitabilityConfiguration",
                columns: new[] { "ConfigurationType", "ConfigurationKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SuitabilityConfiguration_UpdatedByUserId",
                table: "SuitabilityConfiguration",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                table: "User",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_RoleId",
                table: "User",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_Name",
                table: "UserRole",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CropEnvironmentalRequirement");

            migrationBuilder.DropTable(
                name: "ForecastRecord");

            migrationBuilder.DropTable(
                name: "RecommendationCrop");

            migrationBuilder.DropTable(
                name: "RecommendationValidation");

            migrationBuilder.DropTable(
                name: "SoilCompatibility");

            migrationBuilder.DropTable(
                name: "SuitabilityConfiguration");

            migrationBuilder.DropTable(
                name: "Recommendation");

            migrationBuilder.DropTable(
                name: "Crop");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "UserRole");
        }
    }
}
