using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationAnalysisStatusFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'BugFix',
                    'Clearer recording vs analyzing status',
                    'Meeting cards now correctly show "Recording" while recording and "Analyzing" during AI analysis, with distinct visual styles for each state.',
                    'https://github.com/garethbaumgart/praxis-note/issues/324',
                    '2026-02-05T09:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Clearer recording vs analyzing status';
                """);
        }
    }
}
