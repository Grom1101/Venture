using System.Net.Mail;
using Venture.Domain.Abstractions.ValueObjects;
using Venture.Domain.Common.Results;

namespace Venture.Domain.Users.ValueObjects;

public sealed class Email : ValueObject
{
    public const int MaxLength = 254;

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<Email>(Error.Conflict("Users.Email.Empty", "почта не должна быть пустой"));

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength)
            return Result.Failure<Email>(Error.Conflict(
                "Users.Email.TooLong",
                $"почта не должна быть длинее{MaxLength} символов"));

        if (!IsValidEmail(normalized))
            return Result.Failure<Email>(Error.Conflict("Users.Email.Invalid", "невалидная почта"));

        return Result.Success(new Email(normalized));
    }

    private static bool IsValidEmail(string value) =>
        MailAddress.TryCreate(value, out var mailAddress) &&
        string.Equals(mailAddress.Address, value, StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}