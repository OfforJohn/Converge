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
// Add using for Domain entity
using DomainEntity = Converge.Configuration.Persistence.Entities.Domain;

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
            if (dtoScope == DtoScope.Tenant) return PersistenceScope.Tenant;
            if (dtoScope == DtoScope.Company) return PersistenceScope.Company;
            return PersistenceScope.Global;
        }

        private DtoScope FromPersistenceScope(PersistenceScope scope)
        {
            if (scope == PersistenceScope.Tenant) return DtoScope.Tenant;
            if (scope == PersistenceScope.Company) return DtoScope.Company;
            return DtoScope.Global;
        }

        public async Task<ConfigResponse?> GetEffectiveAsync(string key, Guid? tenantId, int? version, string correlationId)
        {
            if (version.HasValue)
            {
                var specific = await _db.ConfigurationItems
                    .Where(c => c.Key == key && c.Version == version.Value && c.TenantId == tenantId)
                    .OrderByDescending(c => c.Version)
                    .FirstOrDefaultAsync();

                if (specific == null) return null;

                var scope = FromPersistenceScope(specific.Scope);
                return new ConfigResponse
                {
                    Key = specific.Key,
                    Value = specific.Value,
                    Scope = scope,
                    TenantId = specific.TenantId,
                    CompanyId = scope == DtoScope.Company ? specific.CompanyId : null,
                    Version = specific.Version ?? 0,
                    Domain = scope == DtoScope.Global ? "Global" : null
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
                        CompanyId = null,
                        Version = tenantConfig.Version ?? 0,
                        Domain = null
                    };
                }

                // company override
                var companyConfig = await _db.ConfigurationItems
                    .Where(c => c.Key == key && c.Scope == PersistenceScope.Company && c.TenantId == tenantId && c.Status == "ACTIVE")
                    .OrderByDescending(c => c.Version)
                    .FirstOrDefaultAsync();

                if (companyConfig != null)
                {
                    return new ConfigResponse
                    {
                        Key = companyConfig.Key,
                        Value = companyConfig.Value,
                        Scope = DtoScope.Company,
                        TenantId = companyConfig.TenantId,
                        CompanyId = companyConfig.CompanyId,
                        Version = companyConfig.Version ?? 0,
                        Domain = null
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
                CompanyId = null,
                Version = globalConfig.Version ?? 0,
                Domain = "Global"
            };
        }

        public async Task<ConfigResponse> CreateAsync(CreateConfigRequest request, string correlationId)
        {
            if (string.IsNullOrWhiteSpace(request.Key))
                throw new ArgumentException("Key is required", nameof(request.Key));

            if (string.IsNullOrWhiteSpace(request.Value))
                throw new ArgumentException("Value is required", nameof(request.Value));

            // Remove TenantId requirement for Company scope
            if (request.Scope == DtoScope.Tenant && request.TenantId == null)
                throw new InvalidOperationException("TenantId is required for TENANT scoped config");

            // Remove this check:
            // if (request.Scope == DtoScope.Company && request.TenantId == null)
            //     throw new InvalidOperationException("TenantId is required for COMPANY scoped config");

            if (request.Scope == DtoScope.Global && request.TenantId != null)
                throw new InvalidOperationException("TenantId must be null for GLOBAL scoped config");

            using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                // Save domain to Domain table if provided and not already present
                Guid? domainId = null;
                if (!string.IsNullOrWhiteSpace(request.Domain))
                {
                    var existingDomain = await _db.Set<DomainEntity>().FirstOrDefaultAsync(d => d.Name == request.Domain);
                    if (existingDomain == null)
                    {
                        var newDomain = new DomainEntity { Id = Guid.NewGuid(), Name = request.Domain };
                        _db.Set<DomainEntity>().Add(newDomain);
                        await _db.SaveChangesAsync();
                        domainId = newDomain.Id;
                    }
                    else
                    {
                        domainId = existingDomain.Id;
                    }
                }

                var persistenceScope = ToPersistenceScope(request.Scope);

                var existingActive = await _db.ConfigurationItems.FirstOrDefaultAsync(e => e.Status == "ACTIVE" && e.Scope == persistenceScope && e.TenantId == request.TenantId && e.Key == request.Key);

                if (existingActive != null)
                    throw new Converge.Configuration.Application.Exceptions.ConfigurationAlreadyExistsException(request.Key);

                var maxVersion = await _db.ConfigurationItems.Where(e => e.Key == request.Key).MaxAsync(e => (int?)e.Version) ?? 0;
                var newVersion = maxVersion + 1;

                var generatedCompanyId = (request.Scope == DtoScope.Company) ? Guid.NewGuid() : (Guid?)null;

                // Use ConfigEntityFactory to create all entities with shared properties
                var (item, companyEvent, outboxEvent) = ConfigEntityFactory.CreateAllEntities(
                    key: request.Key,
                    value: request.Value,
                    scope: persistenceScope,
                    tenantId: request.TenantId,
                    companyId: generatedCompanyId,
                    version: newVersion,
                    eventType: "ConfigCreated",
                    correlationId: correlationId,
                    domainId: domainId
                );

                // Always add to ConfigurationItems, regardless of scope
                _db.ConfigurationItems.Add(item);

                // Always add to both event tables for consistency
                _db.CompanyConfigEvents.Add(companyEvent);
                _db.OutboxEvents.Add(outboxEvent);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return new ConfigResponse
                {
                    Key = item.Key,
                    Value = item.Value,
                    Scope = request.Scope,
                    TenantId = item.TenantId,
                    CompanyId = request.Scope == DtoScope.Company ? item.CompanyId : null,
                    Version = item.Version ?? 0,
                    Domain = request.Scope == DtoScope.Global ? "Global" : null
                };
            }
            catch (Exception)
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

                if (request.ExpectedVersion.HasValue && request.ExpectedVersion.Value != (active.Version ?? 0))
                {
                    throw new Converge.Configuration.Application.Exceptions.VersionConflictException(key, request.ExpectedVersion.Value, active.Version ?? 0);
                }

                // Deprecate active
                active.Status = "DEPRECATED";

                var maxVersion = await _db.ConfigurationItems.Where(e => e.Key == key).MaxAsync(e => (int?)e.Version) ?? 0;
                var newVersion = maxVersion + 1;

                var newCompanyId = (active.Scope == PersistenceScope.Company)
                    ? (Guid?)(active.CompanyId ?? Guid.NewGuid())
                    : null;

                var newItem = new ConfigurationItem
                {
                    Key = key,
                    Value = request.Value,
                    Scope = active.Scope,
                    TenantId = active.TenantId,
                    CompanyId = newCompanyId,
                    Version = newVersion,
                    Status = "ACTIVE",
                    CreatedAt = DateTime.UtcNow
                };

                _db.ConfigurationItems.Update(active);
                _db.ConfigurationItems.Add(newItem);

                var updatedScope = FromPersistenceScope(newItem.Scope);

                if (updatedScope == DtoScope.Company)
                {
                    _db.CompanyConfigEvents.Add(new CompanyConfigEvent
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = newItem.CompanyId!.Value,
                        Key = newItem.Key,
                        Value = newItem.Value,
                        EventType = "ConfigUpdated",
                        CorrelationId = correlationId,
                        OccurredAt = DateTime.UtcNow,
                        Dispatched = false,
                        Attempts = 0
                    });
                }
                else
                {
                    _db.OutboxEvents.Add(new OutboxEvent
                    {
                        Id = Guid.NewGuid(),
                        Key = newItem.Key,
                        Value = newItem.Value,
                        Scope = (int)newItem.Scope,
                        TenantId = newItem.TenantId,
                        CompanyId = newItem.CompanyId,
                        Version = newItem.Version ?? 0,
                        EventType = "ConfigUpdated",
                        CorrelationId = correlationId,
                        OccurredAt = DateTime.UtcNow,
                        Dispatched = false,
                        Attempts = 0
                    });
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return new ConfigResponse
                {
                    Key = newItem.Key,
                    Value = newItem.Value,
                    Scope = updatedScope,
                    TenantId = newItem.TenantId,
                    CompanyId = updatedScope == DtoScope.Company ? newItem.CompanyId : null,
                    Version = newItem.Version ?? 0,
                    Domain = updatedScope == DtoScope.Global ? "Global" : null
                };
            }
            catch (Exception)
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
                var target = await _db.ConfigurationItems.FirstOrDefaultAsync(e => e.Key == key && e.Version == version && e.TenantId == (tenantId ?? Guid.Empty));
                if (target == null) return null;

                var active = await _db.ConfigurationItems
                    .Where(e => e.Key == key && e.Status == "ACTIVE" && e.Scope == target.Scope && e.TenantId == (tenantId ?? Guid.Empty))
                    .OrderByDescending(e => e.Version)
                    .FirstOrDefaultAsync();

                if (active != null)
                {
                    active.Status = "DEPRECATED";
                    _db.ConfigurationItems.Update(active);
                }

                var maxVersion = await _db.ConfigurationItems.Where(e => e.Key == key).MaxAsync(e => (int?)e.Version) ?? 0;
                var newVersion = maxVersion + 1;

                var rollbackCompanyId = (target.Scope == PersistenceScope.Company)
                    ? (Guid?)(target.CompanyId ?? Guid.NewGuid())
                    : null;

                var newItem = new ConfigurationItem
                {
                    Key = key,
                    Value = target.Value,
                    Scope = target.Scope,
                    TenantId = target.TenantId,
                    CompanyId = rollbackCompanyId,
                    Version = newVersion,
                    Status = "ACTIVE",
                    CreatedAt = DateTime.UtcNow
                };

                _db.ConfigurationItems.Add(newItem);

                var rollbackScope = FromPersistenceScope(newItem.Scope);

                if (rollbackScope == DtoScope.Company)
                {
                    _db.CompanyConfigEvents.Add(new CompanyConfigEvent
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = newItem.CompanyId!.Value,
                        Key = newItem.Key,
                        Value = newItem.Value,
                        EventType = "ConfigRolledBack",
                        CorrelationId = correlationId,
                        OccurredAt = DateTime.UtcNow,
                        Dispatched = false,
                        Attempts = 0
                    });
                }
                else
                {
                    _db.OutboxEvents.Add(new OutboxEvent
                    {
                        Id = Guid.NewGuid(),
                        Key = newItem.Key,
                        Value = newItem.Value,
                        Scope = (int)newItem.Scope,
                        TenantId = newItem.TenantId,
                        CompanyId = newItem.CompanyId,
                        Version = newItem.Version ?? 0,
                        EventType = "ConfigRolledBack",
                        CorrelationId = correlationId,
                        OccurredAt = DateTime.UtcNow,
                        Dispatched = false,
                        Attempts = 0
                    });
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return new ConfigResponse
                {
                    Key = newItem.Key,
                    Value = newItem.Value,
                    Scope = rollbackScope,
                    TenantId = newItem.TenantId,
                    CompanyId = rollbackScope == DtoScope.Company ? newItem.CompanyId : null,
                    Version = newItem.Version ?? 0,
                    Domain = rollbackScope == DtoScope.Global ? "Global" : null
                };
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                throw;
            }
        }

    }
}
