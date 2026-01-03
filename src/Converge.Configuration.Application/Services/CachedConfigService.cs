
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Converge.Configuration.DTOs;

namespace Converge.Configuration.Services
{
    // Decorator over IConfigService that caches GetEffective results in IDistributedCache (e.g., Redis).
    public class CachedConfigService : IConfigService
    {
        private readonly IConfigService _inner;
        private readonly IDistributedCache _cache;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        private readonly TimeSpan _ttl;

        public CachedConfigService(IConfigService inner, IDistributedCache cache, TimeSpan? ttl = null)
        {
            _inner = inner;
            _cache = cache;
            _ttl = ttl ?? TimeSpan.FromMinutes(5);
        }

        private string MakeCacheKey(string key, Guid? tenantId, int? version)
        {
            var tenantPart = tenantId?.ToString() ?? "global";
            var versionPart = version?.ToString() ?? "latest";
            return $"config:{key}:{tenantPart}:{versionPart}";
        }

        public async Task<ConfigResponse?> GetEffectiveAsync(string key, Guid? tenantId, int? version, string correlationId)
        {
            var cacheKey = MakeCacheKey(key, tenantId, version);
            var cached = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                return JsonSerializer.Deserialize<ConfigResponse>(cached, _jsonOptions);
            }

            var result = await _inner.GetEffectiveAsync(key, tenantId, version, correlationId);
            if (result != null)
            {
                var json = JsonSerializer.Serialize(result, _jsonOptions);
                var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _ttl };
                await _cache.SetStringAsync(cacheKey, json, options);
            }

            return result;
        }

        public Task<ConfigResponse> CreateAsync(CreateConfigRequest request, string correlationId)
        {
            // on create, invalidate relevant cache keys for the key
            // simplest is to remove latest and tenant/global keys
            return InvalidateAndCall(() => _inner.CreateAsync(request, correlationId), request.Key, request.TenantId);
        }

        public Task<ConfigResponse?> UpdateAsync(string key, UpdateConfigRequest request, string correlationId)
        {
            return InvalidateAndCall(() => _inner.UpdateAsync(key, request, correlationId), key, null);
        }

        public Task<ConfigResponse?> RollbackAsync(string key, int version, Guid? tenantId, string correlationId)
        {
            return InvalidateAndCall(() => _inner.RollbackAsync(key, version, tenantId, correlationId), key, tenantId);
        }

        private async Task<T> InvalidateAndCall<T>(Func<Task<T>> op, string key, Guid? tenantId)
        {
            // remove cached variants for latest and tenant/global
            var keys = new[] {
                MakeCacheKey(key, tenantId, null),
                MakeCacheKey(key, null, null),
            };

            foreach (var k in keys)
                await _cache.RemoveAsync(k);

            return await op();
        }
    }
}
