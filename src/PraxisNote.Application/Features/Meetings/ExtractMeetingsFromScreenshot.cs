using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Application.Features.UserAiKeys.Services;

namespace PraxisNote.Application.Features.Meetings;

public sealed class ExtractMeetingsFromScreenshot(IResolvedAiServices aiServices)
{
    public record Command(Guid UserId, string Base64Image, string MediaType, string? TimeZone = null);
    public record Result(List<ExtractedCalendarEvent> Events);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var meetingAnalyzer = await aiServices.GetMeetingAnalyzerAsync(command.UserId, cancellationToken);
        var extraction = await meetingAnalyzer.ExtractFromScreenshotAsync(
            command.Base64Image, command.MediaType, command.TimeZone, cancellationToken);

        return new Result(extraction.Events);
    }
}
