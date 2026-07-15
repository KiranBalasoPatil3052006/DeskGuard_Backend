using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Services.Interfaces;

namespace DeskGuardBackend.Services
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _localCache;
        private readonly IDistributedCache? _distributedCache;
        private readonly ILogger<CacheService> _logger;
        private readonly bool _redisAvailable;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        public CacheService(
            IMemoryCache localCache,
            IDistributedCache? distributedCache,
            ILogger<CacheService> logger)
        {
            _localCache = localCache;
            _distributedCache = distributedCache;
            _logger = logger;
            _redisAvailable = distributedCache != null;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            if (_localCache.TryGetValue<T>(key, out var localValue) && localValue != null)
            {
                return localValue;
            }

            if (_redisAvailable && _distributedCache != null)
            {
                try
                {
                    var redisBytes = await _distributedCache.GetAsync(key, cancellationToken);
                    if (redisBytes != null)
                    {
                        var value = JsonSerializer.Deserialize<T>(redisBytes, JsonOptions);
                        if (value != null)
                        {
                            _localCache.Set(key, value, TimeSpan.FromMinutes(5));
                            return value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis GET failed for key {Key}, falling back to DB", key);
                }
            }

            return null;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
        {
            var localExpiry = expiry ?? TimeSpan.FromMinutes(5);
            _localCache.Set(key, value, localExpiry);

            if (_redisAvailable && _distributedCache != null)
            {
                try
                {
                    var redisExpiry = expiry ?? TimeSpan.FromMinutes(10);
                    var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
                    await _distributedCache.SetAsync(key, bytes, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = redisExpiry
                    }, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis SET failed for key {Key}, local cache only", key);
                }
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _localCache.Remove(key);

            if (_redisAvailable && _distributedCache != null)
            {
                try
                {
                    await _distributedCache.RemoveAsync(key, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis REMOVE failed for key {Key}", key);
                }
            }
        }
    }
}
