using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasks_UserId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tags_UserId_Name",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Notes_UserId",
                table: "Notes");

            migrationBuilder.DropIndex(
                name: "IX_Meetings_UserId",
                table: "Meetings");

            migrationBuilder.DropIndex(
                name: "IX_Meetings_UserId_CalendarEventId",
                table: "Meetings");

            migrationBuilder.DropIndex(
                name: "IX_CalendarConnections_UserId_Provider",
                table: "CalendarConnections");

            migrationBuilder.DropIndex(
                name: "IX_BehavioralGoals_UserId",
                table: "BehavioralGoals");

            migrationBuilder.AddColumn<Guid>(
                name: "ProfileId",
                table: "Tasks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProfileId",
                table: "Tags",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProfileId",
                table: "Notes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProfileId",
                table: "Meetings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProfileId",
                table: "CalendarConnections",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProfileId",
                table: "BehavioralGoals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "AccountLinkCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsRedeemed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountLinkCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountLinkCodes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KeyPrefix = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BlindSpotNudges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Dimension = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Suggestion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BlindSpotDescription = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConvertedGoalId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlindSpotNudges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LinkedIdentities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AvatarUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    DefaultProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkedIdentities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LinkedIdentities_Profiles_DefaultProfileId",
                        column: x => x.DefaultProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LinkedIdentities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UserId_ProfileId",
                table: "Tasks",
                columns: new[] { "UserId", "ProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_UserId_ProfileId_Name",
                table: "Tags",
                columns: new[] { "UserId", "ProfileId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notes_UserId_ProfileId",
                table: "Notes",
                columns: new[] { "UserId", "ProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_UserId_ProfileId",
                table: "Meetings",
                columns: new[] { "UserId", "ProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_UserId_ProfileId_CalendarEventId",
                table: "Meetings",
                columns: new[] { "UserId", "ProfileId", "CalendarEventId" },
                unique: true,
                filter: "\"CalendarEventId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarConnections_UserId_ProfileId_Provider",
                table: "CalendarConnections",
                columns: new[] { "UserId", "ProfileId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BehavioralGoals_UserId_ProfileId",
                table: "BehavioralGoals",
                columns: new[] { "UserId", "ProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountLinkCodes_CodeHash",
                table: "AccountLinkCodes",
                column: "CodeHash");

            migrationBuilder.CreateIndex(
                name: "IX_AccountLinkCodes_UserId",
                table: "AccountLinkCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_KeyHash",
                table: "ApiKeys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_UserId",
                table: "ApiKeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BlindSpotNudges_UserId_ProfileId",
                table: "BlindSpotNudges",
                columns: new[] { "UserId", "ProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_LinkedIdentities_DefaultProfileId",
                table: "LinkedIdentities",
                column: "DefaultProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_LinkedIdentities_Provider_ProviderId",
                table: "LinkedIdentities",
                columns: new[] { "Provider", "ProviderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LinkedIdentities_UserId",
                table: "LinkedIdentities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_UserId_IsDefault_Unique",
                table: "Profiles",
                column: "UserId",
                unique: true,
                filter: "\"IsDefault\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountLinkCodes");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "BlindSpotNudges");

            migrationBuilder.DropTable(
                name: "LinkedIdentities");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_UserId_ProfileId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tags_UserId_ProfileId_Name",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Notes_UserId_ProfileId",
                table: "Notes");

            migrationBuilder.DropIndex(
                name: "IX_Meetings_UserId_ProfileId",
                table: "Meetings");

            migrationBuilder.DropIndex(
                name: "IX_Meetings_UserId_ProfileId_CalendarEventId",
                table: "Meetings");

            migrationBuilder.DropIndex(
                name: "IX_CalendarConnections_UserId_ProfileId_Provider",
                table: "CalendarConnections");

            migrationBuilder.DropIndex(
                name: "IX_BehavioralGoals_UserId_ProfileId",
                table: "BehavioralGoals");

            migrationBuilder.DropColumn(
                name: "ProfileId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ProfileId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "ProfileId",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "ProfileId",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "ProfileId",
                table: "CalendarConnections");

            migrationBuilder.DropColumn(
                name: "ProfileId",
                table: "BehavioralGoals");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UserId",
                table: "Tasks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_UserId_Name",
                table: "Tags",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notes_UserId",
                table: "Notes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_UserId",
                table: "Meetings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_UserId_CalendarEventId",
                table: "Meetings",
                columns: new[] { "UserId", "CalendarEventId" },
                unique: true,
                filter: "\"CalendarEventId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarConnections_UserId_Provider",
                table: "CalendarConnections",
                columns: new[] { "UserId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BehavioralGoals_UserId",
                table: "BehavioralGoals",
                column: "UserId");
        }
    }
}
