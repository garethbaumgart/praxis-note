using Microsoft.Extensions.DependencyInjection;
using PraxisNote.Application.Features.ApiKeys;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Notes;
using PraxisNote.Application.Features.Notes.Services;
using PraxisNote.Application.Features.Calendar;
using PraxisNote.Application.Features.Drive;
using PraxisNote.Application.Features.Jira;
using PraxisNote.Application.Features.Notifications;
using PraxisNote.Application.Features.Tags;
using PraxisNote.Application.Features.Tasks;
using PraxisNote.Application.Features.Goals;
using PraxisNote.Application.Features.Insights;
using PraxisNote.Application.Features.ActionItems;
using PraxisNote.Application.Features.AccountLinking;
using PraxisNote.Application.Features.Profiles;
using PraxisNote.Application.Features.Users;

namespace PraxisNote.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // API Key use cases
        services.AddScoped<CreateApiKey>();
        services.AddScoped<RevokeApiKey>();
        services.AddScoped<GetUserApiKeys>();

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

        // Task-Tag use cases
        services.AddScoped<AddTagToTask>();
        services.AddScoped<RemoveTagFromTask>();

        // Tag use cases
        services.AddScoped<GetUserTags>();
        services.AddScoped<GetItemsByTag>();
        services.AddScoped<CreateTag>();
        services.AddScoped<UpdateTag>();
        services.AddScoped<DeleteTag>();
        services.AddScoped<MergeTags>();
        services.AddScoped<PreviewTagMerge>();
        services.AddScoped<AskTagAi>();
        services.AddScoped<GenerateTagStarters>();

        // User use cases
        services.AddScoped<LoginOrRegisterUser>();

        // Account linking use cases
        services.AddScoped<GenerateLinkCode>();
        services.AddScoped<RedeemLinkCode>();
        services.AddScoped<GetLinkedIdentities>();
        services.AddScoped<UnlinkIdentity>();
        services.AddScoped<SetIdentityDefaultProfile>();
        services.AddScoped<UserDataTransferService>();

        // Profile use cases
        services.AddScoped<GetUserProfiles>();
        services.AddScoped<CreateProfile>();
        services.AddScoped<UpdateProfile>();
        services.AddScoped<DeleteProfile>();
        services.AddScoped<SetDefaultProfile>();

        // Note use cases
        services.AddScoped<GetUserNotes>();
        services.AddScoped<GetNoteById>();
        services.AddScoped<CreateNote>();
        services.AddScoped<UpdateNoteContent>();
        services.AddScoped<DeleteNote>();
        services.AddScoped<PromoteCheckboxToTask>();
        services.AddScoped<GetCheckboxStatus>();

        // Note-Tag use cases
        services.AddScoped<AddTagToNote>();
        services.AddScoped<RemoveTagFromNote>();

        // Note services
        services.AddSingleton<ICheckboxExtractor, TiptapCheckboxExtractor>();
        services.AddSingleton<ICheckboxUpdater, TiptapCheckboxUpdater>();
        services.AddScoped<ICheckboxSyncService, CheckboxSyncService>();

        // Meeting use cases
        services.AddScoped<CreateMeeting>();
        services.AddScoped<GetUserMeetings>();
        services.AddScoped<GetMeetingById>();
        services.AddScoped<UpdateMeeting>();
        services.AddScoped<DeleteMeeting>();
        services.AddScoped<SubmitTranscript>();
        services.AddScoped<ClearTranscript>();
        services.AddScoped<AnalyzeMeeting>();
        services.AddScoped<AddTagToMeeting>();
        services.AddScoped<RemoveTagFromMeeting>();
        services.AddScoped<ToggleActionItem>();
        services.AddScoped<PromoteActionItemToTask>();
        services.AddScoped<GetActionItemStatus>();
        services.AddScoped<GenerateReflectionPrompts>();
        services.AddScoped<SubmitReflection>();
        services.AddScoped<GetMeetingReflection>();
        services.AddScoped<ExtractMeetingsFromScreenshot>();
        services.AddScoped<UpdateMeetingExcludeFromInsights>();
        services.AddScoped<CreateMeetingNote>();
        services.AddScoped<UpdateMeetingNote>();
        services.AddScoped<GetMeetingNote>();
        services.AddScoped<ParseTranscriptForImport>();
        services.AddScoped<ConfirmTranscriptImport>();

        // Calendar use cases
        services.AddScoped<ConnectGoogleCalendar>();
        services.AddScoped<DisconnectGoogleCalendar>();
        services.AddScoped<GetCalendarConnectionStatus>();
        services.AddScoped<SyncCalendarEvents>();

        // Drive use cases
        services.AddScoped<ConnectGoogleDrive>();
        services.AddScoped<DisconnectGoogleDrive>();
        services.AddScoped<GetDriveConnectionStatus>();

        // Jira use cases
        services.AddScoped<ConnectJira>();
        services.AddScoped<DisconnectJira>();
        services.AddScoped<GetJiraConnectionStatus>();
        services.AddScoped<ResolveJiraIssue>();

        // Insight use cases
        services.AddScoped<GetBehavioralTrends>();
        services.AddScoped<GetInsightsSummary>();
        services.AddScoped<GetCommunicationProfile>();
        services.AddScoped<GetJohariWindow>();

        // Nudge use cases
        services.AddScoped<GenerateBlindSpotNudges>();
        services.AddScoped<DismissNudge>();
        services.AddScoped<AcceptNudgeAsGoal>();

        // Action item use cases
        services.AddScoped<GetOutstandingActionItems>();

        // Goal use cases
        services.AddScoped<CreateBehavioralGoal>();
        services.AddScoped<UpdateBehavioralGoal>();
        services.AddScoped<DeleteBehavioralGoal>();
        services.AddScoped<GetUserGoals>();
        services.AddScoped<EvaluateGoalProgress>();

        // Notification use cases
        services.AddScoped<GetNotifications>();
        services.AddScoped<GetUnseenNotificationCount>();
        services.AddScoped<MarkNotificationsSeen>();

        return services;
    }
}
