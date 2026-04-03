using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBehavioralReflectionInsights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BehavioralGoals");

            migrationBuilder.DropTable(
                name: "BlindSpotNudges");

            migrationBuilder.DropColumn(
                name: "BehavioralAnalysis",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "ExcludeFromInsights",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "ReflectionData",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "ReflectionSubmittedAt",
                table: "Meetings");

            // Clean up stale feature notification rows for removed features
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" IN (
                    'Post-meeting self-reflection',
                    'Behavioral Goals',
                    'Blind Spot Nudges'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BehavioralAnalysis",
                table: "Meetings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExcludeFromInsights",
                table: "Meetings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReflectionData",
                table: "Meetings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReflectionSubmittedAt",
                table: "Meetings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BehavioralGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MetricType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Operator = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetValue = table.Column<double>(type: "double precision", nullable: false),
                    TargetValueUpper = table.Column<double>(type: "double precision", nullable: true),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BehavioralGoals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BlindSpotNudges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlindSpotDescription = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConvertedGoalId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Dimension = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Suggestion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlindSpotNudges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BehavioralGoals_UserId_ProfileId",
                table: "BehavioralGoals",
                columns: new[] { "UserId", "ProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_BlindSpotNudges_UserId_ProfileId",
                table: "BlindSpotNudges",
                columns: new[] { "UserId", "ProfileId" });
        }
    }
}
