using System;
using System.Threading.Tasks;

namespace Converge.Configuration.Application.Services
{
    public class ConsoleAuditService : IAuditService
    {
        public ConsoleAuditService()
        {
        }

        public Task AuditAsync(string action, string key, object? before, object? after, string? actor, Guid? tenantId, Guid correlationId)
        {
            Console.WriteLine($"AUDIT {action} key={key} actor={actor} tenant={tenantId} correlation={correlationId} before={before} after={after}");
            return Task.CompletedTask;
        }
    }
}
