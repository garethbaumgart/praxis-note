using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PraxisNote.Domain.Aggregates.JiraConnections;

namespace PraxisNote.Infrastructure.Persistence.Configurations;

public sealed class JiraConnectionConfiguration : IEntityTypeConfiguration<JiraConnection>
{
    public void Configure(EntityTypeBuilder<JiraConnection> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.Property(c => c.ProfileId)
            .IsRequired();

        builder.Property(c => c.CloudId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.SiteUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.AccessToken)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(c => c.RefreshToken)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(c => c.TokenExpiresAt);
        builder.Property(c => c.ConnectedAt);

        // One connection per user per profile
        builder.HasIndex(c => new { c.UserId, c.ProfileId })
            .IsUnique();

        builder.Ignore(c => c.DomainEvents);
    }
}
