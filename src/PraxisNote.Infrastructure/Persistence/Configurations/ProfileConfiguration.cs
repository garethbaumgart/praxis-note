using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PraxisNote.Domain.Aggregates.Profiles;

namespace PraxisNote.Infrastructure.Persistence.Configurations;

public sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.ToTable("Profiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Icon)
            .HasMaxLength(50);

        builder.Property(p => p.IsDefault)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired();

        // Index for efficient lookup by user
        builder.HasIndex(p => p.UserId);

        // Unique partial index: at most one default profile per user
        builder.HasIndex(p => p.UserId)
            .IsUnique()
            .HasFilter("\"IsDefault\" = true")
            .HasDatabaseName("IX_Profiles_UserId_IsDefault_Unique");

        builder.Ignore(p => p.DomainEvents);
    }
}
