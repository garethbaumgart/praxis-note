using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Application.Features.Tags.Services;
using PraxisNote.Application.Features.UserAiKeys.Services;

namespace PraxisNote.Infrastructure.External;

public sealed class ResolvedAiServices(
    IAiKeyResolver keyResolver,
    IAiProviderFactory providerFactory) : IResolvedAiServices
{
    private readonly Dictionary<Guid, ResolvedAiKey> _keyCache = [];
    private readonly Dictionary<Guid, IMeetingAnalyzer> _analyzerCache = [];
    private readonly Dictionary<Guid, ITagAiChatService> _chatServiceCache = [];

    public async Task<IMeetingAnalyzer> GetMeetingAnalyzerAsync(Guid userId, CancellationToken ct = default)
    {
        if (_analyzerCache.TryGetValue(userId, out var cached))
            return cached;

        var resolved = await ResolveWithCacheAsync(userId, ct);
        var analyzer = providerFactory.CreateMeetingAnalyzer(resolved.ApiKey, resolved.Provider, resolved.Model);
        _analyzerCache[userId] = analyzer;
        return analyzer;
    }

    public async Task<ITagAiChatService> GetTagAiChatServiceAsync(Guid userId, CancellationToken ct = default)
    {
        if (_chatServiceCache.TryGetValue(userId, out var cached))
            return cached;

        var resolved = await ResolveWithCacheAsync(userId, ct);
        var chatService = providerFactory.CreateTagAiChatService(resolved.ApiKey, resolved.Provider, resolved.Model);
        _chatServiceCache[userId] = chatService;
        return chatService;
    }

    private async Task<ResolvedAiKey> ResolveWithCacheAsync(Guid userId, CancellationToken ct)
    {
        if (_keyCache.TryGetValue(userId, out var cached))
            return cached;

        var resolved = await keyResolver.ResolveAsync(userId, ct)
            ?? throw new NoAiKeyConfiguredException();

        _keyCache[userId] = resolved;
        return resolved;
    }
}
