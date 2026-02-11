using System.Security.Cryptography;
using System.Text;

namespace PraxisNote.Application.Features.AccountLinking;

/// <summary>
/// Utility service for generating and hashing account link codes.
/// </summary>
public static class LinkCodeService
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Excludes I, O, 0, 1 for readability
    private const int CodeLength = 8;

    /// <summary>
    /// Generates a random 8-character alphanumeric code formatted as PRAXIS-XXXX-XXXX.
    /// </summary>
    public static string GenerateCode()
    {
        var chars = new char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        var raw = new string(chars);
        return $"PRAXIS-{raw[..4]}-{raw[4..]}";
    }

    /// <summary>
    /// Computes a SHA-256 hash of the provided code (case-insensitive).
    /// The code is normalized by removing "PRAXIS-" prefix and dashes, converting to uppercase.
    /// </summary>
    public static string HashCode(string code)
    {
        var normalized = NormalizeCode(code);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Normalizes a code by stripping the prefix and dashes, converting to uppercase.
    /// </summary>
    private static string NormalizeCode(string code)
    {
        return code
            .ToUpperInvariant()
            .Replace("PRAXIS-", "", StringComparison.OrdinalIgnoreCase)
            .Replace("-", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }
}
