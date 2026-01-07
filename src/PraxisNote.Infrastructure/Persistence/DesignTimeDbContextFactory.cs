using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PraxisNote.Infrastructure.Persistence;

/// <summary>
/// Factory for creating DbContext at design time (EF migrations, scaffolding).
/// Uses environment variable if set, otherwise falls back to placeholder for local dev.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PraxisNoteDbContext>
{
    public PraxisNoteDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PraxisNoteDbContext>();

        // Use environment variable if set (CI/E2E), otherwise use placeholder for local dev.
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=praxisnote_design;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);

        return new PraxisNoteDbContext(optionsBuilder.Options);
    }
}
