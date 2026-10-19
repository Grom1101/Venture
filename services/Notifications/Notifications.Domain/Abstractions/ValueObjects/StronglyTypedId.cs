using Venture.Domain.Common.Results;

namespace Venture.Domain.Abstractions.ValueObjects;

public abstract class StronglyTypedId : ValueObject
{
    public Guid Value { get; }

    protected StronglyTypedId(Guid value)
    {
        Value = value;
    }

    protected static Result<TId> Create<TId>(Guid value, Func<Guid, TId> factory, string code)
        where TId : StronglyTypedId
    {
        if (value == Guid.Empty)
            return Result.Failure<TId>(Error.Conflict(code, "StronglyTypedId cannot be an empty guid."));

        return Result.Success(factory(value));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}