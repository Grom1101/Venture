using Venture.Domain.Abstractions.ValueObjects;
using Venture.Domain.Common.Results;

namespace Venture.Domain.Users.ValueObjects;

public sealed class Username : ValueObject
{
    public const int MinLength = 3;
    public const int MaxLength = 30;

    public string Value { get; }

    private Username(string value)
    {
        Value = value;
    }

    public static Result<Username> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<Username>(Error.Conflict("Users.Username.Empty", "имя не может быть пустым"));

        var normalized = new string(
            value.ToLowerInvariant()
            .Where(c => !char.IsWhiteSpace(c))
            .ToArray());

        if (normalized.Length < MinLength || normalized.Length > MaxLength)
            return Result.Failure<Username>(Error.Conflict(
                "Users.Username.Length",
                $"Длина имени должна быть в диапазоне от {MinLength} до {MaxLength} символов"));

        if (char.IsDigit(normalized[0]))
            return Result.Failure<Username>(Error.Conflict(
                "Users.Username.StartsWithDigit",
                "имя не может начинаться с числа"));

        return Result.Success(new Username(normalized));
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
