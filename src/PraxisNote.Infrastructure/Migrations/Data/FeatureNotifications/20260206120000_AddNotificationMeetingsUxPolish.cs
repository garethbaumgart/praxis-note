using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationMeetingsUxPolish : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Meetings UX polish',
                    'Dark mode dropdowns now use proper Nord backgrounds, overlays close reliably, status labels are clearer, and the transcript textarea auto-grows with your content.',
                    'https://github.com/garethbaumgart/praxis-note/pull/346',
                    '2026-02-06T12:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Meetings UX polish';
                """);
        }
    }
}
