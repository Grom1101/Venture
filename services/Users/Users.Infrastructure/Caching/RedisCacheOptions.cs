namespace Venture.Infrastructure.Caching;

public sealed class RedisCacheOptions
{
    public const string SectionName = "Redis";

    /// <summary>
    /// Префикс всех ключей в Redis, чтобы несколько окружений могли делить один инстанс.
    /// </summary>
    public string InstanceName { get; init; } = "Venture:";
}
