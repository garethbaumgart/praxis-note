using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PraxisNote.Infrastructure.Persistence;

/// <summary>
/// Factory for creating <see cref="PraxisNoteDbContext"/> at design time (EF migrations, scaffolding).
/// Uses the same configuration sources as runtime: user secrets, environment variables, appsettings.json.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PraxisNoteDbContext>
{
    public PraxisNoteDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<PraxisNoteDbContext>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found. " +
                "Configure via: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<your-connection-string>\" " +
                "or set ConnectionStrings__DefaultConnection environment variable.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<PraxisNoteDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new PraxisNoteDbContext(optionsBuilder.Options);
    }
}
