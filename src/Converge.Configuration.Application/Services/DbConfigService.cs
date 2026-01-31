using System;
using System.Linq;
using System.Threading.Tasks;
using Converge.Configuration.DTOs;
using Converge.Configuration.Persistence;
using Converge.Configuration.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Converge.Configuration.Services;
using ConvergeERP.Shared.Abstractions;
using ConvergeERP.Shared.Domain; // Add correct using for BaseEntity
//
// Alias ambiguous enums
using DtoScope = Converge.Configuration.DTOs.ConfigurationScope;

namespace Converge.Configuration.Application.Services
{
    public class DbConfigService : IConfigService
    {
        private readonly ConfigurationDbContext _db;
        private readonly ICurrentUser? _currentUser;

        public DbConfigService(ConfigurationDbContext db, ICurrentUser? currentUser = null)
        {
            _db = db;
            _currentUser = currentUser;
        }

        private int ToIntScope(DtoScope dtoScope)
        {
            if (dtoScope == DtoScope.Tenant) return 1;
            if (dtoScope == DtoScope.Company) return 2;
            return 0; // Global
        }

        private DtoScope FromIntScope(int scope)
        {
            if (scope == 1) return DtoScope.Tenant;
            if (scope == 2) return DtoScope.Company;
            return DtoScope.Global;
        }

        private async Task<string?> GetDomainNameAsync(Guid? domainId, DtoScope scope)
        {
            // Global scope always returns "Global" as domain
            if (scope == DtoScope.Global)
                return "Global";

            // For other scopes, look up the domain name if domainId exists
            if (domainId.HasValue)
            {
                var domain = await _db.Domains.FindAsync(domainId.Value);
                return domain?.Name;
            }

            return null;
        }

        public async Task<ConfigResponse?> GetEffectiveAsync(string key, Guid? tenantId, string? domain, Guid? companyId, Guid correlationId)
        {
            // Guard against null tenant/company
            if (_currentUser?.TenantId == Guid.Empty && tenantId == null)
                return null;
            if (_currentUser?.CompanyId == Guid.Empty && companyId == null)
                return null;

            // If companyId is explicitly provided, only search for company-scoped config
            if (companyId != null)
            {
                var companyConfig = await _db.OutboxEvents
                    .Where(c => c.Key == key && c.Scope == 2 && c.CompanyId == companyId)
                    .OrderByDescending(c => c.Version)
                    .FirstOrDefaultAsync();

                if (companyConfig == null) return null;
                var domainName = await GetDomainNameAsync(companyConfig.DomainId, DtoScope.Company);
                return new ConfigResponse
                {
                    Key = companyConfig.Key,
                    Value = companyConfig.Value,
                    Scope = DtoScope.Company,
                    TenantId = companyConfig.TenantId,
                    CompanyId = companyConfig.CompanyId,
                    Version = companyConfig.Version ?? 0,
                    Domain = domainName
                };
            }

            // If tenantId is explicitly provided, only search for tenant-scoped config
            if (tenantId != null)
            {
                var tenantConfig = await _db.OutboxEvents
                    .Where(c => c.Key == key && c.Scope == 1 && c.TenantId == tenantId)
                    .OrderByDescending(c => c.Version)
                    .FirstOrDefaultAsync();

                if (tenantConfig == null) return null;
                var domainName = await GetDomainNameAsync(tenantConfig.DomainId, DtoScope.Tenant);
                return new ConfigResponse
                {
                    Key = tenantConfig.Key,
                    Value = tenantConfig.Value,
                    Scope = DtoScope.Tenant,
                    TenantId = tenantConfig.TenantId,
                    CompanyId = null,
                    Version = tenantConfig.Version ?? 0,
                    Domain = domainName
                };
            }

            // Fallback to global config
            var globalConfig = await _db.OutboxEvents
                .Where(c => c.Key == key && c.Scope == 0)
                .OrderByDescending(c => c.Version)
                .FirstOrDefaultAsync();

            if (globalConfig == null) return null;
            var globalDomainName = await GetDomainNameAsync(globalConfig.DomainId, DtoScope.Global);
            return new ConfigResponse
            {
                Key = globalConfig.Key,
                Value = globalConfig.Value,
                Scope = DtoScope.Global,
                TenantId = null,
                CompanyId = null,
                Version = globalConfig.Version ?? 0,
                Domain = globalDomainName
            };
        }

        public async Task<ConfigResponse> CreateAsync(CreateConfigRequest request, Guid correlationId)
        {
            if (string.IsNullOrWhiteSpace(request.Key))
                throw new ArgumentException("Key is required", nameof(request.Key));

            if (string.IsNullOrWhiteSpace(request.Value))
                throw new ArgumentException("Value is required", nameof(request.Value));

            // Validation based on scope:
            // - Global: TenantId must be null, no IDs returned
            // - Tenant: TenantId required
            // - Company: TenantId optional (auto-generated if not provided), CompanyId auto-generated
            if (request.Scope == DtoScope.Global && request.TenantId != null)
                throw new InvalidOperationException("TenantId must be null for GLOBAL scoped config");

            if (request.Scope == DtoScope.Tenant && request.TenantId == null)
                throw new InvalidOperationException("TenantId is required for TENANT scoped config");

            // Company scope: TenantId is optional - will be auto-generated if not provided

            using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                // Save domain to Domain table if provided and not already present
                Guid? domainId = null;
                if (!string.IsNullOrWhiteSpace(request.Domain))
                {
                    var existingDomain = await _db.Domains.FirstOrDefaultAsync(d => d.Name == request.Domain);
                    if (existingDomain == null)
                    {
                        var newDomain = new DomainEntity { Id = Guid.NewGuid(), Name = request.Domain };
                        _db.Domains.Add(newDomain);
                        await _db.SaveChangesAsync();
                        domainId = newDomain.Id;
                    }
                    else
                    {
                        domainId = existingDomain.Id;
                    }
                }

                var intScope = ToIntScope(request.Scope ?? DtoScope.Global);
                var resolvedScope = request.Scope ?? DtoScope.Global;

                OutboxEvent? existingActive = null;

                if (resolvedScope == DtoScope.Global)
                {
                    // Global scope: check only by key
                    existingActive = await _db.OutboxEvents
                        .Where(e => e.Scope == intScope && e.Key == request.Key)
                        .OrderByDescending(e => e.Version)
                        .FirstOrDefaultAsync();
                }
                else if (resolvedScope == DtoScope.Tenant)
                {
                    // Tenant scope: check by key + tenantId
                    existingActive = await _db.OutboxEvents
                        .Where(e => e.Scope == intScope && e.TenantId == request.TenantId && e.Key == request.Key)
                        .OrderByDescending(e => e.Version)
                        .FirstOrDefaultAsync();
                }
                else if (resolvedScope == DtoScope.Company)
                {
                    // Company scope: check by key + companyId (not tenantId, since companyId is what uniquely identifies the override)
                    // CompanyId will be generated, so no existing config with same tenant + company + key should exist
                    // Since CompanyId is always auto-generated as new, we just need to verify no existing with same tenant/company
                    existingActive = null; // Company scope always gets new CompanyId, so can't have duplicate
                }

                if (existingActive != null)
                    throw new Converge.Configuration.Application.Exceptions.ConfigurationAlreadyExistsException(request.Key);


                var maxVersion = await _db.OutboxEvents.Where(e => e.Key == request.Key).MaxAsync(e => (int?)e.Version) ?? 0;
                var newVersion = maxVersion + 1;

                // Set IDs based on scope:
                // - Global: null, null
                // - Tenant: tenantId (from request), null
                // - Company: tenantId (from request or auto-generated), companyId (auto-generated)
                Guid? effectiveTenantId;
                Guid? effectiveCompanyId;

                if (request.Scope == DtoScope.Global)
                {
                    effectiveTenantId = null;
                    effectiveCompanyId = null;
                }
                else if (request.Scope == DtoScope.Tenant)
                {
                    effectiveTenantId = request.TenantId;
                    effectiveCompanyId = null;
                }
                else // Company scope
                {
                    effectiveTenantId = request.TenantId ?? Guid.NewGuid();  // Auto-generate if not provided
                    effectiveCompanyId = Guid.NewGuid();  // Always auto-generate
                }

                // Create OutboxEvent as single source of truth
                var outboxEvent = new OutboxEvent
                {
                    Id = Guid.NewGuid(),
                    Key = request.Key,
                    Value = request.Value,
                    Scope = intScope,
                    TenantId = effectiveTenantId,
                    CompanyId = effectiveCompanyId,
                    Version = newVersion,
                    DomainId = domainId,
                    EventType = "ConfigCreated",
                    CorrelationId = correlationId,
                    OccurredAt = DateTime.UtcNow,
                    Dispatched = false
                };

                _db.OutboxEvents.Add(outboxEvent);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // Get domain name for response
                string? domainName = null;
                if (domainId.HasValue)
                {
                    var domain = await _db.Domains.FindAsync(domainId.Value);
                    domainName = domain?.Name;
                }
                // For Global scope, set domain name to "Global"
                if (request.Scope == DtoScope.Global)
                {
                    domainName = "Global";
                }

                // Return response based on scope:
                // - Global: no tenantId, no companyId
                // - Tenant: tenantId only
                // - Company: both tenantId and companyId
                return new ConfigResponse
                {
                    Key = outboxEvent.Key,
                    Value = outboxEvent.Value,
                    Scope = resolvedScope,
                    TenantId = resolvedScope == DtoScope.Global ? null : outboxEvent.TenantId,
                    CompanyId = resolvedScope == DtoScope.Company ? outboxEvent.CompanyId : null,
                    Version = outboxEvent.Version ?? 0,
                    Domain = domainName
                };
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<ConfigResponse?> UpdateAsync(string key, UpdateConfigRequest request, Guid correlationId)
        {
            if (string.IsNullOrWhiteSpace(request.Value))
                throw new ArgumentException("Value is required", nameof(request.Value));

            using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var resolvedScope = request.Scope ?? DtoScope.Global;
                var intScope = ToIntScope(resolvedScope);

                // Find the existing configuration based on scope:
                // - Global: just by key (no tenant/company filter)
                // - Tenant: by key + tenantId
                // - Company: by key + companyId
                OutboxEvent? existingConfig = null;

                if (resolvedScope == DtoScope.Global)
                {
                    // Global scope: find by key only
                    existingConfig = await _db.OutboxEvents
                        .Where(e => e.Key == key && e.Scope == intScope)
                        .OrderByDescending(e => e.Version)
                        .FirstOrDefaultAsync();
                }
                else if (resolvedScope == DtoScope.Tenant)
                {
                    // Tenant scope: find by key + tenantId
                    if (request.TenantId == null)
                        throw new ArgumentException("TenantId is required to update Tenant scoped config");

                    existingConfig = await _db.OutboxEvents
                        .Where(e => e.Key == key && e.Scope == intScope && e.TenantId == request.TenantId)
                        .OrderByDescending(e => e.Version)
                        .FirstOrDefaultAsync();
                }
                else if (resolvedScope == DtoScope.Company)
                {
                    // Company scope: find by key + companyId
                    if (request.CompanyId == null)
                        throw new ArgumentException("CompanyId is required to update Company scoped config");

                    existingConfig = await _db.OutboxEvents
                        .Where(e => e.Key == key && e.Scope == intScope && e.CompanyId == request.CompanyId)
                        .OrderByDescending(e => e.Version)
                        .FirstOrDefaultAsync();
                }

                if (existingConfig == null) return null;

                // Check version for optimistic concurrency
                if (request.ExpectedVersion.HasValue && request.ExpectedVersion.Value != (existingConfig.Version ?? 0))
                {
                    throw new Converge.Configuration.Application.Exceptions.VersionConflictException(key, request.ExpectedVersion.Value, existingConfig.Version ?? 0);
                }

                // Increment version
                var newVersion = existingConfig.Version + 1;

                // Update OutboxEvent in place
                existingConfig.Value = request.Value;
                existingConfig.Version = newVersion;
                existingConfig.EventType = "ConfigUpdated";
                existingConfig.CorrelationId = correlationId;
                existingConfig.OccurredAt = DateTime.UtcNow;
                existingConfig.Dispatched = false;
                _db.OutboxEvents.Update(existingConfig);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // Get domain name for response
                var updatedScope = FromIntScope(existingConfig.Scope);
                string? domainName = null;
                if (existingConfig.DomainId.HasValue)
                {
                    var domain = await _db.Domains.FindAsync(existingConfig.DomainId.Value);
                    domainName = domain?.Name;
                }
                if (updatedScope == DtoScope.Global)
                {
                    domainName = "Global";
                }

                return new ConfigResponse
                {
                    Key = existingConfig.Key,
                    Value = existingConfig.Value,
                    Scope = updatedScope,
                    TenantId = updatedScope == DtoScope.Global ? null : existingConfig.TenantId,
                    CompanyId = updatedScope == DtoScope.Company ? existingConfig.CompanyId : null,
                    Version = existingConfig.Version ?? 0,
                    Domain = domainName
                };
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<ConfigResponse?> RollbackAsync(string key, int version, Guid? tenantId, string? domain, Guid correlationId)
        {
            using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var target = await _db.OutboxEvents.FirstOrDefaultAsync(e => e.Key == key && e.Version == version && e.TenantId == tenantId);
                if (target == null) return null;

                var maxVersion = await _db.OutboxEvents.Where(e => e.Key == key).MaxAsync(e => (int?)e.Version) ?? 0;
                var newVersion = maxVersion + 1;

                var rollbackScope = FromIntScope(target.Scope);
                var rollbackCompanyId = (rollbackScope == DtoScope.Company)
                    ? (Guid?)(target.CompanyId ?? Guid.NewGuid())
                    : null;

                var newEvent = new OutboxEvent
                {
                    Id = Guid.NewGuid(),
                    Key = key,
                    Value = target.Value,
                    Scope = target.Scope,
                    TenantId = target.TenantId,
                    CompanyId = rollbackCompanyId,
                    Version = newVersion,
                    DomainId = target.DomainId,
                    EventType = "ConfigRolledBack",
                    CorrelationId = correlationId,
                    OccurredAt = DateTime.UtcNow,
                    Dispatched = false
                };

                _db.OutboxEvents.Add(newEvent);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return new ConfigResponse
                {
                    Key = newEvent.Key,
                    Value = newEvent.Value,
                    Scope = rollbackScope,
                    TenantId = newEvent.TenantId,
                    CompanyId = rollbackScope == DtoScope.Company ? newEvent.CompanyId : null,
                    Version = newEvent.Version ?? 0,
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

    public class MyScopeFilter : IScopeFilter
    {
        private readonly ICurrentUser _currentUser;

        public MyScopeFilter(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        public IQueryable<T> ApplyScopeFilter<T>(IQueryable<T> query) where T : class
        {
            if (typeof(T).IsAssignableTo(typeof(BaseEntity)))
            {
                var tenantId = _currentUser.TenantId;
                var companyId = _currentUser.CompanyId;

                query = query.Where(e => EF.Property<Guid>(e, "TenantId") == tenantId &&
                                         EF.Property<Guid>(e, "CompanyId") == companyId);
            }

            return query;
        }
    }
}