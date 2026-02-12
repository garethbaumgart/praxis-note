using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBlindSpotNudgesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_BlindSpotNudges_UserId_ProfileId",
                table: "BlindSpotNudges",
                columns: new[] { "UserId", "ProfileId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlindSpotNudges");
        }
    }
}
