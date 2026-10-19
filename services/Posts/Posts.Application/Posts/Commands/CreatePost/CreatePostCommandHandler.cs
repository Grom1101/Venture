using Venture.Application.Abstractions.Authentication;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Application.Abstractions.Time;
using Venture.Domain.Common.Results;
using Venture.Application.Users;
using Venture.Domain.Posts;
using Venture.Domain.Posts.ValueObjects;

namespace Venture.Application.Posts.Commands.CreatePost;

internal sealed class CreatePostCommandHandler(
    IPostRepository postRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreatePostCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreatePostCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } authorId)
            return Result.Failure<Guid>(UserErrors.NotAuthenticated);

        if (!await userRepository.ExistsAsync(authorId, cancellationToken))
            return Result.Failure<Guid>(UserErrors.NotFound(authorId.Value));

        var content = PostContent.Create(command.Content);
        if (content.IsFailure)
            return Result.Failure<Guid>(content.Error);

        var post = Post.Publish(
            authorId,
            content.Value,
            dateTimeProvider.UtcNow);

        if (post.IsFailure)
            return Result.Failure<Guid>(post.Error);

        postRepository.Add(post.Value);

        return Result.Success(post.Value.Id.Value);
    }
}
