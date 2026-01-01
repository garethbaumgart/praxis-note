using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PraxisNote.Infrastructure.Application.Abstractions;
using PraxisNote.Infrastructure.Application.Users;
using PraxisNote.Infrastructure.Persistence;
using PraxisNote.Infrastructure.Persistence.Repositories;

namespace PraxisNote.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // EF Core with SQLite Database
        services.AddDbContext<PraxisNoteDbContext>(options =>
            options.UseSqlite("Data Source=praxisnote.db"));

        // Unit of Work
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PraxisNoteDbContext>());

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();

        // Use cases
        services.AddScoped<LoginOrRegisterUser>();

        return services;
    }
}
