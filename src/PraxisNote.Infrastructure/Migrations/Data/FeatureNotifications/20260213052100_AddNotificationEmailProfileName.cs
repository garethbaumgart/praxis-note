using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationEmailProfileName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Improvement',
                    'Smarter profile naming on account link',
                    'New profiles created during account linking now default to the linked email address, making it easier to tell profiles apart when both accounts share the same name.',
                    'https://github.com/garethbaumgart/praxis-note/issues/499',
                    '2026-02-13T05:21:00+00:00'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Smarter profile naming on account link';
                """);
        }
    }
}
