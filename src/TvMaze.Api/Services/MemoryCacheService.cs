using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TvMaze.Api.Configuration;

namespace TvMaze.Api.Services;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly CacheSettings _cacheSettings;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly HashSet<string> _cacheKeys = new();
    private readonly object _lock = new();

    public MemoryCacheService(
        IMemoryCache memoryCache,
        IOptions<CacheSettings> cacheSettings,
        ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache;
        _cacheSettings = cacheSettings.Value;
        _logger = logger;
    }

    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? absoluteExpiration = null) where T : class
    {
        if (!_cacheSettings.EnableCaching)
        {
            _logger.LogDebug("Caching is disabled, executing factory for key: {Key}", key);
            return await factory();
        }

        if (_memoryCache.TryGetValue(key, out T? cachedValue))
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return cachedValue;
        }

        _logger.LogDebug("Cache miss for key: {Key}", key);

        var value = await factory();

        if (value != null)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(absoluteExpiration ?? TimeSpan.FromMinutes(10))
                .RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
                {
                    _logger.LogDebug("Cache entry evicted - Key: {Key}, Reason: {Reason}", evictedKey, reason);
                    lock (_lock)
                    {
                        _cacheKeys.Remove(evictedKey.ToString() ?? string.Empty);
                    }
                });

            _memoryCache.Set(key, value, cacheEntryOptions);

            lock (_lock)
            {
                _cacheKeys.Add(key);
            }

            _logger.LogDebug("Cached value for key: {Key} with expiration: {Expiration}", key, absoluteExpiration);
        }

        return value;
    }

    public async Task<T> GetOrCreateValueAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null) where T : struct
    {
        if (!_cacheSettings.EnableCaching)
        {
            _logger.LogDebug("Caching is disabled, executing factory for key: {Key}", key);
            return await factory();
        }

        if (_memoryCache.TryGetValue(key, out T cachedValue))
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return cachedValue;
        }

        _logger.LogDebug("Cache miss for key: {Key}", key);

        var value = await factory();

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(absoluteExpiration ?? TimeSpan.FromMinutes(10))
            .RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
            {
                _logger.LogDebug("Cache entry evicted - Key: {Key}, Reason: {Reason}", evictedKey, reason);
                lock (_lock)
                {
                    _cacheKeys.Remove(evictedKey.ToString() ?? string.Empty);
                }
            });

        _memoryCache.Set(key, value, cacheEntryOptions);

        lock (_lock)
        {
            _cacheKeys.Add(key);
        }

        _logger.LogDebug("Cached value for key: {Key} with expiration: {Expiration}", key, absoluteExpiration);

        return value;
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(key);
        lock (_lock)
        {
            _cacheKeys.Remove(key);
        }
        _logger.LogDebug("Removed cache entry for key: {Key}", key);
    }

    public void RemoveByPrefix(string prefix)
    {
        List<string> keysToRemove;
        lock (_lock)
        {
            keysToRemove = _cacheKeys.Where(k => k.StartsWith(prefix)).ToList();
        }

        foreach (var key in keysToRemove)
        {
            Remove(key);
        }

        _logger.LogInformation("Removed {Count} cache entries with prefix: {Prefix}", keysToRemove.Count, prefix);
    }

    public void Clear()
    {
        List<string> keysToRemove;
        lock (_lock)
        {
            keysToRemove = _cacheKeys.ToList();
        }

        foreach (var key in keysToRemove)
        {
            _memoryCache.Remove(key);
        }

        lock (_lock)
        {
            _cacheKeys.Clear();
        }

        _logger.LogInformation("Cleared all cache entries. Total cleared: {Count}", keysToRemove.Count);
    }
}
