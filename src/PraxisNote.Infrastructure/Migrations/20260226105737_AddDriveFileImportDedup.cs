using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDriveFileImportDedup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DuplicateConfidence",
                table: "DriveFileImports",
                type: "numeric(3,2)",
                precision: 3,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DuplicateMatchTitle",
                table: "DriveFileImports",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DuplicateType",
                table: "DriveFileImports",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DuplicateConfidence",
                table: "DriveFileImports");

            migrationBuilder.DropColumn(
                name: "DuplicateMatchTitle",
                table: "DriveFileImports");

            migrationBuilder.DropColumn(
                name: "DuplicateType",
                table: "DriveFileImports");
        }
    }
}
