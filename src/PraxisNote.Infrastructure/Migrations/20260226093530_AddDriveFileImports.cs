using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDriveFileImports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DriveFileImports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DriveConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriveFileId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FileModifiedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MatchedMeetingId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParsedContent = table.Column<string>(type: "text", nullable: true),
                    ParsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ImportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DiscoveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriveFileImports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriveFileImports_DriveConnectionId_DriveFileId",
                table: "DriveFileImports",
                columns: new[] { "DriveConnectionId", "DriveFileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriveFileImports_DriveConnectionId_Status",
                table: "DriveFileImports",
                columns: new[] { "DriveConnectionId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriveFileImports");
        }
    }
}
