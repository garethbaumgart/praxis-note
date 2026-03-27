using Microsoft.Extensions.Logging;

namespace PraxisNote.Infrastructure.External;

/// <summary>
/// Shared timezone resolution logic used by all meeting analyzer implementations.
/// </summary>
internal static class TimeZoneHelper
{
    internal record ResolvedTimeZone(TimeZoneInfo TimeZoneInfo, string DisplayName);

    /// <summary>
    /// Resolves an IANA timezone string to a TimeZoneInfo and a display name.
    /// Falls back to local timezone if the input is null/empty or invalid.
    /// The display name always reflects the resolved timezone to avoid contradictory prompt data.
    /// </summary>
    internal static ResolvedTimeZone ResolveTimeZone(string? ianaTimeZone, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(ianaTimeZone))
            return new ResolvedTimeZone(TimeZoneInfo.Local, TimeZoneInfo.Local.Id);

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZone);
            // Use the original IANA ID for the prompt (better AI recognition) when resolution succeeds
            return new ResolvedTimeZone(tz, ianaTimeZone);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            logger.LogWarning("Timezone '{TimeZone}' not found, falling back to local timezone", ianaTimeZone);
            return new ResolvedTimeZone(TimeZoneInfo.Local, TimeZoneInfo.Local.Id);
        }
    }
}
