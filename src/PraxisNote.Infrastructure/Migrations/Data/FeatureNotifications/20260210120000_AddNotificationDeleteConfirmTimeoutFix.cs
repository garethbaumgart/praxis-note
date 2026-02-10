using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationDeleteConfirmTimeoutFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'BugFix',
                    'Delete confirmations now timeout independently',
                    'Fixed a bug where only the last delete confirmation would auto-dismiss. Multiple delete confirmations now each have their own independent 5-second timeout.',
                    'https://github.com/garethbaumgart/praxis-note/issues/465',
                    '2026-02-10T12:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Delete confirmations now timeout independently';
                """);
        }
    }
}
