using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.Notes;

namespace PraxisNote.Infrastructure.Persistence.Repositories;

public sealed class NoteRepository(PraxisNoteDbContext context) : INoteRepository
{
    public async Task<Note?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Notes.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<Note>> GetByUserIdAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default)
    {
        return await context.Notes
            .Where(n => n.UserId == userId && n.ProfileId == profileId)
            .OrderByDescending(n => n.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Note>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Notes
            .Where(n => n.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Note>> GetByTagIdAsync(Guid userId, Guid profileId, Guid tagId, CancellationToken cancellationToken = default)
    {
        // In-memory filtering required because TagIds uses a JSON value conversion
        // that EF Core can't translate Contains() on. Same pattern as GetTagUsageCountsAsync.
        var notes = await context.Notes
            .Where(n => n.UserId == userId && n.ProfileId == profileId)
            .ToListAsync(cancellationToken);

        return notes.Where(n => n.TagIds.Contains(tagId)).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetTagUsageCountsAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default)
    {
        var tagIdLists = await context.Notes
            .Where(n => n.UserId == userId && n.ProfileId == profileId)
            .Select(n => n.TagIds)
            .ToListAsync(cancellationToken);

        return tagIdLists
            .SelectMany(tagIds => tagIds)
            .GroupBy(tagId => tagId)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<bool> ExistsByProfileAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default)
    {
        return await context.Notes
            .AnyAsync(n => n.UserId == userId && n.ProfileId == profileId, cancellationToken);
    }

    public async Task AddAsync(Note note, CancellationToken cancellationToken = default)
    {
        await context.Notes.AddAsync(note, cancellationToken);
    }

    public void Remove(Note note)
    {
        context.Notes.Remove(note);
    }
}
