using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationAccountLinkingDataTransfer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'BugFix',
                    'Account linking preserves your data',
                    'Linking accounts now safely transfers all your tasks, notes, meetings, tags, and goals to the target account before completing the link.',
                    'https://github.com/garethbaumgart/praxis-note/issues/518',
                    '2026-02-14T12:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Account linking preserves your data';
                """);
        }
    }
}
