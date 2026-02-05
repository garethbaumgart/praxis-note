using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.Tags;

namespace PraxisNote.Infrastructure.Persistence.Repositories;

public sealed class TagRepository(PraxisNoteDbContext context) : ITagRepository
{
    public async Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Tags.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<Tag>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Tags
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tag?> GetByNameAsync(Guid userId, string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.ToLowerInvariant();
        return await context.Tags
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Name == normalizedName, cancellationToken);
    }

    public async Task AddAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        await context.Tags.AddAsync(tag, cancellationToken);
    }

    public void Remove(Tag tag)
    {
        context.Tags.Remove(tag);
    }
}
