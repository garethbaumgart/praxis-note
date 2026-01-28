using Microsoft.Extensions.Logging;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Features.Meetings;

public sealed class TranscribeAudio(
    IMeetingRepository meetingRepository,
    ITranscriptionService transcriptionService,
    IUnitOfWork unitOfWork,
    ILogger<TranscribeAudio> logger)
{
    public record Command(Guid MeetingId, Guid UserId, Stream AudioStream, string FileName);

    public async Task<bool> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(command.MeetingId, cancellationToken);

        if (meeting is null || meeting.UserId != command.UserId)
            return false;

        // Prevent re-triggering while already processing
        if (meeting.Status == MeetingStatus.Processing)
            return false;

        meeting.StartTranscription();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await transcriptionService.TranscribeAsync(
                command.AudioStream, command.FileName, cancellationToken);

            meeting.CompleteTranscription(result.Text);
        }
        catch (OperationCanceledException)
        {
            meeting.FailAnalysis();
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to transcribe audio for meeting {MeetingId}, user {UserId}",
                meeting.Id,
                command.UserId);

            meeting.FailAnalysis();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
