using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PraxisNote.Domain.Aggregates.ApiKeys;

namespace PraxisNote.Infrastructure.Persistence.Configurations;

public sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.HasKey(k => k.Id);
        builder.Property(k => k.UserId).IsRequired();
        builder.Property(k => k.ProfileId).IsRequired();
        builder.Property(k => k.Name).HasMaxLength(100).IsRequired();
        builder.Property(k => k.KeyHash).HasMaxLength(64).IsRequired();
        builder.Property(k => k.KeyPrefix).HasMaxLength(11).IsRequired();
        builder.Property(k => k.CreatedAt).IsRequired();
        builder.Property(k => k.IsRevoked).IsRequired().HasDefaultValue(false);

        builder.HasIndex(k => k.KeyHash).IsUnique();
        builder.HasIndex(k => k.UserId);

        builder.Ignore(k => k.DomainEvents);
        builder.Ignore(k => k.IsExpired);
        builder.Ignore(k => k.IsValid);
    }
}
