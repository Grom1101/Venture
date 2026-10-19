using Venture.Domain.Posts.ValueObjects;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Abstractions.Persistence.Models;

/// <summary>
/// Лайк — простая persistence-модель, а не агрегат: у него нет инвариантов и поведения,
/// только связь user↔post. Схему для него описывает Infrastructure.
/// </summary>
public sealed class PostLike
{
    public PostId PostId { get; private set; }

    public UserId UserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private PostLike()
    {
        PostId = null!;
        UserId = null!;
    }

    public PostLike(PostId postId, UserId userId, DateTime createdAtUtc)
    {
        PostId = postId;
        UserId = userId;
        CreatedAtUtc = createdAtUtc;
    }
}
