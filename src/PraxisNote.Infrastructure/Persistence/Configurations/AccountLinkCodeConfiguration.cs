using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PraxisNote.Domain.Aggregates.Users;

namespace PraxisNote.Infrastructure.Persistence.Configurations;

public sealed class AccountLinkCodeConfiguration : IEntityTypeConfiguration<AccountLinkCode>
{
    public void Configure(EntityTypeBuilder<AccountLinkCode> builder)
    {
        builder.ToTable("AccountLinkCodes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.Property(c => c.CodeHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(c => c.ExpiresAt)
            .IsRequired();

        builder.Property(c => c.IsRedeemed)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // Index for quick lookup by user
        builder.HasIndex(c => c.UserId)
            .HasDatabaseName("IX_AccountLinkCodes_UserId");

        // FK to User
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
