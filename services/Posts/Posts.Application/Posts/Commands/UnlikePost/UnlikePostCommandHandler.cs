using Venture.Application.Abstractions.Authentication;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Application.Caching;
using Venture.Domain.Common.Results;
using Venture.Application.Users;
using Venture.Domain.Posts.ValueObjects;

namespace Venture.Application.Posts.Commands.UnlikePost;

internal sealed class UnlikePostCommandHandler(
    IPostLikeRepository postLikeRepository,
    ICurrentUser currentUser,
    ICacheInvalidator cacheInvalidator)
    : ICommandHandler<UnlikePostCommand>
{
    public async Task<Result> Handle(UnlikePostCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } currentUserId)
            return Result.Failure(UserErrors.NotAuthenticated);

        var postIdResult = PostId.From(command.PostId);
        if (postIdResult.IsFailure)
            return Result.Failure(postIdResult.Error);

        var postId = postIdResult.Value;

        if (!await postLikeRepository.ExistsAsync(postId, currentUserId, cancellationToken))
            return Result.Failure(PostErrors.NotLiked);

        await postLikeRepository.RemoveAsync(postId, currentUserId, cancellationToken);

        await cacheInvalidator.InvalidateUserFeedAsync(currentUserId.Value, cancellationToken);

        return Result.Success();
    }
}
