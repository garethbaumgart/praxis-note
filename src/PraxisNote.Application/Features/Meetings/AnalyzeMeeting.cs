using System.Text.Json;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Features.Meetings;

public sealed class AnalyzeMeeting(
    IMeetingRepository meetingRepository,
    IMeetingAnalyzer meetingAnalyzer,
    IUnitOfWork unitOfWork)
{
    public record Command(Guid MeetingId, Guid UserId);

    public async Task<bool> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(command.MeetingId, cancellationToken);

        if (meeting is null || meeting.UserId != command.UserId)
            return false;

        meeting.StartAnalysis();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await meetingAnalyzer.AnalyzeAsync(meeting.TranscriptContent!, cancellationToken);

            meeting.CompleteAnalysis(
                result.Summary,
                JsonSerializer.Serialize(result.KeyPoints),
                JsonSerializer.Serialize(result.Decisions));
        }
        catch
        {
            meeting.FailAnalysis();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
