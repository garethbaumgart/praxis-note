using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PraxisNote.Domain.Aggregates.Profiles;
using PraxisNote.Domain.Aggregates.Users;

namespace PraxisNote.Infrastructure.Persistence.Configurations;

public sealed class LinkedIdentityConfiguration : IEntityTypeConfiguration<LinkedIdentity>
{
    public void Configure(EntityTypeBuilder<LinkedIdentity> builder)
    {
        builder.ToTable("LinkedIdentities");

        builder.HasKey(li => li.Id);

        builder.Property(li => li.UserId)
            .IsRequired();

        builder.Property(li => li.Provider)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(li => li.ProviderId)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(li => li.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(li => li.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(li => li.AvatarUrl)
            .HasMaxLength(2048);

        builder.Property(li => li.DefaultProfileId);

        builder.Property(li => li.LinkedAt)
            .IsRequired();

        // One identity can only be linked to one user
        builder.HasIndex(li => new { li.Provider, li.ProviderId })
            .IsUnique()
            .HasDatabaseName("IX_LinkedIdentities_Provider_ProviderId");

        // Efficient lookup by user
        builder.HasIndex(li => li.UserId)
            .HasDatabaseName("IX_LinkedIdentities_UserId");

        // FK to User
        builder.HasOne<User>()
            .WithMany(u => u.LinkedIdentities)
            .HasForeignKey(li => li.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optional FK to Profile for DefaultProfileId — SET NULL on profile deletion
        builder.HasOne<Profile>()
            .WithMany()
            .HasForeignKey(li => li.DefaultProfileId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
