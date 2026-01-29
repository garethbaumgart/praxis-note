using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PraxisNote.Domain.Aggregates.CalendarConnections;

namespace PraxisNote.Infrastructure.Persistence.Configurations;

public sealed class CalendarConnectionConfiguration : IEntityTypeConfiguration<CalendarConnection>
{
    public void Configure(EntityTypeBuilder<CalendarConnection> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.Property(c => c.Provider)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.AccessToken)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(c => c.RefreshToken)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(c => c.TokenExpiresAt);
        builder.Property(c => c.ConnectedAt);
        builder.Property(c => c.LastSyncedAt);

        // One connection per user per provider
        builder.HasIndex(c => new { c.UserId, c.Provider })
            .IsUnique();

        builder.Ignore(c => c.DomainEvents);
    }
}
