using System;
using System.Threading.Tasks;

namespace Converge.Configuration.Application.Services
{
    public interface IAuditService
    {
        Task AuditAsync(string action, string key, object? before, object? after, string? actor, Guid? tenantId, string correlationId);
    }
}
