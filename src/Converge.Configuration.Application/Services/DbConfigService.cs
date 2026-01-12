using System;
using System.Linq;
using System.Threading.Tasks;
using Converge.Configuration.DTOs;
using Converge.Configuration.Persistence;
using Converge.Configuration.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Converge.Configuration.Services;

// Alias ambiguous enums
using PersistenceScope = Converge.Configuration.Persistence.Entities.ConfigurationScope;
using DtoScope = Converge.Configuration.DTOs.ConfigurationScope;

namespace Converge.Configuration.Application.Services
{
    public class DbConfigService : IConfigService
    {
        private readonly ConfigurationDbContext _db;

        public DbConfigService(ConfigurationDbContext db)
        {
            _db = db;
        }

        private PersistenceScope ToPersistenceScope(DtoScope dtoScope)
        {
            return dtoScope == DtoScope.Tenant ? PersistenceScope.Tenant : PersistenceScope.Global;
        }

        private DtoScope FromPersistenceScope(PersistenceScope scope)
        {
            return scope == PersistenceScope.Tenant ? DtoScope.Tenant : DtoScope.Global;
        }

        public async Task<ConfigResponse?> GetEffectiveAsync(string key, Guid? tenantId, int? version, string correlationId)
        {
            if (version.HasValue)
            {
                var specific = await _db.ConfigurationItems
                    .Where(c => c.Key == key && c.Version == version && c.TenantId == tenantId)
                    .OrderByDescending(c => c.Version)
                    .FirstOrDefaultAsync();

                if (specific == null) return null;

                return new ConfigResponse
                {
                    Key = specific.Key,
                    Value = specific.Value,
                    Scope = FromPersistenceScope(specific.Scope),
                    TenantId = specific.TenantId,
                    Version = specific.Version
                };
            }

            // tenant override
            if (tenantId != null)
            {
                var tenantConfig = await _db.ConfigurationItems
                    .Where(c => c.Key == key && c.Scope == PersistenceScope.Tenant && c.TenantId == tenantId && c.Status == "ACTIVE")
                    .OrderByDescending(c => c.Version)
                    .FirstOrDefaultAsync();

                if (tenantConfig != null)
                {
                    return new ConfigResponse
                    {
                        Key = tenantConfig.Key,
                        Value = tenantConfig.Value,
                        Scope = DtoScope.Tenant,
                        TenantId = tenantConfig.TenantId,
                        Version = tenantConfig.Version
                    };
                }
            }

            var globalConfig = await _db.ConfigurationItems
                .Where(c => c.Key == key && c.Scope == PersistenceScope.Global && c.Status == "ACTIVE")
                .OrderByDescending(c => c.Version)
                .FirstOrDefaultAsync();

            if (globalConfig == null) return null;

            return new ConfigResponse
            {
                Key = globalConfig.Key,
                Value = globalConfig.Value,
                Scope = DtoScope.Global,
                TenantId = globalConfig.TenantId,
                Version = globalConfig.Version
            };
        }

        public async Task<ConfigResponse> CreateAsync(CreateConfigRequest request, string correlationId)
        {
            if (string.IsNullOrWhiteSpace(request.Key))
                throw new ArgumentException("Key is required", nameof(request.Key));

            if (string.IsNullOrWhiteSpace(request.Value))
                throw new ArgumentException("Value is required", nameof(request.Value));

            if (request.Scope == DtoScope.Tenant && request.TenantId == null)
                throw new InvalidOperationException("TenantId is required for TENANT scoped config");

            if (request.Scope == DtoScope.Global && request.TenantId != null)
                throw new InvalidOperationException("TenantId must be null for GLOBAL scoped config");

            using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var persistenceScope = ToPersistenceScope(request.Scope);

                var existingActive = await _db.ConfigurationItems.FirstOrDefaultAsync(e => e.Status == "ACTIVE" && e.Scope == persistenceScope && e.TenantId == request.TenantId && e.Key == request.Key);

                if (existingActive != null)
                    throw new Converge.Configuration.Application.Exceptions.ConfigurationAlreadyExistsException(request.Key);

                var maxVersion = await _db.ConfigurationItems.Where(e => e.Key == request.Key).MaxAsync(e => (int?)e.Version) ?? 0;
                var newVersion = maxVersion + 1;

                var item = new ConfigurationItem
                {
                    Key = request.Key,
                    Value = request.Value,
                    Scope = persistenceScope,
                    TenantId = request.TenantId,
                    Version = newVersion,
                    Status = "ACTIVE",
                    CreatedAt = DateTime.UtcNow
                };

                _db.ConfigurationItems.Add(item);

                // Auditing removed - do not write AuditEntry

                // write outbox event
                _db.OutboxEvents.Add(new OutboxEvent
                {
                    EventType = "ConfigCreated",
                    Payload = System.Text.Json.JsonSerializer.Serialize(item),
                    CorrelationId = correlationId,
                    OccurredAt = DateTime.UtcNow,
                    Dispatched = false,
                    Attempts = 0
                });

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return new ConfigResponse
                {
                    Key = item.Key,
                    Value = item.Value,
                    Scope = request.Scope,
                    TenantId = item.TenantId,
                    Version = item.Version
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<ConfigResponse?> UpdateAsync(string key, UpdateConfigRequest request, string correlationId)
        {
            if (string.IsNullOrWhiteSpace(request.Value))
                throw new ArgumentException("Value is required", nameof(request.Value));

            using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var persistenceScope = ToPersistenceScope(request.Scope);

                var active = await _db.ConfigurationItems
                    .Where(e => e.Status == "ACTIVE" && e.Scope == persistenceScope && e.TenantId == request.TenantId)
                    .OrderByDescending(e => e.Version)
                    .FirstOrDefaultAsync();

                if (active == null) return null;

                if (request.ExpectedVersion.HasValue && request.ExpectedVersion.Value != active.Version)
                {
                    throw new Converge.Configuration.Application.Exceptions.VersionConflictException(key, request.ExpectedVersion.Value, active.Version);
                }

                // Deprecate active
                active.Status = "DEPRECATED";

                var maxVersion = await _db.ConfigurationItems.Where(e => e.Key == key).MaxAsync(e => (int?)e.Version) ?? 0;
                var newVersion = maxVersion + 1;

                var newItem = new ConfigurationItem
                {
                    Key = key,
                    Value = request.Value,
                    Scope = active.Scope,
                    TenantId = active.TenantId,
                    Version = newVersion,
                    Status = "ACTIVE",
                    CreatedAt = DateTime.UtcNow
                };

                _db.ConfigurationItems.Update(active);
                _db.ConfigurationItems.Add(newItem);

                // Auditing removed - do not write AuditEntry

                // outbox
                _db.OutboxEvents.Add(new OutboxEvent
                {
                    EventType = "ConfigUpdated",
                    Payload = System.Text.Json.JsonSerializer.Serialize(newItem),
                    CorrelationId = correlationId,
                    OccurredAt = DateTime.UtcNow,
                    Dispatched = false,
                    Attempts = 0
                });

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return new ConfigResponse
                {
                    Key = newItem.Key,
                    Value = newItem.Value,
                    Scope = FromPersistenceScope(newItem.Scope),
                    TenantId = newItem.TenantId,
                    Version = newItem.Version
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<ConfigResponse?> RollbackAsync(string key, int version, Guid? tenantId, string correlationId)
        {
            using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var target = await _db.ConfigurationItems.FirstOrDefaultAsync(e => e.Key == key && e.Version == version && e.TenantId == tenantId);
                if (target == null) return null;

                var active = await _db.ConfigurationItems
                    .Where(e => e.Key == key && e.Status == "ACTIVE" && e.Scope == target.Scope && e.TenantId == tenantId)
                    .OrderByDescending(e => e.Version)
                    .FirstOrDefaultAsync();

                if (active != null)
                {
                    active.Status = "DEPRECATED";
                    _db.ConfigurationItems.Update(active);
                }

                var maxVersion = await _db.ConfigurationItems.Where(e => e.Key == key).MaxAsync(e => (int?)e.Version) ?? 0;
                var newVersion = maxVersion + 1;

                var newItem = new ConfigurationItem
                {
                    Key = key,
                    Value = target.Value,
                    Scope = target.Scope,
                    TenantId = target.TenantId,
                    Version = newVersion,
                    Status = "ACTIVE",
                    CreatedAt = DateTime.UtcNow
                };

                _db.ConfigurationItems.Add(newItem);

                // Auditing removed - do not write AuditEntry

                _db.OutboxEvents.Add(new OutboxEvent
                {
                    EventType = "ConfigRolledBack",
                    Payload = System.Text.Json.JsonSerializer.Serialize(newItem),
                    CorrelationId = correlationId,
                    OccurredAt = DateTime.UtcNow,
                    Dispatched = false,
                    Attempts = 0
                });

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return new ConfigResponse
                {
                    Key = newItem.Key,
                    Value = newItem.Value,
                    Scope = FromPersistenceScope(newItem.Scope),
                    TenantId = newItem.TenantId,
                    Version = newItem.Version
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
