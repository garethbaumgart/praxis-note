using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJiraConnection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JiraConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CloudId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SiteUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AccessToken = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    RefreshToken = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    TokenExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConnectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JiraConnections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JiraConnections_UserId_ProfileId",
                table: "JiraConnections",
                columns: new[] { "UserId", "ProfileId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JiraConnections");
        }
    }
}
