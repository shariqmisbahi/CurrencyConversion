using Microsoft.Extensions.Caching.Memory;

namespace CurrencyConversion.Services
{
    public interface ICacheService
    {
        Task<T> GetOrCreateAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan expiration);
    }

    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<MemoryCacheService> _logger;

        public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<T> GetOrCreateAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan expiration)
        {
            if (_memoryCache.TryGetValue(cacheKey, out T cachedValue))
            {
                _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
                return cachedValue;
            }

            _logger.LogDebug("Cache miss for {CacheKey}", cacheKey);
            var value = await factory();
            _memoryCache.Set(cacheKey, value, expiration);
            return value;
        }
    }
}