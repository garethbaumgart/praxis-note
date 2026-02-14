using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationLoginPageRedesign : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Redesigned login page',
                    'The login page now features the PraxisNote woven logo with a cleaner, centered layout that looks great on all screen sizes.',
                    'https://github.com/garethbaumgart/praxis-note/pull/530',
                    '2026-02-14T09:38:14Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Redesigned login page';
                """);
        }
    }
}
