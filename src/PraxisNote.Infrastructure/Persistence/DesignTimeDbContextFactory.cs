using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PraxisNote.Infrastructure.Persistence;

/// <summary>
/// Factory for creating <see cref="PraxisNoteDbContext"/> at design time (EF migrations, scaffolding).
/// Reads connection string from ConnectionStrings__DefaultConnection environment variable.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PraxisNoteDbContext>
{
    public PraxisNoteDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Environment variable 'ConnectionStrings__DefaultConnection' not set. " +
                "Run migrations inside Docker (docker compose exec api ...) or set the variable manually.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<PraxisNoteDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new PraxisNoteDbContext(optionsBuilder.Options);
    }
}
