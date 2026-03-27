using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Application.Features.Tags.Services;
using PraxisNote.Application.Features.UserAiKeys.Services;

namespace PraxisNote.Infrastructure.External;

public sealed class ResolvedAiServices(
    IAiKeyResolver keyResolver,
    IAiProviderFactory providerFactory) : IResolvedAiServices
{
    private readonly Dictionary<Guid, ResolvedAiKey> _cache = [];

    public async Task<IMeetingAnalyzer> GetMeetingAnalyzerAsync(Guid userId, CancellationToken ct = default)
    {
        var resolved = await ResolveWithCacheAsync(userId, ct);
        return providerFactory.CreateMeetingAnalyzer(resolved.ApiKey, resolved.Provider, resolved.Model);
    }

    public async Task<ITagAiChatService> GetTagAiChatServiceAsync(Guid userId, CancellationToken ct = default)
    {
        var resolved = await ResolveWithCacheAsync(userId, ct);
        return providerFactory.CreateTagAiChatService(resolved.ApiKey, resolved.Provider, resolved.Model);
    }

    private async Task<ResolvedAiKey> ResolveWithCacheAsync(Guid userId, CancellationToken ct)
    {
        if (_cache.TryGetValue(userId, out var cached))
            return cached;

        var resolved = await keyResolver.ResolveAsync(userId, ct)
            ?? throw new NoAiKeyConfiguredException();

        _cache[userId] = resolved;
        return resolved;
    }
}
