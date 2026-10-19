using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Venture.Application.Abstractions.Caching;

namespace Venture.Infrastructure.Caching;

internal sealed class RedisCacheService(
    IConnectionMultiplexer connectionMultiplexer,
    IOptions<RedisCacheOptions> options,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();
    private readonly string _instanceName = options.Value.InstanceName;

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        var value = await _database.StringGetAsync(BuildKey(key));

        if (value.IsNullOrEmpty)
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>((string)value!, SerializerOptions);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Failed to deserialize cache entry {CacheKey}", key);
            await _database.KeyDeleteAsync(BuildKey(key));

            return null;
        }
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var payload = JsonSerializer.Serialize(value, SerializerOptions);

        return _database.StringSetAsync(BuildKey(key), payload, expiration);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        _database.KeyDeleteAsync(BuildKey(key));

    public async Task<long> GetCounterAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(BuildKey(key));

        if (value.IsNullOrEmpty)
            return 0;

        return (long)value;
    }

    public async Task<long> IncrementAsync(string key, CancellationToken cancellationToken = default) =>
        await _database.StringIncrementAsync(BuildKey(key));

    private string BuildKey(string key) => $"{_instanceName}{key}";
}