using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Application.Features.Tags.Services;
using PraxisNote.Application.Features.UserAiKeys.Services;

namespace PraxisNote.Infrastructure.External;

public sealed class ResolvedAiServices(
    IAiKeyResolver keyResolver,
    IAiProviderFactory providerFactory) : IResolvedAiServices
{
    public async Task<IMeetingAnalyzer> GetMeetingAnalyzerAsync(Guid userId, CancellationToken ct = default)
    {
        var resolved = await keyResolver.ResolveAsync(userId, ct)
            ?? throw new NoAiKeyConfiguredException();

        return providerFactory.CreateMeetingAnalyzer(resolved.ApiKey, resolved.Provider, resolved.Model);
    }

    public async Task<ITagAiChatService> GetTagAiChatServiceAsync(Guid userId, CancellationToken ct = default)
    {
        var resolved = await keyResolver.ResolveAsync(userId, ct)
            ?? throw new NoAiKeyConfiguredException();

        return providerFactory.CreateTagAiChatService(resolved.ApiKey, resolved.Provider, resolved.Model);
    }
}
