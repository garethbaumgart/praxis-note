using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    /// <inheritdoc />
    public partial class AddDriveConnectionSyncTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveFailures",
                table: "DriveConnections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSyncAt",
                table: "DriveConnections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSyncError",
                table: "DriveConnections",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastSyncFilesDiscovered",
                table: "DriveConnections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastSyncFilesErrored",
                table: "DriveConnections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastSyncFilesImported",
                table: "DriveConnections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastSyncFilesPendingReview",
                table: "DriveConnections",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsecutiveFailures",
                table: "DriveConnections");

            migrationBuilder.DropColumn(
                name: "LastSyncAt",
                table: "DriveConnections");

            migrationBuilder.DropColumn(
                name: "LastSyncError",
                table: "DriveConnections");

            migrationBuilder.DropColumn(
                name: "LastSyncFilesDiscovered",
                table: "DriveConnections");

            migrationBuilder.DropColumn(
                name: "LastSyncFilesErrored",
                table: "DriveConnections");

            migrationBuilder.DropColumn(
                name: "LastSyncFilesImported",
                table: "DriveConnections");

            migrationBuilder.DropColumn(
                name: "LastSyncFilesPendingReview",
                table: "DriveConnections");
        }
    }
}
