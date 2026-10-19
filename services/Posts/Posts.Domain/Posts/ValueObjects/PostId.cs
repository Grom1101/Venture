using Venture.Domain.Abstractions.ValueObjects;
using Venture.Domain.Common.Results;

namespace Venture.Domain.Posts.ValueObjects;

public sealed class PostId : StronglyTypedId
{
    private PostId(Guid value) : base(value) { }

    public static PostId New() => new(Guid.NewGuid());

    public static Result<PostId> From(Guid value) =>
        Create(value, id => new PostId(id), "Posts.PostId.Empty");
}
