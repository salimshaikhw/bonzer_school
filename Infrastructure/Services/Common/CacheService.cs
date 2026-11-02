using Application.Abstractions;
using Application.Abstractions.Tenancy;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Common
{
    /// <summary>
    /// The memory cache is shared by all tenants, so every key is prefixed with the current tenant:
    /// "Exam:{id}" cached for one school group is never returned to another.
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ITenantContext _tenantContext;

        public CacheService(IMemoryCache memoryCache, ITenantContext tenantContext)
        {
            _memoryCache = memoryCache;
            _tenantContext = tenantContext;
        }

        private string Scoped(string key) => _tenantContext.CacheKeyPrefix + key;

        public bool Exists(string key) => _memoryCache.TryGetValue(Scoped(key), out _);

        public T Get<T>(string key) => _memoryCache.Get<T>(Scoped(key));

        /// <summary>Entries expire after 30 minutes, so memory is bounded and data changed elsewhere is picked up.</summary>
        public void Set<T>(string key, T value) => _memoryCache.Set(Scoped(key), value, DefaultLifetime);

        private static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(30);

        public void Remove(string key) => _memoryCache.Remove(Scoped(key));
    }

}
