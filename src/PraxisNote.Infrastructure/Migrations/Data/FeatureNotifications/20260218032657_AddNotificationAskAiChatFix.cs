using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationAskAiChatFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'BugFix',
                    'Ask AI chat fix in Tag Hub',
                    'The Ask AI button in the Tag Hub now correctly opens the chat panel instead of appearing to do nothing.',
                    'https://github.com/garethbaumgart/praxis-note/pull/595',
                    '2026-02-18T03:26:57Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Type" = 'BugFix'
                  AND "Title" = 'Ask AI chat fix in Tag Hub'
                  AND "IssueUrl" = 'https://github.com/garethbaumgart/praxis-note/pull/595';
                """);
        }
    }
}
