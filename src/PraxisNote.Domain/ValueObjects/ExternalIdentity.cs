using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.ValueObjects;

/// <summary>
/// Represents an external OAuth provider identity.
/// Uniqueness is determined by the combination of Provider and ProviderId.
/// </summary>
public sealed record ExternalIdentity : ValueObject
{
    /// <summary>
    /// The OAuth provider name (e.g., "Google", "Microsoft", "GitHub").
    /// </summary>
    public string Provider { get; }

    /// <summary>
    /// The unique identifier from the OAuth provider.
    /// </summary>
    public string ProviderId { get; }

    public ExternalIdentity(string provider, string providerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        Provider = provider.Trim().ToLowerInvariant();
        ProviderId = providerId.Trim();
    }

    public override string ToString() => $"{Provider}:{ProviderId}";
}
