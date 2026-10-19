namespace Venture.Domain.Users.ValueObjects;

/// <summary>
/// Длина пароля в открытом виде. Хеш хранится в <see cref="PasswordHash"/>.
/// </summary>
public static class Password
{
    public const int MinLength = 8;

    public const int MaxLength = 128;
}
