using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Venture.Domain.Social.Follows;
using Venture.Domain.Social.Follows.ValueObjects;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Infrastructure.Persistence.Configurations;

internal sealed class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.ToTable("Follows");

        builder.HasKey(follow => follow.Id);

        builder.Property(follow => follow.Id)
            .HasConversion(id => id.Value, value => FollowId.From(value).Value)
            .ValueGeneratedNever();

        builder.Property(follow => follow.FollowerId)
            .HasConversion(id => id.Value, value => UserId.From(value).Value)
            .IsRequired();

        builder.Property(follow => follow.FollowingId)
            .HasConversion(id => id.Value, value => UserId.From(value).Value)
            .IsRequired();

        builder.Property(follow => follow.CreatedAtUtc).IsRequired();

        // Уникальность подписки — последняя линия защиты на уровне БД.
        builder.HasIndex(follow => new { follow.FollowerId, follow.FollowingId }).IsUnique();
        builder.HasIndex(follow => follow.FollowingId);

        builder.Ignore(follow => follow.DomainEvents);
    }
}
