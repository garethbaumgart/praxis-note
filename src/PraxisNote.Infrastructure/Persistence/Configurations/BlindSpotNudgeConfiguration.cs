using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PraxisNote.Domain.Aggregates.BlindSpotNudges;

namespace PraxisNote.Infrastructure.Persistence.Configurations;

public sealed class BlindSpotNudgeConfiguration : IEntityTypeConfiguration<BlindSpotNudge>
{
    public void Configure(EntityTypeBuilder<BlindSpotNudge> builder)
    {
        builder.ToTable("BlindSpotNudges");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.UserId)
            .IsRequired();

        builder.Property(n => n.ProfileId)
            .IsRequired();

        builder.Property(n => n.Dimension)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(n => n.Suggestion)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(n => n.BlindSpotDescription)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        builder.Property(n => n.UpdatedAt)
            .IsRequired();

        builder.HasIndex(n => new { n.UserId, n.ProfileId });
    }
}
