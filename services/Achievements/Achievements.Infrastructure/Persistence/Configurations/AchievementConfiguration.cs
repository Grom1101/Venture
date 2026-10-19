using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Venture.Domain.Achievements;
using Venture.Domain.Achievements.ValueObjects;

namespace Venture.Infrastructure.Persistence.Configurations;

internal sealed class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
{
    public void Configure(EntityTypeBuilder<Achievement> builder)
    {
        builder.ToTable("Achievements");

        builder.HasKey(achievement => achievement.Id);

        builder.Property(achievement => achievement.Id)
            .HasConversion(id => id.Value, value => AchievementId.From(value).Value)
            .ValueGeneratedNever();

        builder.Property(achievement => achievement.AchievementType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(achievement => achievement.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(achievement => achievement.Name).IsUnique();

        builder.Property(achievement => achievement.Description)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(achievement => achievement.CreatedOnUtc).IsRequired();
        builder.Property(achievement => achievement.LastModifiedOnUtc);

        builder.Ignore(achievement => achievement.DomainEvents);
    }
}
