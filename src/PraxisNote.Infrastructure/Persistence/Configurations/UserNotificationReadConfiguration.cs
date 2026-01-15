using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PraxisNote.Domain.Aggregates.Notifications;

namespace PraxisNote.Infrastructure.Persistence.Configurations;

public sealed class UserNotificationReadConfiguration : IEntityTypeConfiguration<UserNotificationRead>
{
    public void Configure(EntityTypeBuilder<UserNotificationRead> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId)
            .IsRequired();

        builder.Property(r => r.NotificationId)
            .IsRequired();

        builder.Property(r => r.SeenAt)
            .IsRequired();

        // Composite unique index to prevent duplicates
        builder.HasIndex(r => new { r.UserId, r.NotificationId })
            .IsUnique();

        // Index for querying by user
        builder.HasIndex(r => r.UserId);
    }
}
