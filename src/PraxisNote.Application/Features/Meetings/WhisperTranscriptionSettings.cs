namespace PraxisNote.Application.Features.Meetings;

public class WhisperTranscriptionSettings
{
    public const string SectionName = "WhisperTranscription";

    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "whisper-1";
    public long MaxFileSizeBytes { get; set; } = 25 * 1024 * 1024; // 25MB
    public int TimeoutSeconds { get; set; } = 300;
}
