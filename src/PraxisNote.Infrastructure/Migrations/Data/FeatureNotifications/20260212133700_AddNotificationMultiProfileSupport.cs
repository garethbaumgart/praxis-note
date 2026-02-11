using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationMultiProfileSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Multi-profile support',
                    'Create separate profiles (e.g., Work, Personal) to organize your tasks, notes, and meetings. Switch profiles from the sidebar and manage them in Settings.',
                    'https://github.com/garethbaumgart/praxis-note/issues/476',
                    '2026-02-12T13:37:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Multi-profile support';
                """);
        }
    }
}
