using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PraxisNote.Domain.Aggregates.DriveFileImports;

namespace PraxisNote.Infrastructure.Persistence.Configurations;

public sealed class DriveFileImportConfiguration : IEntityTypeConfiguration<DriveFileImport>
{
    public void Configure(EntityTypeBuilder<DriveFileImport> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.DriveConnectionId).IsRequired();
        builder.Property(f => f.DriveFileId).HasMaxLength(500).IsRequired();
        builder.Property(f => f.FileName).HasMaxLength(1000).IsRequired();
        builder.Property(f => f.MimeType).HasMaxLength(200).IsRequired();
        builder.Property(f => f.FileModifiedTime);
        builder.Property(f => f.Status).HasConversion<int>().IsRequired();
        builder.Property(f => f.MatchedMeetingId);
        builder.Property(f => f.ParsedContent); // No max length — full document text
        builder.Property(f => f.ParsedAt);
        builder.Property(f => f.ImportedAt);
        builder.Property(f => f.ErrorMessage).HasMaxLength(2000);
        builder.Property(f => f.DiscoveredAt);

        // Unique: one tracking record per file per connection
        builder.HasIndex(f => new { f.DriveConnectionId, f.DriveFileId }).IsUnique();

        // Query index: find pending files by connection
        builder.HasIndex(f => new { f.DriveConnectionId, f.Status });

        builder.Ignore(f => f.DomainEvents);
    }
}
