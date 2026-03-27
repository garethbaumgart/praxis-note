using PraxisNote.Application.Features.Tags.Services;
using PraxisNote.Domain.Aggregates.UserAiKeys;

namespace PraxisNote.Application.Features.Meetings.Services;

public interface IAiProviderFactory
{
    IMeetingAnalyzer CreateMeetingAnalyzer(string apiKey, AiProvider provider, string model);
    ITagAiChatService CreateTagAiChatService(string apiKey, AiProvider provider, string model);
}
