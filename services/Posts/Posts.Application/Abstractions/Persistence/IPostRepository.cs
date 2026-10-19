using Venture.Domain.Posts;
using Venture.Domain.Posts.ValueObjects;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Abstractions.Persistence;

public interface IPostRepository
{
    Task<Post?> GetByIdAsync(PostId id, CancellationToken cancellationToken);

    Task<int> CountByAuthorAsync(UserId authorId, CancellationToken cancellationToken);

    void Add(Post post);
}
