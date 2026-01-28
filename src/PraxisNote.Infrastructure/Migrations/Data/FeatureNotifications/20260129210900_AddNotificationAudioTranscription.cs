using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations.Data.FeatureNotifications
{
    public partial class AddNotificationAudioTranscription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    'Feature',
                    'Audio transcription for meetings',
                    'Upload audio files (mp3, wav, webm, etc.) in the meeting editor to automatically transcribe them into text using OpenAI Whisper.',
                    'https://github.com/garethbaumgart/praxis-note/issues/234',
                    '2026-01-29T21:09:00Z'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = 'Audio transcription for meetings';
                """);
        }
    }
}
