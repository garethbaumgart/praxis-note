using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Infrastructure.Persistence.Configurations;

public sealed class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.UserId)
            .IsRequired();

        builder.Property(m => m.Title)
            .HasMaxLength(500);

        builder.Property(m => m.MeetingDate);

        builder.Property(m => m.Attendees)
            .HasMaxLength(2000);

        builder.Property(m => m.CalendarEventId)
            .HasMaxLength(500);

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.TranscriptContent)
            .HasColumnType("text");

        builder.Property(m => m.Summary)
            .HasColumnType("text");

        builder.Property(m => m.KeyPoints)
            .HasColumnType("text");

        builder.Property(m => m.Decisions)
            .HasColumnType("text");

        builder.Property(m => m.BehavioralAnalysis)
            .HasColumnType("text");

        builder.Property(m => m.ReflectionData)
            .HasColumnType("text");

        builder.Property(m => m.ReflectionSubmittedAt);

        builder.Property(m => m.CreatedAt);
        builder.Property(m => m.UpdatedAt);

        // TagIds stored as JSON array - use backing field which is HashSet<Guid>
        var tagIdsComparer = new ValueComparer<HashSet<Guid>>(
            (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SetEquals(c2)),
            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c == null ? new HashSet<Guid>() : c.ToHashSet());

        builder.Property<HashSet<Guid>>("_tagIds")
            .HasColumnName("TagIds")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => string.IsNullOrWhiteSpace(v)
                    ? new HashSet<Guid>()
                    : JsonSerializer.Deserialize<HashSet<Guid>>(v, JsonSerializerOptions.Default) ?? new HashSet<Guid>())
            .Metadata.SetValueComparer(tagIdsComparer);

        builder.Ignore(m => m.TagIds);

        // ActionItems stored as JSONB array - use backing field which is List<ActionItem>
        var actionItemsComparer = new ValueComparer<List<ActionItem>>(
            (c1, c2) => (c1 == null && c2 == null) ||
                        (c1 != null && c2 != null && c1.Count == c2.Count &&
                         c1.Zip(c2).All(pair => pair.First.Id == pair.Second.Id &&
                                                pair.First.Description == pair.Second.Description &&
                                                pair.First.Assignee == pair.Second.Assignee &&
                                                pair.First.IsCompleted == pair.Second.IsCompleted)),
            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Id, v.Description, v.Assignee, v.IsCompleted)),
            c => c == null ? new List<ActionItem>() : c.ToList());

        builder.Property<List<ActionItem>>("_actionItems")
            .HasColumnName("ActionItems")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => string.IsNullOrWhiteSpace(v)
                    ? new List<ActionItem>()
                    : JsonSerializer.Deserialize<List<ActionItem>>(v, JsonSerializerOptions.Default) ?? new List<ActionItem>())
            .Metadata.SetValueComparer(actionItemsComparer);

        builder.Ignore(m => m.ActionItems);

        // Index for querying user's meetings
        builder.HasIndex(m => m.UserId);

        // Index for querying by date (for daily grouped list)
        builder.HasIndex(m => m.MeetingDate);

        // Unique filtered index for calendar event deduplication
        builder.HasIndex(m => new { m.UserId, m.CalendarEventId })
            .IsUnique()
            .HasFilter("\"CalendarEventId\" IS NOT NULL");

        builder.Ignore(m => m.DomainEvents);
    }
}
