using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationHomeDashboard : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Action-First Home Dashboard',
                    'The home screen now shows overdue tasks, upcoming meetings, quick actions, and your recent notes and meetings so you can jump right back into your work.',
                    'https://github.com/garethbaumgart/praxis-note/pull/351',
                    '2026-02-07T01:00:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Action-First Home Dashboard';
                """);
        }
    }
}
