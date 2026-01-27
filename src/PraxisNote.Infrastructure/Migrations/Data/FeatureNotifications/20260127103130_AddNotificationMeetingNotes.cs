using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationMeetingNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Meeting Notes',
                    'Capture and organize meetings with a daily grouped list. Create, edit, and delete meetings with date, time, and attendees.',
                    'https://github.com/garethbaumgart/praxis-note/issues/228',
                    '2026-01-27T10:31:30Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Meeting Notes'
                  AND "IssueUrl" = 'https://github.com/garethbaumgart/praxis-note/issues/228'
                  AND "CreatedAt" = '2026-01-27T10:31:30Z';
                """);
        }
    }
}
