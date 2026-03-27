namespace PraxisNote.Application.Features.UserAiKeys.Services;

public interface IAiKeyEncryptionService
{
    string Encrypt(string plainTextKey);
    string Decrypt(string encryptedKey);
    string ComputeHint(string plainTextKey);
}
