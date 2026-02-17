using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationJiraIntegration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Jira integration for notes',
                    'Connect Jira Cloud and paste issue URLs in notes to see rich inline chips with type, key, summary, and status badge.',
                    'https://github.com/garethbaumgart/praxis-note/pull/589',
                    '2026-02-18T16:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Jira integration for notes';
                """);
        }
    }
}
