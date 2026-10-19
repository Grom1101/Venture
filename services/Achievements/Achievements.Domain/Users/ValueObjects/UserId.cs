using Venture.Domain.Abstractions.ValueObjects;
using Venture.Domain.Common.Results;

namespace Venture.Domain.Users.ValueObjects;

public sealed class UserId : StronglyTypedId
{
    private UserId(Guid value) : base(value) { }

    public static UserId New() => new(Guid.NewGuid());

    public static Result<UserId> From(Guid value) =>
        Create(value, id => new UserId(id), "Users.UserId.Empty");
}
