using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Converge.Configuration.DTOs;

namespace Converge.Configuration.Services
{
    public class InMemoryConfigService : IConfigService
    {
        private record Entry(string Key, string Value, ConfigurationScope Scope, Guid? TenantId, int Version, bool Active);

        private readonly ConcurrentDictionary<string, List<Entry>> _store = new();

        public Task<ConfigResponse?> GetEffectiveAsync(string key, Guid? tenantId, int? version, string correlationId)
        {
            if (!_store.TryGetValue(key, out var versions) || versions.Count == 0)
                return Task.FromResult<ConfigResponse?>(null);

            Entry? selected = null;

            if (version.HasValue)
            {
                selected = versions.FirstOrDefault(v => v.Version == version.Value && (tenantId == null || v.TenantId == tenantId));
            }
            else
            {
                if (tenantId != null)
                {
                    selected = versions
                        .Where(v => v.Scope == ConfigurationScope.Tenant && v.TenantId == tenantId && v.Active)
                        .OrderByDescending(v => v.Version)
                        .FirstOrDefault();
                }

                if (selected == null)
                {
                    selected = versions
                        .Where(v => v.Scope == ConfigurationScope.Global && v.Active)
                        .OrderByDescending(v => v.Version)
                        .FirstOrDefault();
                }
            }

            if (selected == null)
                return Task.FromResult<ConfigResponse?>(null);

            var resp = new ConfigResponse
            {
                Key = selected.Key,
                Value = selected.Value,
                Scope = selected.Scope,
                TenantId = selected.TenantId,
                Version = selected.Version
            };

            return Task.FromResult<ConfigResponse?>(resp);
        }

        public Task<ConfigResponse> CreateAsync(CreateConfigRequest request, string correlationId)
        {
            if (string.IsNullOrWhiteSpace(request.Key))
                throw new ArgumentException("Key is required", nameof(request.Key));
            if (string.IsNullOrWhiteSpace(request.Value))
                throw new ArgumentException("Value is required", nameof(request.Value));
            if (request.Scope == ConfigurationScope.Tenant && request.TenantId == null)
                throw new InvalidOperationException("TenantId is required for TENANT scoped config");
            if (request.Scope == ConfigurationScope.Global && request.TenantId != null)
                throw new InvalidOperationException("TenantId must be null for GLOBAL scoped config");

            var list = _store.GetOrAdd(request.Key, _ => new List<Entry>());

            lock (list)
            {
                var existingActive = list
                    .Where(e => e.Scope == request.Scope && e.TenantId == request.TenantId && e.Active)
                    .OrderByDescending(e => e.Version)
                    .FirstOrDefault();

                if (existingActive != null)
                    throw new InvalidOperationException("Configuration already exists; use update instead.");

                var version = list.Count == 0 ? 1 : list.Max(e => e.Version) + 1;

                var entry = new Entry(request.Key, request.Value, request.Scope, request.TenantId, version, true);
                list.Add(entry);

                var resp = new ConfigResponse
                {
                    Key = entry.Key,
                    Value = entry.Value,
                    Scope = entry.Scope,
                    TenantId = entry.TenantId,
                    Version = entry.Version
                };

                return Task.FromResult(resp);
            }
        }

        public Task<ConfigResponse?> UpdateAsync(string key, UpdateConfigRequest request, string correlationId)
        {
            if (!_store.TryGetValue(key, out var list) || list.Count == 0)
                return Task.FromResult<ConfigResponse?>(null);

            lock (list)
            {
                var active = list.Where(e => e.Active).OrderByDescending(e => e.Version).FirstOrDefault();
                if (active == null)
                    return Task.FromResult<ConfigResponse?>(null);

                if (request.ExpectedVersion.HasValue && request.ExpectedVersion.Value != active.Version)
                    throw new InvalidOperationException("Version conflict");

                var deprecated = new Entry(active.Key, active.Value, active.Scope, active.TenantId, active.Version, false);
                list.RemoveAll(e => e.Version == active.Version && e.TenantId == active.TenantId && e.Scope == active.Scope);
                list.Add(deprecated);

                var newVersion = list.Max(e => e.Version) + 1;
                var newEntry = new Entry(key, request.Value, active.Scope, active.TenantId, newVersion, true);
                list.Add(newEntry);

                var resp = new ConfigResponse
                {
                    Key = newEntry.Key,
                    Value = newEntry.Value,
                    Scope = newEntry.Scope,
                    TenantId = newEntry.TenantId,
                    Version = newEntry.Version
                };

                return Task.FromResult<ConfigResponse?>(resp);
            }
        }

        public Task<ConfigResponse?> RollbackAsync(string key, int version, Guid? tenantId, string correlationId)
        {
            if (!_store.TryGetValue(key, out var list) || list.Count == 0)
                return Task.FromResult<ConfigResponse?>(null);

            lock (list)
            {
                var target = list.FirstOrDefault(e => e.Version == version && e.TenantId == tenantId);
                if (target == null)
                    return Task.FromResult<ConfigResponse?>(null);

                var active = list.Where(e => e.Active && e.TenantId == tenantId).OrderByDescending(e => e.Version).FirstOrDefault();
                if (active != null)
                {
                    var deprecated = new Entry(active.Key, active.Value, active.Scope, active.TenantId, active.Version, false);
                    list.RemoveAll(e => e.Version == active.Version && e.TenantId == active.TenantId && e.Scope == active.Scope);
                    list.Add(deprecated);
                }

                var newVersion = list.Max(e => e.Version) + 1;
                var newEntry = new Entry(key, target.Value, target.Scope, target.TenantId, newVersion, true);
                list.Add(newEntry);

                var resp = new ConfigResponse
                {
                    Key = newEntry.Key,
                    Value = newEntry.Value,
                    Scope = newEntry.Scope,
                    TenantId = newEntry.TenantId,
                    Version = newEntry.Version
                };

                return Task.FromResult<ConfigResponse?>(resp);
            }
        }
    }
}
