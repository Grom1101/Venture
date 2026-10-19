using Microsoft.EntityFrameworkCore;
using Venture.Application.Abstractions.Persistence;
using Venture.Domain.Posts;
using Venture.Domain.Posts.ValueObjects;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Infrastructure.Persistence.Repositories;

internal sealed class PostRepository(VentureDbContext dbContext) : IPostRepository
{
    public Task<Post?> GetByIdAsync(PostId id, CancellationToken cancellationToken) =>
        dbContext.Posts
            .Include(post => post.Media)
            .FirstOrDefaultAsync(post => post.Id == id, cancellationToken);

    public Task<int> CountByAuthorAsync(UserId authorId, CancellationToken cancellationToken) =>
        dbContext.Posts.CountAsync(post => post.AuthorId == authorId && !post.IsDeleted, cancellationToken);

    public void Add(Post post) => dbContext.Posts.Add(post);
}
