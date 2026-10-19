namespace Venture.Application.Caching;

/// <summary>
/// TTL для кэшируемых read-моделей. Короче для ленты: она персональная и чаще меняется.
/// Счётчик версии ленты TTL не имеет — иначе после истечения version снова станет 0.
/// </summary>
public static class CacheDurations
{
    public static readonly TimeSpan UserProfile = TimeSpan.FromMinutes(5);

    public static readonly TimeSpan UserFeed = TimeSpan.FromSeconds(45);
}
