using Microsoft.EntityFrameworkCore;
using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tasks;
using PraxisNote.Domain.Aggregates.Users;

namespace PraxisNote.Infrastructure.Persistence;

public sealed class PraxisNoteDbContext : DbContext, IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public PraxisNoteDbContext(DbContextOptions<PraxisNoteDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PraxisNoteDbContext).Assembly);
    }
}
