using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PraxisNote.Domain.Aggregates.BehavioralGoals;

namespace PraxisNote.Infrastructure.Persistence.Configurations;

public sealed class BehavioralGoalConfiguration : IEntityTypeConfiguration<BehavioralGoal>
{
    public void Configure(EntityTypeBuilder<BehavioralGoal> builder)
    {
        builder.ToTable("BehavioralGoals");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.UserId)
            .IsRequired();

        builder.Property(g => g.ProfileId)
            .IsRequired();

        builder.Property(g => g.MetricType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(g => g.Operator)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(g => g.TargetValue)
            .IsRequired();

        builder.Property(g => g.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(g => g.IsActive)
            .IsRequired();

        builder.Property(g => g.CreatedAt)
            .IsRequired();

        builder.Property(g => g.UpdatedAt)
            .IsRequired();

        builder.HasIndex(g => new { g.UserId, g.ProfileId });
    }
}
