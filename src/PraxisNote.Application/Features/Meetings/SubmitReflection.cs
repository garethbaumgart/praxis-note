using System.Text.Json;
using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Features.Meetings;

public sealed class SubmitReflection(IMeetingRepository meetingRepository, IUnitOfWork unitOfWork)
{
    public record Command(
        Guid MeetingId,
        Guid UserId,
        int? SelfAssessedTalkTime,
        string? SelfAssessedEngagement,
        string? SelfAssessedTone,
        string? InterruptionAwareness,
        string? FreeformReflection,
        IReadOnlyList<PromptResponseDto> PromptResponses);

    public async Task<bool> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(command.MeetingId, cancellationToken);

        if (meeting is null || meeting.UserId != command.UserId)
            return false;

        var reflectionData = new ReflectionDto(
            command.SelfAssessedTalkTime,
            command.SelfAssessedEngagement,
            command.SelfAssessedTone,
            command.InterruptionAwareness,
            command.FreeformReflection,
            command.PromptResponses);

        var json = JsonSerializer.Serialize(reflectionData, JsonOptions);
        meeting.SubmitReflection(json);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
