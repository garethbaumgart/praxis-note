using System.Text.Json;
using Microsoft.Extensions.Logging;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Application.Features.UserAiKeys;
using PraxisNote.Application.Features.UserAiKeys.Services;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Features.Meetings;

public sealed class AnalyzeMeeting(
    IMeetingRepository meetingRepository,
    IResolvedAiServices aiServices,
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
            var meetingAnalyzer = await aiServices.GetMeetingAnalyzerAsync(command.UserId, cancellationToken);
            var result = await meetingAnalyzer.AnalyzeAsync(meeting.TranscriptContent, cancellationToken);

            // Convert extracted action items to domain ActionItem objects
            // Filter out any items with null/empty descriptions to prevent exceptions
            var actionItems = result.ExtractedActionItems?
                .Where(a => !string.IsNullOrWhiteSpace(a.Description))
                .Select(a => ActionItem.Create(a.Description, a.Assignee))
                .ToList()
                ?? [];

            meeting.CompleteAnalysis(
                result.Summary,
                JsonSerializer.Serialize(result.KeyPoints),
                JsonSerializer.Serialize(result.Decisions),
                actionItems,
                result.SuggestedTitle,
                result.SuggestedTags.Count > 0
                    ? JsonSerializer.Serialize(result.SuggestedTags)
                    : null);
        }
        catch (NoAiKeyConfiguredException)
        {
            meeting.FailAnalysis();
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (AiKeyInvalidException ex)
        {
            logger.LogWarning("AI key invalid for meeting {MeetingId}, provider {Provider}", meeting.Id, ex.Provider);
            meeting.FailAnalysis();
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (AiRateLimitedException ex)
        {
            logger.LogInformation("AI rate limited for meeting {MeetingId}, provider {Provider}", meeting.Id, ex.Provider);
            meeting.FailAnalysis();
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (AiProviderException ex)
        {
            logger.LogError(ex, "AI provider error for meeting {MeetingId}", meeting.Id);
            meeting.FailAnalysis();
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
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
                "Unexpected error analyzing meeting {MeetingId} for user {UserId}",
                meeting.Id,
                command.UserId);

            meeting.FailAnalysis();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
