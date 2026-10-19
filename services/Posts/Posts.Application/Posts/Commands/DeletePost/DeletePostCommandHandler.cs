using Venture.Application.Abstractions.Authentication;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Application.Caching;
using Venture.Domain.Common.Results;
using Venture.Application.Users;
using Venture.Domain.Posts.ValueObjects;

namespace Venture.Application.Posts.Commands.DeletePost;

internal sealed class DeletePostCommandHandler(
    IPostRepository postRepository,
    ICurrentUser currentUser,
    ICacheInvalidator cacheInvalidator)
    : ICommandHandler<DeletePostCommand>
{
    public async Task<Result> Handle(DeletePostCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } currentUserId)
            return Result.Failure(UserErrors.NotAuthenticated);

        var postId = PostId.From(command.PostId);
        if (postId.IsFailure)
            return Result.Failure(postId.Error);

        var post = await postRepository.GetByIdAsync(postId.Value, cancellationToken);

        if (post is null || post.IsDeleted)
            return Result.Failure(PostErrors.NotFound(command.PostId));

        var deleted = post.Delete(currentUserId);
        if (deleted.IsFailure)
            return deleted;

        await cacheInvalidator.InvalidateUserFeedAsync(currentUserId.Value, cancellationToken);

        return Result.Success();
    }
}
