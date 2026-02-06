using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationMeetingEditorLayout : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Improved meeting editor layout',
                    'AI Analysis and tags now appear above the transcript for quicker access. Tags are unified in the Details card alongside suggested tags.',
                    'https://github.com/garethbaumgart/praxis-note/issues/350',
                    '2026-02-06T10:40:13Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Improved meeting editor layout';
                """);
        }
    }
}
