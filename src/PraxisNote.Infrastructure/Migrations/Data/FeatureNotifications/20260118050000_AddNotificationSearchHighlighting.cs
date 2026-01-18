using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    /// <inheritdoc />
    public partial class AddNotificationSearchHighlighting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Search term highlighting',
                    'Matching search text is now highlighted in violet in task titles for easy identification in both light and dark modes.',
                    'https://github.com/garethbaumgart/praxis-note/pull/169',
                    '2026-01-18T05:00:00Z'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Search term highlighting'
                  AND "CreatedAt" = '2026-01-18T05:00:00Z';
                """);
        }
    }
}
