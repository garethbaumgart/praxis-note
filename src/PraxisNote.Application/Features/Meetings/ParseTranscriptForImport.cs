using PraxisNote.Application.Features.Meetings.Services;

namespace PraxisNote.Application.Features.Meetings;

public sealed class ParseTranscriptForImport(
    IMeetingAnalyzer meetingAnalyzer,
    ITranscriptExtractor transcriptExtractor)
{
    private const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const string PlainTextContentType = "text/plain";

    public record Command(Guid UserId, string? UserName, string? TimeZone, string? Text, Stream? FileStream, string? FileContentType, string? FileName);

    public record Result(
        string? Title,
        string? MeetingDate,
        string? Attendees,
        string? Summary,
        List<string>? KeyPoints,
        List<string>? Decisions,
        List<ActionItemResult>? ActionItems,
        List<string> SuggestedTags,
        string Transcript,
        bool IsComplete,
        string? Warning);

    public record ActionItemResult(string Description, string? Assignee);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var text = await ExtractTextAsync(command, cancellationToken);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("No text content could be extracted from the provided input.");
        }

        var parseResult = await meetingAnalyzer.ParseTranscriptForImportAsync(text, command.TimeZone, cancellationToken);

        var suggestedTags = parseResult.SuggestedTags;
        var oneOnOneTag = Get1on1Tag(parseResult.Attendees, command.UserName);
        if (oneOnOneTag is not null && !suggestedTags.Contains(oneOnOneTag, StringComparer.OrdinalIgnoreCase))
        {
            suggestedTags = [oneOnOneTag, .. suggestedTags];
        }

        return new Result(
            parseResult.Title,
            parseResult.MeetingDate?.ToString("o"),
            parseResult.Attendees,
            parseResult.Summary,
            parseResult.KeyPoints,
            parseResult.Decisions,
            parseResult.ActionItems
                .Select(a => new ActionItemResult(a.Description, a.Assignee))
                .ToList(),
            suggestedTags,
            text,
            parseResult.IsComplete,
            parseResult.Warning);
    }

    private static string? Get1on1Tag(string? attendees, string? userName)
    {
        if (string.IsNullOrWhiteSpace(attendees) || string.IsNullOrWhiteSpace(userName))
            return null;

        var names = attendees.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (names.Length != 2)
            return null;

        var userFirst = userName.Trim().Split(' ')[0];

        // Prefer full-name match; fall back to first-name match
        var userMatch =
            names.FirstOrDefault(n => string.Equals(n, userName.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? names.FirstOrDefault(n =>
                string.Equals(n.Split(' ')[0], userFirst, StringComparison.OrdinalIgnoreCase));
        if (userMatch is null)
            return null;

        // Return the other person's first name
        var otherPerson = names.FirstOrDefault(n => n != userMatch);
        return otherPerson?.Split(' ')[0].ToLowerInvariant();
    }

    private async Task<string> ExtractTextAsync(Command command, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(command.Text))
        {
            return command.Text;
        }

        if (command.FileStream is null)
        {
            throw new ArgumentException("Either text or a file must be provided.");
        }

        return command.FileContentType switch
        {
            DocxContentType => await transcriptExtractor.ExtractTextFromDocxAsync(command.FileStream, cancellationToken),
            PlainTextContentType => transcriptExtractor.ExtractTextFromPlainText(command.FileStream),
            _ => throw new InvalidOperationException($"Unsupported file type: {command.FileContentType}. Supported types: .txt, .docx")
        };
    }
}
