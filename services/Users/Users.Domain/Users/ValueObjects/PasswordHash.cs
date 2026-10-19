using Venture.Domain.Abstractions.ValueObjects;
using Venture.Domain.Common.Results;

namespace Venture.Domain.Users.ValueObjects;

/// <summary>
/// Домен не умеет хешировать пароли — он лишь гарантирует, что хеш непустой.
/// Алгоритм хеширования живёт в Infrastructure.
/// </summary>
public sealed class PasswordHash : ValueObject
{
    public string Value { get; }

    private PasswordHash(string value)
    {
        Value = value;
    }

    public static Result<PasswordHash> FromHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return Result.Failure<PasswordHash>(Error.Conflict(
                "Users.PasswordHash.Empty",
                "хеш пароля не может быть пустым"));

        return Result.Success(new PasswordHash(hash));
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
