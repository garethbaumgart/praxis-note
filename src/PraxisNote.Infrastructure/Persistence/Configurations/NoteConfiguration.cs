using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Infrastructure.Persistence.Configurations;

public sealed class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.UserId)
            .IsRequired();

        builder.Property(n => n.ProfileId)
            .IsRequired();

        builder.Property(n => n.Content);

        builder.Property(n => n.CreatedAt);
        builder.Property(n => n.UpdatedAt);

        // Checkboxes stored as JSONB array - use backing field which is List<Checkbox>
        var checkboxesComparer = new ValueComparer<List<Checkbox>>(
            (c1, c2) => (c1 == null && c2 == null) ||
                        (c1 != null && c2 != null && c1.Count == c2.Count &&
                         c1.Zip(c2).All(pair => pair.First.Id == pair.Second.Id &&
                                                pair.First.Text == pair.Second.Text &&
                                                pair.First.IsChecked == pair.Second.IsChecked)),
            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Id, v.Text, v.IsChecked)),
            c => c == null ? new List<Checkbox>() : c.Select(x => new Checkbox(x.Id, x.Text, x.IsChecked)).ToList());

        builder.Property<List<Checkbox>>("_checkboxes")
            .HasColumnName("Checkboxes")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => string.IsNullOrWhiteSpace(v)
                    ? new List<Checkbox>()
                    : JsonSerializer.Deserialize<List<Checkbox>>(v, JsonSerializerOptions.Default) ?? new List<Checkbox>())
            .Metadata.SetValueComparer(checkboxesComparer);

        builder.Ignore(n => n.Checkboxes);

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

        builder.Ignore(n => n.TagIds);

        // Index for querying user's notes by profile
        builder.HasIndex(n => new { n.UserId, n.ProfileId });

        builder.Ignore(n => n.DomainEvents);
    }
}
