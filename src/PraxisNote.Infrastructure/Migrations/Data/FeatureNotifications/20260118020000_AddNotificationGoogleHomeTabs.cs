using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    /// <inheritdoc />
    public partial class AddNotificationGoogleHomeTabs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Redesigned task metadata',
                    'Due dates and comments now use Google Home-style expandable tabs for a cleaner look.',
                    'https://github.com/garethbaumgart/praxis-note/issues/156',
                    '2026-01-18T02:00:00Z'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Redesigned task metadata'
                  AND "CreatedAt" = '2026-01-18T02:00:00Z';
                """);
        }
    }
}
