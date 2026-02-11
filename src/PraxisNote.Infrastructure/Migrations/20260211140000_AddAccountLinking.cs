using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountLinking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // Phase 1: Create LinkedIdentities table
            // ============================================================
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

            // ============================================================
            // Phase 2: Create AccountLinkCodes table
            // ============================================================
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

            migrationBuilder.CreateIndex(
                name: "IX_AccountLinkCodes_CodeHash",
                table: "AccountLinkCodes",
                column: "CodeHash");

            migrationBuilder.CreateIndex(
                name: "IX_AccountLinkCodes_UserId",
                table: "AccountLinkCodes",
                column: "UserId");

            // ============================================================
            // Phase 3: Seed LinkedIdentity rows from existing User ExternalIdentity data
            // ============================================================
            migrationBuilder.Sql("""
                INSERT INTO "LinkedIdentities" ("Id", "UserId", "Provider", "ProviderId", "Email", "Name", "AvatarUrl", "DefaultProfileId", "LinkedAt")
                SELECT
                    gen_random_uuid(),
                    u."Id",
                    u."ExternalIdentity_Provider",
                    u."ExternalIdentity_ProviderId",
                    LOWER(TRIM(u."Email_Value")),
                    u."Name",
                    u."AvatarUrl",
                    NULL,
                    u."CreatedAt"
                FROM "Users" u;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AccountLinkCodes");
            migrationBuilder.DropTable(name: "LinkedIdentities");
        }
    }
}
