using System.Text.Json;
using Microsoft.Extensions.Logging;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Features.Meetings;

public sealed class AnalyzeMeeting(
    IMeetingRepository meetingRepository,
    IMeetingAnalyzer meetingAnalyzer,
    IUnitOfWork unitOfWork,
    ILogger<AnalyzeMeeting> logger)
{
    public record Command(Guid MeetingId, Guid UserId);

    public async Task<bool> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(command.MeetingId, cancellationToken);

        if (meeting is null || meeting.UserId != command.UserId)
            return false;

        // Validate transcript exists before starting analysis
        if (string.IsNullOrWhiteSpace(meeting.TranscriptContent))
            return false;

        // Prevent re-triggering analysis while already processing
        if (meeting.Status == MeetingStatus.Processing)
            return false;

        meeting.StartAnalysis();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await meetingAnalyzer.AnalyzeAsync(meeting.TranscriptContent, cancellationToken);

            meeting.CompleteAnalysis(
                result.Summary,
                JsonSerializer.Serialize(result.KeyPoints),
                JsonSerializer.Serialize(result.Decisions),
                result.BehavioralAnalysis is not null
                    ? JsonSerializer.Serialize(result.BehavioralAnalysis)
                    : null);
        }
        catch (OperationCanceledException)
        {
            // Mark as failed before propagating so meeting isn't stuck in Processing
            meeting.FailAnalysis();
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to analyze meeting {MeetingId} for user {UserId}",
                meeting.Id,
                command.UserId);

            meeting.FailAnalysis();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
