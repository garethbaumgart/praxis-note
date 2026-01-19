using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    /// <inheritdoc />
    public partial class AddNotificationMobileStatusButtons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'One-tap status buttons on mobile',
                    'Tap arrow buttons on task cards to quickly move tasks between Todo, In Progress, and Done columns on mobile.',
                    'https://github.com/garethbaumgart/praxis-note/issues/166',
                    '2026-01-19T07:30:00Z'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'One-tap status buttons on mobile'
                  AND "CreatedAt" = '2026-01-19T07:30:00Z';
                """);
        }
    }
}
