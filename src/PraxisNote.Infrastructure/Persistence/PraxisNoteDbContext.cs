using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.CalendarConnections;
using PraxisNote.Domain.Aggregates.DriveConnections;
using PraxisNote.Domain.Aggregates.JiraConnections;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Notifications;
using PraxisNote.Domain.Aggregates.BehavioralGoals;
using PraxisNote.Domain.Aggregates.BlindSpotNudges;
using PraxisNote.Domain.Aggregates.Profiles;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;
using PraxisNote.Domain.Aggregates.ApiKeys;
using PraxisNote.Domain.Aggregates.Users;

namespace PraxisNote.Infrastructure.Persistence;

public sealed class PraxisNoteDbContext : DbContext, IUnitOfWork, IDataProtectionKeyContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<CalendarConnection> CalendarConnections => Set<CalendarConnection>();
    public DbSet<DriveConnection> DriveConnections => Set<DriveConnection>();
    public DbSet<JiraConnection> JiraConnections => Set<JiraConnection>();
    public DbSet<FeatureNotification> FeatureNotifications => Set<FeatureNotification>();
    public DbSet<BehavioralGoal> BehavioralGoals => Set<BehavioralGoal>();
    public DbSet<BlindSpotNudge> BlindSpotNudges => Set<BlindSpotNudge>();
    public DbSet<LinkedIdentity> LinkedIdentities => Set<LinkedIdentity>();
    public DbSet<AccountLinkCode> AccountLinkCodes => Set<AccountLinkCode>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public PraxisNoteDbContext(DbContextOptions<PraxisNoteDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PraxisNoteDbContext).Assembly);
    }
}
