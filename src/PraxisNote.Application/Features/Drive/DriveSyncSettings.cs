namespace PraxisNote.Application.Features.Drive;

public sealed class DriveSyncSettings
{
    public const string SectionName = "DriveSyncSettings";

    /// <summary>Maximum files to process per sync cycle (prevents runaway processing).</summary>
    public int MaxFilesPerSync { get; init; } = 50;

    /// <summary>Delay between file downloads (ms) to avoid Drive API rate limiting.</summary>
    public int PerFileDelayMs { get; init; } = 1000;

    /// <summary>Maximum file size in bytes to download (10MB default).</summary>
    public long MaxFileSizeBytes { get; init; } = 10_000_000;
}
