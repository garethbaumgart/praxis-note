namespace PraxisNote.Domain.Aggregates.UserAiKeys;

public sealed class UserAiKeyNotFoundException(Guid userId, AiProvider provider)
    : Exception($"AI key not found for user '{userId}' and provider '{provider}'")
{
    public Guid UserId { get; } = userId;
    public AiProvider Provider { get; } = provider;
}
