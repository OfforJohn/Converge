using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ConvergeErp.Configuration.Domain.Entities;
using Converge.Configuration.Domain.Enums;

namespace Converge.Configuration.Persistence
{
    public class EfConfigurationRepository : IConfigurationRepository
    {
        private readonly ConfigurationDbContext _db;

        public EfConfigurationRepository(ConfigurationDbContext db)
        {
            _db = db;
        }

        public async Task<Configuration?> GetLatestAsync(string key, Guid? tenantId)
        {
            if (tenantId.HasValue)
            {
                var tenantCfg = await _db.Configurations
                    .Where(c => EF.Property<string>(c, "Key") == key && EF.Property<int>(c, "Scope") == (int)ConfigurationScope.Tenant && EF.Property<Guid>(c, "TenantId") == tenantId && EF.Property<int>(c, "ConfigStatus") == (int)ConfigurationStatus.Active)
                    .OrderByDescending(c => EF.Property<int>(c, "Version"))
                    .FirstOrDefaultAsync();

                if (tenantCfg != null)
                    return tenantCfg;
            }

            var globalCfg = await _db.Configurations
                .Where(c => EF.Property<string>(c, "Key") == key && EF.Property<int>(c, "Scope") == (int)ConfigurationScope.Global && EF.Property<int>(c, "ConfigStatus") == (int)ConfigurationStatus.Active)
                .OrderByDescending(c => EF.Property<int>(c, "Version"))
                .FirstOrDefaultAsync();

            return globalCfg;
        }

        public async Task<Configuration?> GetByKeyVersionAsync(string key, Guid? tenantId, int version)
        {
            // If tenantId is provided, filter by that tenant. If null, don't filter by tenant (search across tenants and global)
            var q = _db.Configurations.AsQueryable()
                .Where(c => EF.Property<string>(c, "Key") == key && EF.Property<int>(c, "Version") == version);

            if (tenantId.HasValue)
                q = q.Where(c => EF.Property<Guid>(c, "TenantId") == tenantId.Value);

            return await q.FirstOrDefaultAsync();
        }

        public async Task<int> GetMaxVersionAsync(string key)
        {
            var max = await _db.Configurations
                .Where(c => EF.Property<string>(c, "Key") == key)
                .MaxAsync(c => (int?)EF.Property<int>(c, "Version"));

            return max ?? 0;
        }

        public async Task AddAsync(Configuration configuration)
        {
            _db.Configurations.Add(configuration);
            await _db.SaveChangesAsync();
        }

        public async Task DeprecateAsync(Configuration configuration)
        {
            // Set the shadow property and save
            _db.Entry(configuration).Property("ConfigStatus").CurrentValue = (int)ConfigurationStatus.Deprecated;
            _db.Entry(configuration).Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task<bool> ExistsVersionAsync(string key, Guid? tenantId, int version)
        {
            var q = _db.Configurations.AsQueryable()
                .Where(c => EF.Property<string>(c, "Key") == key && EF.Property<int>(c, "Version") == version);

            if (tenantId.HasValue)
                q = q.Where(c => EF.Property<Guid>(c, "TenantId") == tenantId.Value);

            return await q.AnyAsync();
        }
    }
}
