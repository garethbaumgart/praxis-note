namespace PraxisNote.Domain.Aggregates.DriveFileImports;

public enum DriveFileImportStatus
{
    Pending = 0,
    Parsed = 1,
    Imported = 2,
    Skipped = 3,
    Error = 4
}
