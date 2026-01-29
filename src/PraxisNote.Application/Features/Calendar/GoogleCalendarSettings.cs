namespace PraxisNote.Application.Features.Calendar;

public class GoogleCalendarSettings
{
    public const string SectionName = "GoogleCalendar";

    public int DefaultSyncDaysAhead { get; set; } = 7;
}
