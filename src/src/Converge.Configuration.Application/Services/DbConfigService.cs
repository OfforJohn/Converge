using System;
using System.Threading.Tasks;
using Converge.Configuration.DTOs;
using Converge.Configuration.Domain.Enums;
using ConvergeErp.Configuration.Domain.Entities;
using Converge.Configuration.Services;

namespace Converge.Configuration.Application.Services
{
    // Minimal DB-backed implementation delegating to the repository. Not feature-complete but sufficient to switch to Postgres.
    public class DbConfigService : IConfigService
    {
        private readonly IConfigurationRepository _repo;

        public DbConfigService(IConfigurationRepository repo)
        {
            _repo = repo;
        }

        public async Task<ConfigResponse?> GetEffectiveAsync(string key, Guid? tenantId, int? version, string correlationId)
        {
            if (version.HasValue)
            {
                var cfg = await _repo.GetByKeyVersionAsync(key, tenantId, version.Value);
                if (cfg == null) return null;
                return new ConfigResponse { Key = cfg.Key, Value = cfg.Value, Scope = cfg.Scope, TenantId = cfg.TenantId == Guid.Empty ? null : cfg.TenantId, Version = cfg.Version };
            }

            var latest = await _repo.GetLatestAsync(key, tenantId);
            if (latest == null) return null;
            return new ConfigResponse { Key = latest.Key, Value = latest.Value, Scope = latest.Scope, TenantId = latest.TenantId == Guid.Empty ? null : latest.TenantId, Version = latest.Version };
        }

        public async Task<ConfigResponse> CreateAsync(CreateConfigRequest request, string correlationId)
        {
            var max = await _repo.GetMaxVersionAsync(request.Key);
            if (await _repo.ExistsVersionAsync(request.Key, request.TenantId, max))
                throw new Converge.Configuration.Application.Exceptions.ConfigurationAlreadyExistsException(request.Key);

            var version = max + 1;
            var creator = Guid.Empty; // TODO: resolve from context
            var entity = new Configuration(request.Key, request.Value, request.Scope, request.TenantId, version, creator);
            await _repo.AddAsync(entity);

            return new ConfigResponse { Key = entity.Key, Value = entity.Value, Scope = entity.Scope, TenantId = entity.TenantId == Guid.Empty ? null : entity.TenantId, Version = entity.Version };
        }

        public async Task<ConfigResponse?> UpdateAsync(string key, UpdateConfigRequest request, string correlationId)
        {
            // Load latest
            var active = await _repo.GetLatestAsync(key, null);
            if (active == null) return null;

            if (request.ExpectedVersion.HasValue && request.ExpectedVersion.Value != active.Version)
                throw new Converge.Configuration.Application.Exceptions.VersionConflictException(key, request.ExpectedVersion.Value, active.Version);

            // Deprecate
            await _repo.DeprecateAsync(active);

            var version = await _repo.GetMaxVersionAsync(key) + 1;
            var creator = Guid.Empty;
            var newCfg = new Configuration(key, request.Value, active.Scope, active.TenantId == Guid.Empty ? null : active.TenantId, version, creator);
            await _repo.AddAsync(newCfg);

            return new ConfigResponse { Key = newCfg.Key, Value = newCfg.Value, Scope = newCfg.Scope, TenantId = newCfg.TenantId == Guid.Empty ? null : newCfg.TenantId, Version = newCfg.Version };
        }

        public async Task<ConfigResponse?> RollbackAsync(string key, int version, Guid? tenantId, string correlationId)
        {
            var target = await _repo.GetByKeyVersionAsync(key, tenantId, version);
            if (target == null) return null;

            var active = await _repo.GetLatestAsync(key, tenantId);
            if (active != null)
                await _repo.DeprecateAsync(active);

            var newVersion = await _repo.GetMaxVersionAsync(key) + 1;
            var creator = Guid.Empty;
            var newCfg = new Configuration(key, target.Value, target.Scope, target.TenantId == Guid.Empty ? null : target.TenantId, newVersion, creator);
            await _repo.AddAsync(newCfg);

            return new ConfigResponse { Key = newCfg.Key, Value = newCfg.Value, Scope = newCfg.Scope, TenantId = newCfg.TenantId == Guid.Empty ? null : newCfg.TenantId, Version = newCfg.Version };
        }
    }
}
