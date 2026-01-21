using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PraxisNote.Domain.Aggregates.Tags;

namespace PraxisNote.Infrastructure.Persistence.Configurations;

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Color)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(t => t.CreatedAt);

        // Unique index on (UserId, Name) - tag names must be unique per user
        builder.HasIndex(t => new { t.UserId, t.Name })
            .IsUnique();

        // Index for querying user's tags
        builder.HasIndex(t => t.UserId);

        builder.Ignore(t => t.DomainEvents);
    }
}
