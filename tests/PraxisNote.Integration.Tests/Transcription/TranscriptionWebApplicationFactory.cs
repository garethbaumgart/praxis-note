using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PraxisNote.Infrastructure.Persistence;

namespace PraxisNote.Integration.Tests.Transcription;

public class TranscriptionWebApplicationFactory : WebApplicationFactory<Program>
{
    public string FakeDeepgramBaseUrl { get; set; } = "";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("Deepgram:ApiKey", "test-api-key");
        builder.UseSetting("Deepgram:BaseUrl", FakeDeepgramBaseUrl);
        builder.UseSetting("Deepgram:KeepAliveIntervalSeconds", "1");

        // Provide a dummy connection string so AddInfrastructure doesn't throw.
        // The actual DbContext is replaced with InMemory below.
        builder.UseSetting("ConnectionStrings:DefaultConnection",
            "Host=localhost;Database=not_used;Username=test;Password=test");

        builder.ConfigureServices(services =>
        {
            // Remove ALL EF Core and DbContext-related registrations to avoid
            // "multiple database providers" error when replacing Npgsql with InMemory.
            var descriptorsToRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<PraxisNoteDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true ||
                d.ImplementationType?.FullName?.Contains("Npgsql") == true).ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add InMemory database to avoid PostgreSQL dependency
            services.AddDbContext<PraxisNoteDbContext>(options =>
                options.UseInMemoryDatabase($"TranscriptionTests_{Guid.NewGuid()}"));
        });
    }
}
