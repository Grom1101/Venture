using Venture.Application.Abstractions.Authentication;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Application.Abstractions.Time;
using Venture.Application.Caching;
using Venture.Domain.Common.Results;
using Venture.Application.Users;
using Venture.Domain.Posts.ValueObjects;

namespace Venture.Application.Posts.Commands.EditPost;

internal sealed class EditPostCommandHandler(
    IPostRepository postRepository,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider,
    ICacheInvalidator cacheInvalidator)
    : ICommandHandler<EditPostCommand>
{
    public async Task<Result> Handle(EditPostCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } currentUserId)
            return Result.Failure(UserErrors.NotAuthenticated);

        var postId = PostId.From(command.PostId);
        if (postId.IsFailure)
            return Result.Failure(postId.Error);

        var post = await postRepository.GetByIdAsync(postId.Value, cancellationToken);

        if (post is null || post.IsDeleted)
            return Result.Failure(PostErrors.NotFound(command.PostId));

        var content = PostContent.Create(command.Content);
        if (content.IsFailure)
            return Result.Failure(content.Error);

        var edited = post.Edit(currentUserId, content.Value, dateTimeProvider.UtcNow);
        if (edited.IsFailure)
            return edited;

        await cacheInvalidator.InvalidateUserFeedAsync(currentUserId.Value, cancellationToken);

        return Result.Success();
    }
}
