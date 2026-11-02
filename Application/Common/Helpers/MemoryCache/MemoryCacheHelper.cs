using Application.Abstractions.Tenancy;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace Application.Common.Helpers.MemoryCache
{
    /// <summary>
    /// Shared memory cache with keys prefixed by the current tenant, so cached school data never crosses
    /// tenants. Scoped (it depends on the request's tenant).
    /// </summary>
    public class MemoryCacheHelper : IMemoryCacheHelper
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ITenantContext _tenantContext;
        private static readonly object _lock = new object();
        public MemoryCacheHelper(IMemoryCache memoryCache, ITenantContext tenantContext)
        {
            _memoryCache = memoryCache;
            _tenantContext = tenantContext;
        }

        private string Scoped(string key) => _tenantContext.CacheKeyPrefix + key;

        public T GetCache<T>(string key)
        {
            return _memoryCache.Get<T>(Scoped(key));
        }

        public void RemoveCache(string key)
        {
            _memoryCache.Remove(Scoped(key));
        }

        public void SetCache<T>(string key, T value, TimeSpan expirationTimeSpan)
        {
            lock (_lock)
            {
                _memoryCache.Set<T>(Scoped(key), value, DateTime.Now.Add(expirationTimeSpan));
            }
        }
    }
}
