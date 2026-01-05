using Converge.Configuration.Application.Exceptions;
using Converge.Configuration.DTOs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Converge.Configuration.Services
{
    public class InMemoryConfigService : IConfigService
    {
        // Immutable internal storage model
        private record Entry(
            string Key,
            string Value,
            ConfigurationScope Scope,
            Guid? TenantId,
            int Version,
            bool Active
        );

        // Key = config key, Value = all versions of that key
        private readonly ConcurrentDictionary<string, List<Entry>> _store = new();

        // -------------------------------
        // READ (Query)
        // -------------------------------
        public Task<ConfigResponse?> GetEffectiveAsync(
            string key,
            Guid? tenantId,
            int? version,
            string correlationId)
        {
            if (!_store.TryGetValue(key, out var versions) || versions.Count == 0)
                return Task.FromResult<ConfigResponse?>(null);

            Entry? selected;

            if (version.HasValue)
            {
                // Explicit version fetch (tenant-specific if tenantId provided)
                selected = versions.FirstOrDefault(v =>
                    v.Version == version.Value &&
                    v.TenantId == tenantId);
            }
            else
            {
                // 1️⃣ Tenant override (if tenantId provided)
                selected = tenantId != null
                    ? versions
                        .Where(v =>
                            v.Active &&
                            v.Scope == ConfigurationScope.Tenant &&
                            v.TenantId == tenantId)
                        .OrderByDescending(v => v.Version)
                        .FirstOrDefault()
                    : null;

                // 2️⃣ Global fallback
                selected ??= versions
                    .Where(v =>
                        v.Active &&
                        v.Scope == ConfigurationScope.Global)
                    .OrderByDescending(v => v.Version)
                    .FirstOrDefault();
            }

            if (selected == null)
                return Task.FromResult<ConfigResponse?>(null);

            return Task.FromResult<ConfigResponse?>(new ConfigResponse
            {
                Key = selected.Key,
                Value = selected.Value,
                Scope = selected.Scope,
                TenantId = selected.TenantId,
                Version = selected.Version
            });
        }

        // -------------------------------
        // CREATE (Command)
        // -------------------------------
        public Task<ConfigResponse> CreateAsync(
            CreateConfigRequest request,
            string correlationId)
        {
            // Domain validation
            if (string.IsNullOrWhiteSpace(request.Key))
                throw new ArgumentException("Key is required", nameof(request.Key));

            if (string.IsNullOrWhiteSpace(request.Value))
                throw new ArgumentException("Value is required", nameof(request.Value));

            if (request.Scope == ConfigurationScope.Tenant && request.TenantId == null)
                throw new InvalidOperationException(
                    "TenantId is required for TENANT scoped config");

            if (request.Scope == ConfigurationScope.Global && request.TenantId != null)
                throw new InvalidOperationException(
                    "TenantId must be null for GLOBAL scoped config");

            var list = _store.GetOrAdd(request.Key, _ => new List<Entry>());

            lock (list)
            {
                // Enforce single active config per scope+tenant
                var existingActive = list.FirstOrDefault(e =>
                    e.Active &&
                    e.Scope == request.Scope &&
                    e.TenantId == request.TenantId);

                if (existingActive != null)
                    throw new ConfigurationAlreadyExistsException(request.Key);

                var newVersion = list.Count == 0
                    ? 1
                    : list.Max(e => e.Version) + 1;

                var entry = new Entry(
                    request.Key,
                    request.Value,
                    request.Scope,
                    request.TenantId,
                    newVersion,
                    true);

                list.Add(entry);

                return Task.FromResult(new ConfigResponse
                {
                    Key = entry.Key,
                    Value = entry.Value,
                    Scope = entry.Scope,
                    TenantId = entry.TenantId,
                    Version = entry.Version
                });
            }
        }

        // -------------------------------
        // UPDATE (Command)
        // -------------------------------
        public Task<ConfigResponse?> UpdateAsync(
            string key,
            UpdateConfigRequest request,
            string correlationId)
        {
            if (string.IsNullOrWhiteSpace(request.Value))
                throw new ArgumentException("Value is required", nameof(request.Value));

            if (!_store.TryGetValue(key, out var list) || list.Count == 0)
                return Task.FromResult<ConfigResponse?>(null);

            lock (list)
            {
                // Find current active config for this key (most recent active entry)
                var active = list
                    .Where(e => e.Active)
                    .OrderByDescending(e => e.Version)
                    .FirstOrDefault();

                if (active == null)
                    return Task.FromResult<ConfigResponse?>(null);

                // Optimistic concurrency
                if (request.ExpectedVersion.HasValue &&
                    request.ExpectedVersion.Value != active.Version)
                {
                    throw new VersionConflictException(
                        key,
                        request.ExpectedVersion.Value,
                        active.Version);
                }

                // Deprecate active version
                list.RemoveAll(e => e.Version == active.Version && e.Scope == active.Scope && e.TenantId == active.TenantId);
                list.Add(active with { Active = false });

                var newVersion = list.Max(e => e.Version) + 1;

                var newEntry = new Entry(
                    key,
                    request.Value,
                    active.Scope,
                    active.TenantId,
                    newVersion,
                    true);

                list.Add(newEntry);

                return Task.FromResult<ConfigResponse?>(new ConfigResponse
                {
                    Key = newEntry.Key,
                    Value = newEntry.Value,
                    Scope = newEntry.Scope,
                    TenantId = newEntry.TenantId,
                    Version = newEntry.Version
                });
            }
        }

        // -------------------------------
        // ROLLBACK (Command)
        // -------------------------------
        public Task<ConfigResponse?> RollbackAsync(
            string key,
            int version,
            Guid? tenantId,
            string correlationId)
        {
            if (!_store.TryGetValue(key, out var list) || list.Count == 0)
                return Task.FromResult<ConfigResponse?>(null);

            lock (list)
            {
                var target = list.FirstOrDefault(e =>
                    e.Version == version &&
                    e.TenantId == tenantId);

                if (target == null)
                    return Task.FromResult<ConfigResponse?>(null);

                // Deprecate current active version (same scope + tenant)
                var active = list
                    .Where(e =>
                        e.Active &&
                        e.Scope == target.Scope &&
                        e.TenantId == tenantId)
                    .OrderByDescending(e => e.Version)
                    .FirstOrDefault();

                if (active != null)
                {
                    list.RemoveAll(e =>
                        e.Version == active.Version &&
                        e.Scope == active.Scope &&
                        e.TenantId == active.TenantId);

                    list.Add(active with { Active = false });
                }

                var newVersion = list.Max(e => e.Version) + 1;

                var newEntry = new Entry(
                    key,
                    target.Value,
                    target.Scope,
                    target.TenantId,
                    newVersion,
                    true);

                list.Add(newEntry);

                return Task.FromResult<ConfigResponse?>(new ConfigResponse
                {
                    Key = newEntry.Key,
                    Value = newEntry.Value,
                    Scope = newEntry.Scope,
                    TenantId = newEntry.TenantId,
                    Version = newEntry.Version
                });
            }
        }
    }
}
