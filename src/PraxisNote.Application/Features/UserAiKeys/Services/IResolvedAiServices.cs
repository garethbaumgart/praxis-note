using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Application.Features.Tags.Services;

namespace PraxisNote.Application.Features.UserAiKeys.Services;

public interface IResolvedAiServices
{
    Task<IMeetingAnalyzer> GetMeetingAnalyzerAsync(Guid userId, CancellationToken ct = default);
    Task<ITagAiChatService> GetTagAiChatServiceAsync(Guid userId, CancellationToken ct = default);
}
