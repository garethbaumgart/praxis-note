using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationNeuralNetworkLogo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'New Neural Network app icon',
                    'PraxisNote now has a custom brain-inspired logo representing intelligent note-taking and connected thinking.',
                    'https://github.com/garethbaumgart/praxis-note/pull/310',
                    '2026-02-03T09:53:00.000Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'New Neural Network app icon';
                """);
        }
    }
}
