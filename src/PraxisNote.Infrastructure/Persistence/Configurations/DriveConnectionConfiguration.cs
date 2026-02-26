using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PraxisNote.Domain.Aggregates.DriveConnections;

namespace PraxisNote.Infrastructure.Persistence.Configurations;

public sealed class DriveConnectionConfiguration : IEntityTypeConfiguration<DriveConnection>
{
    public void Configure(EntityTypeBuilder<DriveConnection> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.Property(c => c.ProfileId)
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
        builder.Property(c => c.FolderId).HasMaxLength(500);
        builder.Property(c => c.FolderName).HasMaxLength(500);
        builder.Property(c => c.InitialImportCutoffDate);
        builder.Property(c => c.SyncFrequencyMinutes).HasDefaultValue(15);
        builder.Property(c => c.AutoAcceptTags).HasDefaultValue(false);

        // One Drive connection per user per profile
        builder.HasIndex(c => new { c.UserId, c.ProfileId }).IsUnique();

        builder.Ignore(c => c.DomainEvents);
    }
}
