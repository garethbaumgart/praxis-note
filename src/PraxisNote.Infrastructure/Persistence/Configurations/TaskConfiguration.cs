using Microsoft.EntityFrameworkCore;
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

        // LabelIds stored as JSON array - use backing field which is HashSet<Guid>
        builder.Property<HashSet<Guid>>("_labelIds")
            .HasColumnName("LabelIds")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, System.Text.Json.JsonSerializerOptions.Default),
                v => System.Text.Json.JsonSerializer.Deserialize<HashSet<Guid>>(v, System.Text.Json.JsonSerializerOptions.Default) ?? new HashSet<Guid>());

        builder.Ignore(t => t.LabelIds);

        // Index for querying user's tasks
        builder.HasIndex(t => t.UserId);

        builder.Ignore(t => t.DomainEvents);
    }
}
