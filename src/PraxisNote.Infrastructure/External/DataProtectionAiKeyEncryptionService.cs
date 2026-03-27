using Microsoft.AspNetCore.DataProtection;
using PraxisNote.Application.Features.UserAiKeys.Services;

namespace PraxisNote.Infrastructure.External;

public sealed class DataProtectionAiKeyEncryptionService : IAiKeyEncryptionService
{
    private readonly IDataProtector _protector;

    public DataProtectionAiKeyEncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("PraxisNote.UserAiKeys.v1");
    }

    public string Encrypt(string plainTextKey) => _protector.Protect(plainTextKey);

    public string Decrypt(string encryptedKey) => _protector.Unprotect(encryptedKey);

    /// <summary>
    /// Returns a masked hint showing only the last 4 characters.
    /// Assumes <paramref name="plainTextKey"/> is at least 8 characters (API keys are).
    /// </summary>
    public string ComputeHint(string plainTextKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainTextKey, nameof(plainTextKey));

        if (plainTextKey.Length <= 8)
            return "...";

        var suffix = plainTextKey[^4..];
        return $"...{suffix}";
    }
}
