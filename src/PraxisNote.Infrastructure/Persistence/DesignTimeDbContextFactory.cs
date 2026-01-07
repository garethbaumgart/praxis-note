using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PraxisNote.Infrastructure.Persistence;

/// <summary>
/// Factory for creating DbContext at design time (EF migrations, scaffolding).
/// Uses a placeholder connection string - actual connection is configured at runtime.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PraxisNoteDbContext>
{
    public PraxisNoteDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PraxisNoteDbContext>();

        // Placeholder connection string for design-time tooling only.
        // The actual connection is configured via user-secrets/env vars at runtime.
        optionsBuilder.UseNpgsql("Host=localhost;Database=praxisnote_design;Username=postgres;Password=postgres");

        return new PraxisNoteDbContext(optionsBuilder.Options);
    }
}
