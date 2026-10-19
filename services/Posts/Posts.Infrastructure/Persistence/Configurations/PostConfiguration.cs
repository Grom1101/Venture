using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Venture.Domain.Posts;
using Venture.Domain.Posts.ValueObjects;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Infrastructure.Persistence.Configurations;

internal sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("Posts");

        builder.HasKey(post => post.Id);

        builder.Property(post => post.Id)
            .HasConversion(id => id.Value, value => PostId.From(value).Value)
            .ValueGeneratedNever();

        builder.Property(post => post.AuthorId)
            .HasConversion(id => id.Value, value => UserId.From(value).Value)
            .IsRequired();

        builder.Property(post => post.Content)
            .HasConversion(content => content.Value, value => PostContent.Create(value).Value)
            .HasMaxLength(PostContent.MaxLength)
            .IsRequired();

        builder.Property(post => post.CreatedAtUtc).IsRequired();
        builder.Property(post => post.EditedAtUtc);
        builder.Property(post => post.IsDeleted).IsRequired();

        builder.HasIndex(post => new { post.AuthorId, post.CreatedAtUtc });

        builder.Ignore(post => post.DomainEvents);

        builder.OwnsMany(post => post.Media, media =>
        {
            media.ToTable("PostMedia");
            media.WithOwner().HasForeignKey("PostId");

            media.HasKey(postMedia => postMedia.Id);

            media.Property(postMedia => postMedia.Id)
                .HasConversion(id => id.Value, value => PostMediaId.From(value).Value)
                .ValueGeneratedNever();

            media.Property(postMedia => postMedia.Url)
                .HasConversion(url => url.Value, value => MediaUrl.Create(value).Value)
                .HasColumnName("Url")
                .HasMaxLength(MediaUrl.MaxLength)
                .IsRequired();

            media.Property(postMedia => postMedia.Order).IsRequired();
        });
    }
}
