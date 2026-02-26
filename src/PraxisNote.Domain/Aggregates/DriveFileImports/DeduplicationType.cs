namespace PraxisNote.Domain.Aggregates.DriveFileImports;

public enum DeduplicationType
{
    None = 0,
    ExactFile = 1,
    CalendarEvent = 2,
    FuzzyMatch = 3
}
