using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PraxisNote.Infrastructure.Persistence;

/// <summary>
/// Factory for creating <see cref="PraxisNoteDbContext"/> at design time (EF migrations, scaffolding).
/// Used only by EF Core tooling; resolves a connection string from an environment variable or a local
/// placeholder value and is not used by the application at runtime.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PraxisNoteDbContext>
{
    public PraxisNoteDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PraxisNoteDbContext>();

        // Use environment variable if set (CI/E2E), otherwise use placeholder for local dev.
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=designtime_only;Username=designtime;Password=designtime_only";

        optionsBuilder.UseNpgsql(connectionString);

        return new PraxisNoteDbContext(optionsBuilder.Options);
    }
}
