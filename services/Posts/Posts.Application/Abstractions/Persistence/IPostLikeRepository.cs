using Venture.Domain.Posts.ValueObjects;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Abstractions.Persistence;

/// <summary>
/// Лайк — это простая связь user↔post, а не агрегат, поэтому у него нет domain-модели:
/// абстракция оперирует идентификаторами, а строка живёт только в Infrastructure.
/// </summary>
public interface IPostLikeRepository
{
    Task<bool> ExistsAsync(PostId postId, UserId userId, CancellationToken cancellationToken);

    Task AddAsync(PostId postId, UserId userId, DateTime createdAtUtc, CancellationToken cancellationToken);

    Task RemoveAsync(PostId postId, UserId userId, CancellationToken cancellationToken);
}
