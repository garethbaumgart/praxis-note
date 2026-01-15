using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PraxisNote.Domain.Aggregates.Notifications;
using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Infrastructure.Persistence.Configurations;

public sealed class FeatureNotificationConfiguration : IEntityTypeConfiguration<FeatureNotification>
{
    public void Configure(EntityTypeBuilder<FeatureNotification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<NotificationType>(v))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(n => n.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(n => n.Summary)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(n => n.IssueUrl)
            .HasMaxLength(500);

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        // Index for efficient querying by date
        builder.HasIndex(n => n.CreatedAt);
    }
}
