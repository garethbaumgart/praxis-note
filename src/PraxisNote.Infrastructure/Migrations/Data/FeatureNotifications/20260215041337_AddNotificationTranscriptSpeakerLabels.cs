using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationTranscriptSpeakerLabels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'BugFix',
                    'Fixed transcript speaker labels',
                    'Online meeting transcripts now correctly distinguish between speakers instead of showing only your name for all participants.',
                    'https://github.com/garethbaumgart/praxis-note/issues/541',
                    '2026-02-15T04:13:37Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Fixed transcript speaker labels';
                """);
        }
    }
}
