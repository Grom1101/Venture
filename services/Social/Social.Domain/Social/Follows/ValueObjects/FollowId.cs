using Venture.Domain.Abstractions.ValueObjects;
using Venture.Domain.Common.Results;

namespace Venture.Domain.Social.Follows.ValueObjects;

public sealed class FollowId : StronglyTypedId
{
    private FollowId(Guid value) : base(value) { }

    public static FollowId New() => new(Guid.NewGuid());

    public static Result<FollowId> From(Guid value) =>
        Create(value, id => new FollowId(id), "Social.FollowId.Empty");
}
