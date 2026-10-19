using Venture.Domain.Abstractions.Entities;
using Venture.Domain.Common.Results;
using Venture.Domain.Posts.ValueObjects;

namespace Venture.Domain.Posts.Entities;

public class PostMedia : BaseEntity<PostMediaId>
{
    public MediaUrl Url { get; private set; }

    public int Order { get; private set; }

    private PostMedia()
    {
        Url = null!;
    }

    private PostMedia(PostMediaId id, MediaUrl url, int order)
    {
        Id = id;
        Url = url;
        Order = order;
    }

    internal static Result<PostMedia> Create(MediaUrl url, int order)
    {
        if (url is null)
            return Result.Failure<PostMedia>(Error.Conflict("Posts.MediaUrl.Empty", "ссылка на медиа не может быть пустой"));

        return Result.Success(new PostMedia(PostMediaId.New(), url, order));
    }
}
