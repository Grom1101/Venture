using Venture.Application.Abstractions.Authentication;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Notifications;
using Venture.Application.Abstractions.Persistence;
using Venture.Application.Abstractions.Time;
using Venture.Application.Caching;
using Venture.Domain.Common.Results;
using Venture.Application.Users;
using Venture.Domain.Posts.ValueObjects;

namespace Venture.Application.Posts.Commands.LikePost;

internal sealed class LikePostCommandHandler(
    IPostRepository postRepository,
    IPostLikeRepository postLikeRepository,
    INotificationWriter notificationWriter,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider,
    ICacheInvalidator cacheInvalidator)
    : ICommandHandler<LikePostCommand>
{
    public async Task<Result> Handle(LikePostCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } currentUserId)
            return Result.Failure(UserErrors.NotAuthenticated);

        var postIdResult = PostId.From(command.PostId);
        if (postIdResult.IsFailure)
            return Result.Failure(postIdResult.Error);

        var postId = postIdResult.Value;

        var post = await postRepository.GetByIdAsync(postId, cancellationToken);

        if (post is null || post.IsDeleted)
            return Result.Failure(PostErrors.NotFound(command.PostId));

        if (await postLikeRepository.ExistsAsync(postId, currentUserId, cancellationToken))
            return Result.Failure(PostErrors.AlreadyLiked);

        var now = dateTimeProvider.UtcNow;

        await postLikeRepository.AddAsync(postId, currentUserId, now, cancellationToken);

        if (post.AuthorId != currentUserId)
            await notificationWriter.AddAsync(
                post.AuthorId,
                NotificationType.PostLiked,
                currentUserId,
                postId.Value,
                now,
                cancellationToken);

        await cacheInvalidator.InvalidateUserFeedAsync(currentUserId.Value, cancellationToken);

        return Result.Success();
    }
}
