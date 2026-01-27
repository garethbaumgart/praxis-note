using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Infrastructure.Persistence.Configurations;

public sealed class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.UserId)
            .IsRequired();

        builder.Property(m => m.Title)
            .HasMaxLength(500);

        builder.Property(m => m.MeetingDate);

        builder.Property(m => m.Attendees)
            .HasMaxLength(2000);

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.TranscriptContent)
            .HasColumnType("text");

        builder.Property(m => m.CreatedAt);
        builder.Property(m => m.UpdatedAt);

        // Index for querying user's meetings
        builder.HasIndex(m => m.UserId);

        // Index for querying by date (for daily grouped list)
        builder.HasIndex(m => m.MeetingDate);

        builder.Ignore(m => m.DomainEvents);
    }
}
