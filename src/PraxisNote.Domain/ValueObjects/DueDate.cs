using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.ValueObjects;

/// <summary>
/// Represents a task's due date with behavior for checking overdue status.
/// </summary>
/// <remarks>
/// Using DateOnly (C# 10+) instead of DateTime because:
/// - Due dates don't need time precision
/// - Avoids timezone confusion
/// - Clearer intent: this is a calendar date, not a moment in time
/// </remarks>
public sealed record DueDate : ValueObject
{
    public DateOnly Date { get; }

    public DueDate(DateOnly date)
    {
        Date = date;
    }

    /// <summary>
    /// Creates a DueDate from a DateTime, extracting just the date portion.
    /// </summary>
    public static DueDate FromDateTime(DateTime dateTime) =>
        new(DateOnly.FromDateTime(dateTime));

    /// <summary>
    /// Returns true if the due date has passed (UTC-based comparison).
    /// </summary>
    public bool IsOverdue() => Date < DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>
    /// Returns true if the due date is within the specified number of days (UTC-based).
    /// </summary>
    public bool IsDueSoon(int days = 3) =>
        Date <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(days)) && !IsOverdue();

    /// <summary>
    /// Returns the number of days until (positive) or since (negative) the due date (UTC-based).
    /// </summary>
    public int DaysUntilDue() => Date.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;

    /// <summary>
    /// Returns a human-readable string like "Today", "Tomorrow", "Overdue by 2d", etc.
    /// </summary>
    public string ToDisplayString()
    {
        var daysUntil = DaysUntilDue();

        return daysUntil switch
        {
            < 0 => $"Overdue by {-daysUntil}d",
            0 => "Today",
            1 => "Tomorrow",
            <= 7 => $"In {daysUntil}d",
            _ => Date.ToString("MMM d")
        };
    }

    public override string ToString() => Date.ToString("yyyy-MM-dd");
}
