using Venture.Domain.Abstractions.ValueObjects;
using Venture.Domain.Common.Results;

namespace Venture.Domain.Posts.ValueObjects;

public sealed class PostMediaId : StronglyTypedId
{
    private PostMediaId(Guid value) : base(value) { }

    public static PostMediaId New() => new(Guid.NewGuid());

    public static Result<PostMediaId> From(Guid value) =>
        Create(value, id => new PostMediaId(id), "Posts.PostMediaId.Empty");
}
