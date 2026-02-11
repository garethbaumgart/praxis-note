using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // Phase 1: Create Profiles table
            // ============================================================
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

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_UserId",
                table: "Profiles",
                column: "UserId");

            // ============================================================
            // Phase 2: Add ProfileId as NULLABLE to all entity tables
            // ============================================================
            string[] tables = ["Tasks", "Tags", "Notes", "Meetings", "CalendarConnections", "BehavioralGoals"];

            foreach (var table in tables)
            {
                migrationBuilder.AddColumn<Guid>(
                    name: "ProfileId",
                    table: table,
                    type: "uuid",
                    nullable: true);
            }

            // ============================================================
            // Phase 3: Create a default profile per user and backfill
            // ============================================================
            migrationBuilder.Sql("""
                -- Create a default profile for every existing user
                INSERT INTO "Profiles" ("Id", "UserId", "Name", "Icon", "IsDefault", "CreatedAt", "UpdatedAt")
                SELECT gen_random_uuid(), "Id", 'Default', NULL, true, NOW(), NOW()
                FROM "Users";

                -- Backfill ProfileId on all entity tables using the user's default profile
                UPDATE "Tasks" t
                SET "ProfileId" = p."Id"
                FROM "Profiles" p
                WHERE p."UserId" = t."UserId" AND p."IsDefault" = true;

                UPDATE "Tags" t
                SET "ProfileId" = p."Id"
                FROM "Profiles" p
                WHERE p."UserId" = t."UserId" AND p."IsDefault" = true;

                UPDATE "Notes" n
                SET "ProfileId" = p."Id"
                FROM "Profiles" p
                WHERE p."UserId" = n."UserId" AND p."IsDefault" = true;

                UPDATE "Meetings" m
                SET "ProfileId" = p."Id"
                FROM "Profiles" p
                WHERE p."UserId" = m."UserId" AND p."IsDefault" = true;

                UPDATE "CalendarConnections" c
                SET "ProfileId" = p."Id"
                FROM "Profiles" p
                WHERE p."UserId" = c."UserId" AND p."IsDefault" = true;

                UPDATE "BehavioralGoals" bg
                SET "ProfileId" = p."Id"
                FROM "Profiles" p
                WHERE p."UserId" = bg."UserId" AND p."IsDefault" = true;
                """);

            // ============================================================
            // Phase 4: Make ProfileId NOT NULL
            // ============================================================
            foreach (var table in tables)
            {
                migrationBuilder.AlterColumn<Guid>(
                    name: "ProfileId",
                    table: table,
                    type: "uuid",
                    nullable: false,
                    defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                    oldClrType: typeof(Guid),
                    oldType: "uuid",
                    oldNullable: true);
            }

            // ============================================================
            // Phase 5: Drop old indexes and create new composite indexes
            // ============================================================
            migrationBuilder.DropIndex(name: "IX_Tasks_UserId", table: "Tasks");
            migrationBuilder.DropIndex(name: "IX_Tags_UserId_Name", table: "Tags");
            migrationBuilder.DropIndex(name: "IX_Notes_UserId", table: "Notes");
            migrationBuilder.DropIndex(name: "IX_Meetings_UserId", table: "Meetings");
            migrationBuilder.DropIndex(name: "IX_CalendarConnections_UserId_Provider", table: "CalendarConnections");
            migrationBuilder.DropIndex(name: "IX_BehavioralGoals_UserId", table: "BehavioralGoals");

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
                name: "IX_CalendarConnections_UserId_ProfileId_Provider",
                table: "CalendarConnections",
                columns: new[] { "UserId", "ProfileId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BehavioralGoals_UserId_ProfileId",
                table: "BehavioralGoals",
                columns: new[] { "UserId", "ProfileId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropIndex(name: "IX_Tasks_UserId_ProfileId", table: "Tasks");
            migrationBuilder.DropIndex(name: "IX_Tags_UserId_ProfileId_Name", table: "Tags");
            migrationBuilder.DropIndex(name: "IX_Notes_UserId_ProfileId", table: "Notes");
            migrationBuilder.DropIndex(name: "IX_Meetings_UserId_ProfileId", table: "Meetings");
            migrationBuilder.DropIndex(name: "IX_CalendarConnections_UserId_ProfileId_Provider", table: "CalendarConnections");
            migrationBuilder.DropIndex(name: "IX_BehavioralGoals_UserId_ProfileId", table: "BehavioralGoals");

            migrationBuilder.DropColumn(name: "ProfileId", table: "Tasks");
            migrationBuilder.DropColumn(name: "ProfileId", table: "Tags");
            migrationBuilder.DropColumn(name: "ProfileId", table: "Notes");
            migrationBuilder.DropColumn(name: "ProfileId", table: "Meetings");
            migrationBuilder.DropColumn(name: "ProfileId", table: "CalendarConnections");
            migrationBuilder.DropColumn(name: "ProfileId", table: "BehavioralGoals");

            migrationBuilder.CreateIndex(name: "IX_Tasks_UserId", table: "Tasks", column: "UserId");
            migrationBuilder.CreateIndex(name: "IX_Tags_UserId_Name", table: "Tags", columns: new[] { "UserId", "Name" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_Notes_UserId", table: "Notes", column: "UserId");
            migrationBuilder.CreateIndex(name: "IX_Meetings_UserId", table: "Meetings", column: "UserId");
            migrationBuilder.CreateIndex(name: "IX_CalendarConnections_UserId_Provider", table: "CalendarConnections", columns: new[] { "UserId", "Provider" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_BehavioralGoals_UserId", table: "BehavioralGoals", column: "UserId");
        }
    }
}
