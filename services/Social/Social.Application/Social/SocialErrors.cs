using Venture.Domain.Common.Results;

namespace Venture.Application.Social;

public static class SocialErrors
{
    public static readonly Error CannotFollowYourself =
        Error.Validation("Social.CannotFollowYourself", "Нельзя подписаться на самого себя");

    public static readonly Error AlreadyFollowing =
        Error.Conflict("Social.AlreadyFollowing", "Подписка уже существует");

    public static readonly Error NotFollowing =
        Error.Conflict("Social.NotFollowing", "Подписка не найдена");
}
