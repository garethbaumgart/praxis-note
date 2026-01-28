namespace PraxisNote.Application.Features.Meetings.Services;

public interface ITranscriptionService
{
    Task<TranscriptionResult> TranscribeAsync(Stream audioStream, string fileName, CancellationToken cancellationToken = default);
}

public record TranscriptionResult(string Text, string? Language = null);
