using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDriveConnectionConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoAcceptTags",
                table: "DriveConnections",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "InitialImportCutoffDate",
                table: "DriveConnections",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SyncFrequencyMinutes",
                table: "DriveConnections",
                type: "integer",
                nullable: false,
                defaultValue: 15);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoAcceptTags",
                table: "DriveConnections");

            migrationBuilder.DropColumn(
                name: "InitialImportCutoffDate",
                table: "DriveConnections");

            migrationBuilder.DropColumn(
                name: "SyncFrequencyMinutes",
                table: "DriveConnections");
        }
    }
}
