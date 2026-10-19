using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Venture.Application.Abstractions.Persistence.Models;
using Venture.Domain.Posts.ValueObjects;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Infrastructure.Persistence.Configurations;

internal sealed class PostLikeConfiguration : IEntityTypeConfiguration<PostLike>
{
    public void Configure(EntityTypeBuilder<PostLike> builder)
    {
        builder.ToTable("PostLikes");

        builder.HasKey(like => new { like.PostId, like.UserId });

        builder.Property(like => like.PostId)
            .HasConversion(id => id.Value, value => PostId.From(value).Value)
            .IsRequired();

        builder.Property(like => like.UserId)
            .HasConversion(id => id.Value, value => UserId.From(value).Value)
            .IsRequired();

        builder.Property(like => like.CreatedAtUtc).IsRequired();
    }
}
