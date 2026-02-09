using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationDynamicBreadcrumbs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Dynamic breadcrumbs',
                    'Breadcrumbs now show where you navigated from — e.g. "Home / Meeting Title" when opening from the dashboard.',
                    'https://github.com/garethbaumgart/praxis-note/issues/454',
                    '2026-02-09T11:04:28Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Dynamic breadcrumbs';
                """);
        }
    }
}
