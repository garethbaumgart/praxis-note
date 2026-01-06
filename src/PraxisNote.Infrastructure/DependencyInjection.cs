using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tasks;
using PraxisNote.Domain.Aggregates.Users;
using PraxisNote.Infrastructure.Persistence;
using PraxisNote.Infrastructure.Persistence.Repositories;

namespace PraxisNote.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // EF Core with PostgreSQL Database
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string 'DefaultConnection' is not configured. " +
                "Set it via appsettings.json, environment variable, or user secrets.");
        }
        services.AddDbContext<PraxisNoteDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Data Protection - persist keys to database
        // This ensures auth cookies remain valid across app restarts/deployments
        services.AddDataProtection()
            .SetApplicationName("PraxisNote")
            .PersistKeysToDbContext<PraxisNoteDbContext>();

        // Unit of Work
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PraxisNoteDbContext>());

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();

        return services;
    }
}
