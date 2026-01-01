using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.Users;
using PraxisNote.Infrastructure.Application.Abstractions;

namespace PraxisNote.Infrastructure.Persistence;

public sealed class PraxisNoteDbContext : DbContext, IUnitOfWork
{
    public DbSet<User> Users => Set<User>();

    public PraxisNoteDbContext(DbContextOptions<PraxisNoteDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PraxisNoteDbContext).Assembly);
    }
}
