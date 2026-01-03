using System;
using System.Threading.Tasks;
using Converge.Configuration.DTOs;

namespace Converge.Configuration.Services
{
    public interface IConfigService
    {
        Task<ConfigResponse?> GetEffectiveAsync(string key, Guid? tenantId, int? version, string correlationId);
        Task<ConfigResponse> CreateAsync(CreateConfigRequest request, string correlationId);
        Task<ConfigResponse?> UpdateAsync(string key, UpdateConfigRequest request, string correlationId);
        Task<ConfigResponse?> RollbackAsync(string key, int version, Guid? tenantId, string correlationId);
    }
}
