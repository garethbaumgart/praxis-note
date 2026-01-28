using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PraxisNote.Domain.Aggregates.Tasks;

using TaskStatus = PraxisNote.Domain.ValueObjects.TaskStatus;

namespace PraxisNote.Infrastructure.Persistence.Configurations;

public sealed class TaskConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.Title)
            .HasMaxLength(500)
            .IsRequired();

        // Store TaskStatus as string for readability
        builder.Property(t => t.Status)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<TaskStatus>(v))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Position);

        builder.Property(t => t.CreatedAt);
        builder.Property(t => t.UpdatedAt);
        builder.Property(t => t.StartedAt);
        builder.Property(t => t.CompletedAt);

        // Optional DueDate value object
        builder.OwnsOne(t => t.DueDate, dd =>
        {
            dd.Property(d => d.Date)
                .HasColumnName("DueDate");
        });

        // Optional CheckboxRef value object (for tasks created from notes)
        builder.OwnsOne(t => t.CheckboxRef, cr =>
        {
            cr.Property(c => c.NoteId)
                .HasColumnName("CheckboxNoteId");
            cr.Property(c => c.CheckboxId)
                .HasColumnName("CheckboxId")
                .HasMaxLength(100);
        });

        // Optional ActionItemRef value object (for tasks created from meeting action items)
        builder.OwnsOne(t => t.ActionItemRef, ar =>
        {
            ar.Property(a => a.MeetingId)
                .HasColumnName("ActionItemMeetingId");
            ar.Property(a => a.ActionItemId)
                .HasColumnName("ActionItemId");
        });

        // TagIds stored as JSON array - use backing field which is HashSet<Guid>
        var tagIdsComparer = new ValueComparer<HashSet<Guid>>(
            (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SetEquals(c2)),
            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c == null ? new HashSet<Guid>() : c.ToHashSet());

        builder.Property<HashSet<Guid>>("_tagIds")
            .HasColumnName("TagIds")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<HashSet<Guid>>(v, JsonSerializerOptions.Default) ?? new HashSet<Guid>())
            .Metadata.SetValueComparer(tagIdsComparer);

        builder.Ignore(t => t.TagIds);

        // Comments stored as JSONB array - use backing field which is List<Comment>
        var commentsComparer = new ValueComparer<List<Comment>>(
            (c1, c2) => (c1 == null && c2 == null) ||
                        (c1 != null && c2 != null && c1.Count == c2.Count &&
                         c1.Zip(c2).All(pair => pair.First.Id == pair.Second.Id &&
                                                pair.First.Content == pair.Second.Content &&
                                                pair.First.UpdatedAt == pair.Second.UpdatedAt)),
            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Id, v.Content, v.UpdatedAt)),
            c => c == null ? new List<Comment>() : c.Select(x => x).ToList());

        builder.Property<List<Comment>>("_comments")
            .HasColumnName("Comments")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<Comment>>(v, JsonSerializerOptions.Default) ?? new List<Comment>())
            .Metadata.SetValueComparer(commentsComparer);

        builder.Ignore(t => t.Comments);

        // Index for querying user's tasks
        builder.HasIndex(t => t.UserId);

        builder.Ignore(t => t.DomainEvents);
    }
}
