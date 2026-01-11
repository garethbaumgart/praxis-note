namespace PraxisNote.Application.Features.Tasks;

public class TaskSettings
{
    public const string SectionName = "Tasks";

    public int ArchiveThresholdDays { get; set; } = 2;
    public int MaxArchivedTasks { get; set; } = 50;
}
