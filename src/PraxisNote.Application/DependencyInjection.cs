using Microsoft.Extensions.DependencyInjection;
using PraxisNote.Application.Features.Notifications;
using PraxisNote.Application.Features.Tags;
using PraxisNote.Application.Features.Tasks;
using PraxisNote.Application.Features.Users;

namespace PraxisNote.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Task use cases
        services.AddScoped<CreateTask>();
        services.AddScoped<GetUserTasks>();
        services.AddScoped<GetArchivedCount>();
        services.AddScoped<UpdateTask>();
        services.AddScoped<ChangeTaskStatus>();
        services.AddScoped<DeleteTask>();
        services.AddScoped<ReorderTasks>();
        services.AddScoped<ToggleTaskPriority>();

        // Comment use cases
        services.AddScoped<AddComment>();
        services.AddScoped<UpdateComment>();
        services.AddScoped<DeleteComment>();

        // Due date use cases
        services.AddScoped<SetDueDate>();
        services.AddScoped<ClearDueDate>();

        // Tag use cases
        services.AddScoped<GetUserTags>();
        services.AddScoped<CreateTag>();
        services.AddScoped<UpdateTag>();
        services.AddScoped<DeleteTag>();

        // Task-Tag use cases
        services.AddScoped<AddTagToTask>();
        services.AddScoped<RemoveTagFromTask>();

        // User use cases
        services.AddScoped<LoginOrRegisterUser>();

        // Notification use cases
        services.AddScoped<GetNotifications>();
        services.AddScoped<GetUnseenNotificationCount>();
        services.AddScoped<MarkNotificationsSeen>();

        return services;
    }
}
