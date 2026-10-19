using Microsoft.EntityFrameworkCore;
using Venture.Application.Abstractions.Persistence;
using Venture.Application.Abstractions.Persistence.Models;
using Venture.Domain.Posts.ValueObjects;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Infrastructure.Persistence.Repositories;

internal sealed class PostLikeRepository(VentureDbContext dbContext) : IPostLikeRepository
{
    public Task<bool> ExistsAsync(PostId postId, UserId userId, CancellationToken cancellationToken) =>
        dbContext.PostLikes.AnyAsync(
            like => like.PostId == postId && like.UserId == userId,
            cancellationToken);

    public Task AddAsync(PostId postId, UserId userId, DateTime createdAtUtc, CancellationToken cancellationToken)
    {
        dbContext.PostLikes.Add(new PostLike(postId, userId, createdAtUtc));

        return Task.CompletedTask;
    }

    public async Task RemoveAsync(PostId postId, UserId userId, CancellationToken cancellationToken)
    {
        var like = await dbContext.PostLikes.FirstOrDefaultAsync(
            postLike => postLike.PostId == postId && postLike.UserId == userId,
            cancellationToken);

        if (like is not null)
            dbContext.PostLikes.Remove(like);
    }
}
