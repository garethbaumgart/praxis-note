using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PraxisNote.Domain.Aggregates.DriveFileImports;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Features.Drive.Services;

public sealed partial class DriveDeduplicationService(
    IMeetingRepository meetingRepository,
    ILogger<DriveDeduplicationService> logger) : IDriveDeduplicationService
{
    // Matches URLs like: https://calendar.google.com/event?eid=xxx or https://meet.google.com/xxx-xxxx-xxx
    [GeneratedRegex(@"(?:calendar\.google\.com/.*[?&]eid=|meet\.google\.com/)([a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase)]
    private static partial Regex CalendarEventIdRegex();

    private const double DateProximityHours = 1.0;
    private const decimal AttendeeOverlapThreshold = 0.50m;
    private const decimal MinimumFuzzyConfidence = 0.5m;

    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task DeduplicateAsync(
        Guid userId,
        Guid profileId,
        IReadOnlyList<DriveFileImport> parsedFiles,
        CancellationToken cancellationToken = default)
    {
        if (parsedFiles.Count == 0) return;

        var earliestDate = GetEarliestParsedDate(parsedFiles);
        var lookbackDate = earliestDate.AddDays(-7);

        var existingMeetings = await meetingRepository.GetRecentMeetingsForDedupAsync(
            userId, profileId, lookbackDate, cancellationToken);

        foreach (var fileImport in parsedFiles)
        {
            if (fileImport.Status != DriveFileImportStatus.Parsed) continue;
            if (string.IsNullOrEmpty(fileImport.ParsedResultJson)) continue;

            var parsed = DeserializeParsedResult(fileImport.ParsedResultJson);
            if (parsed is null) continue;

            // Layer 2: Calendar Event ID match
            var calendarMatch = TryMatchCalendarEventId(parsed, existingMeetings);
            if (calendarMatch is not null)
            {
                fileImport.MarkDuplicate(
                    DeduplicationType.CalendarEvent,
                    calendarMatch.Id,
                    calendarMatch.Title,
                    1.0m);
                logger.LogInformation(
                    "Drive file duplicate detected (Calendar Event ID) — matched meeting '{MeetingTitle}'",
                    calendarMatch.Title);
                continue;
            }

            // Layer 3: Fuzzy title + date + attendees
            var fuzzyMatch = TryFuzzyMatch(parsed, existingMeetings);
            if (fuzzyMatch.HasValue)
            {
                fileImport.MarkDuplicate(
                    DeduplicationType.FuzzyMatch,
                    fuzzyMatch.Value.Meeting.Id,
                    fuzzyMatch.Value.Meeting.Title,
                    fuzzyMatch.Value.Confidence);
                logger.LogInformation(
                    "Drive file duplicate detected (Fuzzy Match, confidence={Confidence:F2}) — matched meeting '{MeetingTitle}'",
                    fuzzyMatch.Value.Confidence, fuzzyMatch.Value.Meeting.Title);
            }
        }
    }

    internal Meeting? TryMatchCalendarEventId(ParsedFileResult parsed, IReadOnlyList<Meeting> meetings)
    {
        // Extract calendar event IDs from the transcript/content
        var eventIds = ExtractCalendarEventIds(parsed.Transcript);
        if (eventIds.Count == 0) return null;

        foreach (var meeting in meetings)
        {
            if (string.IsNullOrWhiteSpace(meeting.CalendarEventId)) continue;

            if (eventIds.Contains(meeting.CalendarEventId))
            {
                return meeting;
            }
        }

        return null;
    }

    internal (Meeting Meeting, decimal Confidence)? TryFuzzyMatch(
        ParsedFileResult parsed, IReadOnlyList<Meeting> meetings)
    {
        if (string.IsNullOrWhiteSpace(parsed.Title)) return null;

        DateTimeOffset? parsedDate = null;
        if (!string.IsNullOrWhiteSpace(parsed.MeetingDate) &&
            DateTimeOffset.TryParse(parsed.MeetingDate, out var d))
        {
            parsedDate = d;
        }

        (Meeting Meeting, decimal Confidence)? bestMatch = null;

        foreach (var meeting in meetings)
        {
            var confidence = 0m;

            // Title comparison — case-insensitive contains in either direction
            var titleMatch = false;
            if (!string.IsNullOrWhiteSpace(meeting.Title))
            {
                titleMatch = meeting.Title.Contains(parsed.Title, StringComparison.OrdinalIgnoreCase)
                          || parsed.Title.Contains(meeting.Title, StringComparison.OrdinalIgnoreCase);
            }

            if (!titleMatch) continue;

            confidence += 0.4m; // Base for title match

            // Date proximity
            if (parsedDate.HasValue && meeting.MeetingDate.HasValue)
            {
                if (AreDatesWithinProximity(parsedDate, meeting.MeetingDate))
                {
                    confidence += 0.35m;
                }
            }

            // Attendee overlap
            var overlap = CalculateAttendeeOverlap(parsed.Attendees, meeting.Attendees);
            if (overlap >= AttendeeOverlapThreshold)
            {
                confidence += 0.25m * overlap;
            }

            if (confidence >= MinimumFuzzyConfidence)
            {
                var capped = Math.Min(confidence, 1.0m);
                if (bestMatch is null || capped > bestMatch.Value.Confidence)
                {
                    bestMatch = (meeting, capped);
                }
            }
        }

        return bestMatch;
    }

    internal static decimal CalculateAttendeeOverlap(string? parsedAttendees, string? existingAttendees)
    {
        if (string.IsNullOrWhiteSpace(parsedAttendees) || string.IsNullOrWhiteSpace(existingAttendees))
            return 0m;

        var parsedSet = parsedAttendees
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(n => n.ToLower())
            .ToHashSet();

        var existingSet = existingAttendees
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(n => n.ToLower())
            .ToHashSet();

        if (parsedSet.Count == 0 || existingSet.Count == 0)
            return 0m;

        var intersection = parsedSet.Intersect(existingSet).Count();
        var union = parsedSet.Union(existingSet).Count();

        return union == 0 ? 0m : (decimal)intersection / union;
    }

    internal static bool AreDatesWithinProximity(DateTimeOffset? date1, DateTimeOffset? date2)
    {
        if (!date1.HasValue || !date2.HasValue) return false;
        return Math.Abs((date1.Value - date2.Value).TotalHours) <= DateProximityHours;
    }

    internal static DateTimeOffset GetEarliestParsedDate(IReadOnlyList<DriveFileImport> files)
    {
        var earliest = DateTimeOffset.UtcNow;

        foreach (var file in files)
        {
            if (string.IsNullOrEmpty(file.ParsedResultJson)) continue;

            var parsed = DeserializeParsedResult(file.ParsedResultJson);
            if (parsed is null || string.IsNullOrWhiteSpace(parsed.MeetingDate)) continue;

            if (DateTimeOffset.TryParse(parsed.MeetingDate, out var date) && date < earliest)
            {
                earliest = date;
            }
        }

        return earliest;
    }

    internal static HashSet<string> ExtractCalendarEventIds(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return [];

        var matches = CalendarEventIdRegex().Matches(content);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1 && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
            {
                ids.Add(match.Groups[1].Value);
            }
        }

        return ids;
    }

    internal static ParsedFileResult? DeserializeParsedResult(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ParsedFileResult>(json, CamelCaseOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>
/// Internal DTO for deserialized parsed result (from ParseTranscriptForImport.Result JSON).
/// </summary>
internal sealed record ParsedFileResult(
    string? Title,
    string? MeetingDate,
    string? Attendees,
    string? Summary,
    string? Transcript);
