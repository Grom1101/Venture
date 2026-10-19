using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Abstractions.Authentication;

/// <summary>
/// Application знает только про идентификатор текущего пользователя,
/// но ничего не знает про HttpContext.
/// </summary>
public interface ICurrentUser
{
    UserId? UserId { get; }

    bool IsAuthenticated { get; }
}
