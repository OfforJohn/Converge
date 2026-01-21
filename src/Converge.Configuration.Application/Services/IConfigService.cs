using System;
using System.Threading.Tasks;
using Converge.Configuration.DTOs;

namespace Converge.Configuration.Services
{
    public interface IConfigService
    {
        Task<ConfigResponse?> GetEffectiveAsync(string key, Guid? tenantId, Guid? companyId, int? version, Guid correlationId);
        Task<ConfigResponse> CreateAsync(CreateConfigRequest request, Guid correlationId);
        Task<ConfigResponse?> UpdateAsync(string key, UpdateConfigRequest request, Guid correlationId);
        Task<ConfigResponse?> RollbackAsync(string key, int version, Guid? tenantId, Guid correlationId);
    }
}
