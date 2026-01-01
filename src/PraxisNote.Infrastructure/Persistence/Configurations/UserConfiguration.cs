using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PraxisNote.Domain.Aggregates.Users;

namespace PraxisNote.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        // Configure ExternalIdentity as complex type (value object)
        builder.ComplexProperty(u => u.ExternalIdentity, ei =>
        {
            ei.Property(e => e.Provider)
                .HasMaxLength(50)
                .IsRequired();

            ei.Property(e => e.ProviderId)
                .HasMaxLength(255)
                .IsRequired();
        });

        // Configure Email as complex type (value object)
        builder.ComplexProperty(u => u.Email, e =>
        {
            e.Property(em => em.Value)
                .HasMaxLength(255)
                .IsRequired();
        });

        builder.Property(u => u.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.AvatarUrl)
            .HasMaxLength(2048);

        builder.Property(u => u.CreatedAt);
        builder.Property(u => u.LastLoginAt);

        builder.Ignore(u => u.DomainEvents);
    }
}
