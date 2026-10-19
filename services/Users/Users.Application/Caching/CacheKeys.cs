namespace Venture.Application.Caching;

public static class CacheKeys
{
    public static string UserProfileById(Guid userId) => $"users:profile:id:{userId:D}";

    public static string UserProfileByUsername(string username) =>
        $"users:profile:username:{username.Trim().ToLowerInvariant()}";

    /// <summary>
    /// Числовая версия ленты. Ключ без TTL: истечение вернуло бы version к 0 и «оживило» старые vN.
    /// </summary>
    public static string UserFeedVersion(Guid userId) => $"users:feed:ver:{userId:D}";

    public static string UserFeed(Guid userId, long version, int page, int pageSize) =>
        $"users:feed:{userId:D}:v{version}:p{page}:s{pageSize}";
}
