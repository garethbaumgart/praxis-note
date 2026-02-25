using System.Globalization;
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

        var personTags = GetAttendeePersonTags(parseResult.Attendees, command.UserName);
        var aiTags = parseResult.SuggestedTags;

        // Prepend person tags, then AI tags, with global case-insensitive deduplication
        var suggestedTags = new List<string>();

        foreach (var pt in personTags)
        {
            if (!suggestedTags.Contains(pt, StringComparer.OrdinalIgnoreCase))
                suggestedTags.Add(pt);
        }

        foreach (var aiTag in aiTags)
        {
            if (!suggestedTags.Contains(aiTag, StringComparer.OrdinalIgnoreCase))
                suggestedTags.Add(aiTag);
        }

        // Auto-tag ad-hoc meetings
        if (parseResult.IsAdhoc)
        {
            const string adhocTag = "adhoc-call";
            if (!suggestedTags.Contains(adhocTag, StringComparer.OrdinalIgnoreCase))
                suggestedTags.Add(adhocTag);
        }

        return new Result(
            parseResult.Title,
            parseResult.MeetingDate?.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture),
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

    private static List<string> GetAttendeePersonTags(string? attendees, string? userName)
    {
        if (string.IsNullOrWhiteSpace(attendees))
            return [];

        var names = attendees.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (names.Length == 0)
            return [];

        var normalizedUser = userName?.Trim();
        var userFirst = normalizedUser?
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        var personTags = new List<string>();
        foreach (var name in names)
        {
            var nameParts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var attendeeFirst = nameParts.FirstOrDefault();

            // Skip the current user (full-name match, or first-name match only for single-word names)
            if (!string.IsNullOrWhiteSpace(normalizedUser) &&
                (string.Equals(name, normalizedUser, StringComparison.OrdinalIgnoreCase) ||
                 (nameParts.Length == 1 &&
                  !string.IsNullOrWhiteSpace(attendeeFirst) &&
                  string.Equals(attendeeFirst, userFirst, StringComparison.OrdinalIgnoreCase))))
            {
                continue;
            }

            // Convert "First Last" → "first-last"
            var tag = string.Join("-", nameParts).ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(tag))
            {
                personTags.Add(tag);
            }
        }

        return personTags;
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
