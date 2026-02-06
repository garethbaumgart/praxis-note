using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationToastTemplateFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'BugFix',
                    'Toast notifications restored',
                    'Toast notifications now display with the correct Nord-themed styling, including colored backgrounds, severity icons, and animated progress bars.',
                    'https://github.com/garethbaumgart/praxis-note/issues/379',
                    '2026-02-06T21:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Type" = 'BugFix'
                  AND "Title" = 'Toast notifications restored'
                  AND "IssueUrl" = 'https://github.com/garethbaumgart/praxis-note/issues/379'
                  AND "CreatedAt" = '2026-02-06T21:00:00Z';
                """);
        }
    }
}
