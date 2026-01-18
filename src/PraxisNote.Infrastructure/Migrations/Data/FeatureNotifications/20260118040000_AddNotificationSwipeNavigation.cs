using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    /// <inheritdoc />
    public partial class AddNotificationSwipeNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'NewFeature',
                    'Swipe navigation on mobile',
                    'Swipe left/right to switch between Todo, In Progress, and Done columns. Dots at the bottom show your current position.',
                    'https://github.com/garethbaumgart/praxis-note/issues/147',
                    '2026-01-18T04:00:00Z'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Swipe navigation on mobile'
                  AND "CreatedAt" = '2026-01-18T04:00:00Z';
                """);
        }
    }
}
