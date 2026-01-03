using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tasks;
using PraxisNote.Domain.Aggregates.Users;
using PraxisNote.Infrastructure.Application.Users;
using PraxisNote.Infrastructure.Persistence;
using PraxisNote.Infrastructure.Persistence.Repositories;

namespace PraxisNote.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // EF Core with PostgreSQL Database
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=praxisnote;Username=praxisnote;Password=devpassword";
        services.AddDbContext<PraxisNoteDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Unit of Work
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PraxisNoteDbContext>());

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();

        // Use cases
        services.AddScoped<LoginOrRegisterUser>();

        return services;
    }
}
