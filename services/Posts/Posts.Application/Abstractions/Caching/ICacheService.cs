namespace Venture.Application.Abstractions.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class;

    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
        where T : class;

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Читает целочисленный счётчик. Отсутствующий ключ = 0.
    /// Не использует JSON: значение хранится как raw Redis integer/string.
    /// </summary>
    Task<long> GetCounterAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Атомарно увеличивает счётчик (Redis INCR). Без TTL: сброс в 0 сделал бы старые page-ключи снова «текущими».
    /// </summary>
    Task<long> IncrementAsync(string key, CancellationToken cancellationToken = default);
}
