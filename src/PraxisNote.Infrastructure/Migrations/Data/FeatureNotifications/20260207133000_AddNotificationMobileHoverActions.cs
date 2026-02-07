using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationMobileHoverActions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Mobile-friendly action buttons',
                    'Delete and open-in-new-tab buttons on meetings, notes, and tags are now visible and tappable on touch devices.',
                    'https://github.com/garethbaumgart/praxis-note/issues/422',
                    '2026-02-07T13:30:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Mobile-friendly action buttons';
                """);
        }
    }
}
