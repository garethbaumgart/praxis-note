using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PraxisNote.Domain.Aggregates.ArchetypeSnapshots;

namespace PraxisNote.Infrastructure.Persistence.Configurations;

public sealed class ArchetypeSnapshotConfiguration : IEntityTypeConfiguration<ArchetypeSnapshot>
{
    public void Configure(EntityTypeBuilder<ArchetypeSnapshot> builder)
    {
        builder.ToTable("ArchetypeSnapshots");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId)
            .IsRequired();

        builder.Property(a => a.ProfileId)
            .IsRequired();

        builder.Property(a => a.WeekStartDate)
            .IsRequired();

        builder.Property(a => a.PrimaryArchetype)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Score)
            .IsRequired();

        builder.Property(a => a.MeetingCount)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.HasIndex(a => new { a.UserId, a.ProfileId, a.WeekStartDate })
            .HasDatabaseName("IX_ArchetypeSnapshots_UserId_ProfileId_WeekStartDate");
    }
}
