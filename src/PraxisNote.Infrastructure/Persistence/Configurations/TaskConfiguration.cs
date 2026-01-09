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

        // LabelIds stored as JSON array - use backing field which is HashSet<Guid>
        var labelIdsComparer = new ValueComparer<HashSet<Guid>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToHashSet());

        builder.Property<HashSet<Guid>>("_labelIds")
            .HasColumnName("LabelIds")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<HashSet<Guid>>(v, JsonSerializerOptions.Default) ?? new HashSet<Guid>())
            .Metadata.SetValueComparer(labelIdsComparer);

        builder.Ignore(t => t.LabelIds);

        // Comments stored as JSONB array - use backing field which is List<Comment>
        var commentsComparer = new ValueComparer<List<Comment>>(
            (c1, c2) => JsonSerializer.Serialize(c1, JsonSerializerOptions.Default) == JsonSerializer.Serialize(c2, JsonSerializerOptions.Default),
            c => JsonSerializer.Serialize(c, JsonSerializerOptions.Default).GetHashCode(),
            c => JsonSerializer.Deserialize<List<Comment>>(JsonSerializer.Serialize(c, JsonSerializerOptions.Default), JsonSerializerOptions.Default)!);

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
