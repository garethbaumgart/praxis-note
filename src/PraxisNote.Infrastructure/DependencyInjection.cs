using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Calendar;
using PraxisNote.Application.Features.Calendar.Services;
using PraxisNote.Application.Features.Drive.Services;
using PraxisNote.Application.Features.Jira;
using PraxisNote.Application.Features.Jira.Services;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Application.Features.Tags.Services;
using PraxisNote.Application.Features.Transcription;
using PraxisNote.Application.Features.UserAiKeys.Services;
using PraxisNote.Domain.Aggregates.ApiKeys;
using PraxisNote.Domain.Aggregates.BehavioralGoals;
using PraxisNote.Domain.Aggregates.BlindSpotNudges;
using PraxisNote.Domain.Aggregates.CalendarConnections;
using PraxisNote.Domain.Aggregates.DriveConnections;
using PraxisNote.Domain.Aggregates.DriveFileImports;
using PraxisNote.Domain.Aggregates.JiraConnections;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Notifications;
using PraxisNote.Domain.Aggregates.Profiles;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;
using PraxisNote.Domain.Aggregates.UserAiKeys;
using PraxisNote.Domain.Aggregates.Users;
using PraxisNote.Infrastructure.External;
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

        // Unit of Work
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PraxisNoteDbContext>());

        // Repositories
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();
        services.AddScoped<IMeetingRepository, MeetingRepository>();
        services.AddScoped<ICalendarConnectionRepository, CalendarConnectionRepository>();
        services.AddScoped<IDriveConnectionRepository, DriveConnectionRepository>();
        services.AddScoped<IDriveFileImportRepository, DriveFileImportRepository>();
        services.AddScoped<IJiraConnectionRepository, JiraConnectionRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IBehavioralGoalRepository, BehavioralGoalRepository>();
        services.AddScoped<IBlindSpotNudgeRepository, BlindSpotNudgeRepository>();
        services.AddScoped<ILinkedIdentityRepository, LinkedIdentityRepository>();
        services.AddScoped<IAccountLinkCodeRepository, AccountLinkCodeRepository>();
        services.AddScoped<IUserAiKeyRepository, UserAiKeyRepository>();

        // External services
        services.Configure<GoogleCalendarSettings>(configuration.GetSection(GoogleCalendarSettings.SectionName));
        services.AddScoped<ICalendarService, GoogleCalendarService>();
        services.Configure<MeetingAnalysisSettings>(configuration.GetSection(MeetingAnalysisSettings.SectionName));
        services.Configure<AiProviderSettings>(configuration.GetSection(AiProviderSettings.SectionName));
        services.AddScoped<IAiProviderFactory, AiProviderFactory>();
        services.AddHttpClient();
        // Default registrations resolve via factory using Anthropic config — replaced by per-user resolution in #681
        services.AddScoped<IMeetingAnalyzer>(sp =>
        {
            var factory = sp.GetRequiredService<IAiProviderFactory>();
            var settings = sp.GetRequiredService<IOptions<AiProviderSettings>>().Value;
            return factory.CreateMeetingAnalyzer(
                settings.Anthropic.ApiKey ?? "",
                Domain.Aggregates.UserAiKeys.AiProvider.Anthropic,
                settings.Anthropic.DefaultModel);
        });
        services.AddScoped<ITranscriptExtractor, TranscriptExtractor>();
        services.AddScoped<ITagAiChatService>(sp =>
        {
            var factory = sp.GetRequiredService<IAiProviderFactory>();
            var settings = sp.GetRequiredService<IOptions<AiProviderSettings>>().Value;
            return factory.CreateTagAiChatService(
                settings.Anthropic.ApiKey ?? "",
                Domain.Aggregates.UserAiKeys.AiProvider.Anthropic,
                settings.Anthropic.DefaultModel);
        });
        services.Configure<DeepgramSettings>(configuration.GetSection(DeepgramSettings.SectionName));
        services.Configure<JiraSettings>(configuration.GetSection(JiraSettings.SectionName));
        services.AddScoped<IJiraService, JiraService>();
        services.AddScoped<IDriveService, GoogleDriveService>();
        services.AddScoped<IAiKeyEncryptionService, DataProtectionAiKeyEncryptionService>();

        return services;
    }
}
