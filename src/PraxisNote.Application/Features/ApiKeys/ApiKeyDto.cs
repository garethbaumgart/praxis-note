namespace PraxisNote.Application.Features.ApiKeys;

public record ApiKeyDto(
    Guid Id, string Name, string Prefix, Guid ProfileId,
    DateTimeOffset CreatedAt, DateTimeOffset? LastUsedAt,
    DateTimeOffset? ExpiresAt, bool IsRevoked);
