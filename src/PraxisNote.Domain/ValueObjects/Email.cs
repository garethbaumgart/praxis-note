using System.Net.Mail;
using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.ValueObjects;

/// <summary>
/// Represents a validated email address.
/// Stored in lowercase for case-insensitive equality.
/// </summary>
public sealed record Email : ValueObject
{
    public string Value { get; }

    public Email(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmed = value.Trim();
        if (!IsValidEmail(trimmed))
        {
            throw new ArgumentException("Invalid email format.", nameof(value));
        }

        Value = trimmed.ToLowerInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var mailAddress = new MailAddress(email);
            return mailAddress.Address == email.Trim();
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public override string ToString() => Value;
}
