using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Venture.Domain.Achievements;
using Venture.Domain.Achievements.ValueObjects;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Infrastructure.Persistence.Configurations;

internal sealed class UserAchievementProgressConfiguration : IEntityTypeConfiguration<UserAchievementProgress>
{
    public void Configure(EntityTypeBuilder<UserAchievementProgress> builder)
    {
        builder.ToTable("UserAchievementStates");

        builder.HasKey(progress => progress.Id);

        builder.Property(progress => progress.Id)
            .HasColumnName("UserId")
            .HasConversion(id => id.Value, value => UserId.From(value).Value)
            .ValueGeneratedNever();

        builder.Property(progress => progress.PostsObserved).IsRequired();
        builder.Property(progress => progress.FollowersObserved).IsRequired();

        builder.Ignore(progress => progress.DomainEvents);

        builder.OwnsMany(progress => progress.Earned, earned =>
        {
            earned.ToTable("EarnedAchievements");
            earned.WithOwner().HasForeignKey("UserId");

            earned.Property(item => item.AchievementId)
                .HasConversion(id => id.Value, value => AchievementId.From(value).Value)
                .HasColumnName("AchievementId")
                .IsRequired();

            earned.Property(item => item.EarnedOnUtc)
                .HasColumnName("EarnedOnUtc")
                .IsRequired();

            earned.HasKey("UserId", "AchievementId");
        });
    }
}
