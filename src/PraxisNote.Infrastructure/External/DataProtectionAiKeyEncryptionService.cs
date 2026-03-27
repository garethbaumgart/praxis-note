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

    public string ComputeHint(string plainTextKey)
    {
        if (plainTextKey.Length <= 8)
            return "...";

        var prefix = plainTextKey[..Math.Min(8, plainTextKey.Length)];
        var suffix = plainTextKey[^4..];
        return $"{prefix}...{suffix}";
    }
}
