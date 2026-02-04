using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationSpeakerIdentification : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Speaker identification in transcripts',
                    'Meeting transcripts now show who said what. Speakers are identified using multichannel audio separation and voice diarization.',
                    'https://github.com/garethbaumgart/praxis-note/issues/315',
                    '2026-02-04T12:01:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Speaker identification in transcripts';
                """);
        }
    }
}
