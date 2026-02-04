using PraxisNote.Application.Features.Meetings.Services;

namespace PraxisNote.Application.Features.Meetings;

public sealed class ExtractMeetingsFromScreenshot(IMeetingAnalyzer meetingAnalyzer)
{
    public record Command(Guid UserId, string Base64Image, string MediaType);
    public record Result(List<ExtractedCalendarEvent> Events);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var extraction = await meetingAnalyzer.ExtractFromScreenshotAsync(
            command.Base64Image, command.MediaType, cancellationToken);

        return new Result(extraction.Events);
    }
}
