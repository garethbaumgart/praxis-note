namespace PraxisNote.Application.Features.Meetings.Services;

public interface ITranscriptExtractor
{
    Task<string> ExtractTextFromDocxAsync(Stream fileStream, CancellationToken cancellationToken = default);
    string ExtractTextFromPlainText(Stream fileStream);
}
