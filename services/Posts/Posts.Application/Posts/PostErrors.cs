using Venture.Domain.Common.Results;

namespace Venture.Application.Posts;

public static class PostErrors
{
    public static readonly Error AlreadyLiked =
        Error.Conflict("Posts.AlreadyLiked", "Пост уже лайкнут");

    public static readonly Error NotLiked =
        Error.Conflict("Posts.NotLiked", "Лайк не найден");

    public static Error NotFound(Guid postId) =>
        Error.NotFound("Posts.NotFound", $"Пост {postId} не найден");
}
