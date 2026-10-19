using Venture.Domain.Abstractions.ValueObjects;
using Venture.Domain.Common.Results;

namespace Venture.Domain.Users.ValueObjects;

public sealed class DisplayName : ValueObject
{
    public const int MinLength = 2;
    public const int MaxLength = 50;

    public string Value { get; }

    private DisplayName(string value)
    {
        Value = value;
    }

    public static Result<DisplayName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<DisplayName>(Error.Conflict(
                "Users.DisplayName.Empty",
                "отображаемое имя не может быть пустым"));

        var trimmed = value.Trim();

        if (trimmed.Length < MinLength || trimmed.Length > MaxLength)
            return Result.Failure<DisplayName>(Error.Conflict(
                "Users.DisplayName.Length",
                $"Длина отображаемого имени должна быть в диапазоне от {MinLength} до {MaxLength} символов"));

        if (char.IsDigit(trimmed[0]))
            return Result.Failure<DisplayName>(Error.Conflict(
                "Users.DisplayName.StartsWithDigit",
                "отображаемое имя не может начинаться с числа"));

        return Result.Success(new DisplayName(trimmed));
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
