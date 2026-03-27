using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PraxisNote.Domain.Aggregates.UserAiKeys;

namespace PraxisNote.Infrastructure.Persistence.Configurations;

public sealed class UserAiKeyConfiguration : IEntityTypeConfiguration<UserAiKey>
{
    public void Configure(EntityTypeBuilder<UserAiKey> builder)
    {
        builder.HasKey(k => k.Id);
        builder.Property(k => k.UserId).IsRequired();
        builder.Property(k => k.Provider)
            .HasConversion(v => v.ToString(), v => Enum.Parse<AiProvider>(v))
            .HasMaxLength(20).IsRequired();
        builder.Property(k => k.EncryptedKey).IsRequired();
        builder.Property(k => k.KeyHint).HasMaxLength(30).IsRequired();
        builder.Property(k => k.PreferredModel).HasMaxLength(100);
        builder.Property(k => k.CreatedAt).IsRequired();
        builder.Property(k => k.UpdatedAt).IsRequired();

        builder.HasIndex(k => new { k.UserId, k.Provider }).IsUnique();
        builder.HasIndex(k => k.UserId);

        builder.Ignore(k => k.DomainEvents);
    }
}
