using Microsoft.Extensions.Caching.Memory;
using System;

namespace Application.Common.Helpers.MemoryCache
{
    public class MemoryCacheHelper : IMemoryCacheHelper
    {
        private readonly IMemoryCache _memoryCache;
        private static readonly object _lock = new object();
        public MemoryCacheHelper(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }
        public T GetCache<T>(string key)
        {
            return _memoryCache.Get<T>(key);
        }

        public void RemoveCache(string key)
        {
            _memoryCache.Remove(key);
        }

        public void SetCache<T>(string key, T value, TimeSpan expirationTimeSpan)
        {
            lock (_lock)
            {
                _memoryCache.Set<T>(key, value, DateTime.Now.Add(expirationTimeSpan));
            }
        }
    }
}
