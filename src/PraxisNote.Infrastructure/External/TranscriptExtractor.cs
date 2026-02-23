using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PraxisNote.Application.Features.Meetings.Services;

namespace PraxisNote.Infrastructure.External;

public sealed class TranscriptExtractor : ITranscriptExtractor
{
    public Task<string> ExtractTextFromDocxAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        using var doc = WordprocessingDocument.Open(fileStream, isEditable: false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return Task.FromResult(string.Empty);

        var text = string.Join("\n", body.Elements<Paragraph>().Select(p => p.InnerText));
        return Task.FromResult(text);
    }

    public string ExtractTextFromPlainText(Stream fileStream)
    {
        using var reader = new StreamReader(fileStream);
        return reader.ReadToEnd();
    }
}
