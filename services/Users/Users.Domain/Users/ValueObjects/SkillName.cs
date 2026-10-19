using Venture.Domain.Abstractions.ValueObjects;
using Venture.Domain.Common.Results;

namespace Venture.Domain.Users.ValueObjects;

public sealed class SkillName : ValueObject
{
    public const int MaxLength = 50;

    public string Value { get; }

    private SkillName(string value)
    {
        Value = value;
    }

    public static Result<SkillName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<SkillName>(Error.Conflict("Users.SkillName.Empty", "скилл не должен быть пустым"));

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            return Result.Failure<SkillName>(Error.Conflict(
                "Users.SkillName.TooLong",
                $"Длина скилла должна быть меньше {MaxLength} символов"));

        return Result.Success(new SkillName(trimmed));
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }
}
