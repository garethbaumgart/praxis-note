using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationMeetingEditorRedesign : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Redesigned meeting editor',
                    'Meeting editor now has collapsible sections with progressive disclosure. Details collapse to a compact summary, and recording buttons are front and center.',
                    'https://github.com/garethbaumgart/praxis-note/issues/549',
                    '2026-02-15T16:12:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Redesigned meeting editor';
                """);
        }
    }
}
