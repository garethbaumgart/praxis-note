using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Notes;

/// <summary>
/// Gets the link status of all checkboxes in a note (which are linked to tasks and their status).
/// </summary>
public sealed class GetCheckboxStatus(
    INoteRepository noteRepository,
    ITaskRepository taskRepository)
{
    public record Query(Guid NoteId, Guid UserId);

    public record CheckboxStatusDto(
        string CheckboxId,
        bool IsLinked,
        Guid? TaskId,
        string? TaskStatus);

    public async Task<IReadOnlyList<CheckboxStatusDto>?> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var note = await noteRepository.GetByIdAsync(query.NoteId, cancellationToken);
        if (note is null || note.UserId != query.UserId)
            return null;

        // Get all tasks linked to this note
        var userTasks = await taskRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        var linkedTasks = userTasks
            .Where(t => t.CheckboxRef?.NoteId == query.NoteId)
            .ToDictionary(t => t.CheckboxRef!.CheckboxId, t => t);

        // Build status for each checkbox
        var result = new List<CheckboxStatusDto>();
        foreach (var checkbox in note.Checkboxes)
        {
            if (linkedTasks.TryGetValue(checkbox.Id, out var task))
            {
                result.Add(new CheckboxStatusDto(
                    checkbox.Id,
                    IsLinked: true,
                    TaskId: task.Id,
                    TaskStatus: task.Status.ToString()));
            }
            else
            {
                result.Add(new CheckboxStatusDto(
                    checkbox.Id,
                    IsLinked: false,
                    TaskId: null,
                    TaskStatus: null));
            }
        }

        return result;
    }
}
