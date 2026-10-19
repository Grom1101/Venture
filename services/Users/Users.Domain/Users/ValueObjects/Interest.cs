using Venture.Domain.Abstractions.ValueObjects;
using Venture.Domain.Common.Results;

namespace Venture.Domain.Users.ValueObjects;

public sealed class Interest : ValueObject
{
    public const int MaxLength = 50;

    public string Value { get; }

    private Interest(string value)
    {
        Value = value;
    }

    public static Result<Interest> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<Interest>(Error.Conflict("Users.Interest.Empty", "интерес не должен быть пустым"));

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            return Result.Failure<Interest>(Error.Conflict(
                "Users.Interest.TooLong",
                $"Длина интереса должна быть меньше {MaxLength} символов"));

        return Result.Success(new Interest(trimmed));
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }
}
