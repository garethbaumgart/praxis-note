using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Application.Features.Tags.Services;
using PraxisNote.Application.Features.UserAiKeys.Services;

namespace PraxisNote.Infrastructure.External;

public sealed class ResolvedAiServices(
    IAiKeyResolver keyResolver,
    IAiProviderFactory providerFactory) : IResolvedAiServices
{
    // Scoped per-request — single user per instance, so nullable fields are sufficient and thread-safe.
    private ResolvedAiKey? _cachedKey;
    private IMeetingAnalyzer? _cachedAnalyzer;
    private ITagAiChatService? _cachedChatService;

    public async Task<IMeetingAnalyzer> GetMeetingAnalyzerAsync(Guid userId, CancellationToken ct = default)
    {
        if (_cachedAnalyzer is not null)
            return _cachedAnalyzer;

        var resolved = await ResolveWithCacheAsync(userId, ct);
        _cachedAnalyzer = providerFactory.CreateMeetingAnalyzer(resolved.ApiKey, resolved.Provider, resolved.Model);
        return _cachedAnalyzer;
    }

    public async Task<ITagAiChatService> GetTagAiChatServiceAsync(Guid userId, CancellationToken ct = default)
    {
        if (_cachedChatService is not null)
            return _cachedChatService;

        var resolved = await ResolveWithCacheAsync(userId, ct);
        _cachedChatService = providerFactory.CreateTagAiChatService(resolved.ApiKey, resolved.Provider, resolved.Model);
        return _cachedChatService;
    }

    private async Task<ResolvedAiKey> ResolveWithCacheAsync(Guid userId, CancellationToken ct)
    {
        if (_cachedKey is not null)
            return _cachedKey;

        _cachedKey = await keyResolver.ResolveAsync(userId, ct)
            ?? throw new NoAiKeyConfiguredException();

        return _cachedKey;
    }
}
