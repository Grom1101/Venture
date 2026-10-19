using Venture.Domain.Common.Results;

namespace Venture.Application.Users;

public static class UserErrors
{
    public static readonly Error NotAuthenticated =
        Error.Unauthorized("Users.NotAuthenticated", "Требуется аутентификация");

    public static readonly Error InvalidCredentials =
        Error.Unauthorized("Users.InvalidCredentials", "Неверная почта или пароль");

    public static readonly Error UsernameAlreadyTaken =
        Error.Conflict("Users.UsernameAlreadyTaken", "Имя пользователя уже занято");

    public static readonly Error EmailAlreadyTaken =
        Error.Conflict("Users.EmailAlreadyTaken", "Почта уже используется");

    public static Error NotFound(Guid userId) =>
        Error.NotFound("Users.NotFound", $"Пользователь {userId} не найден");

    public static Error NotFoundByUsername(string username) =>
        Error.NotFound("Users.NotFound", $"Пользователь {username} не найден");
}
