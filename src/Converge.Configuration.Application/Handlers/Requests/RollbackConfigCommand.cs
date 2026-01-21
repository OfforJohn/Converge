using System;

namespace Converge.Configuration.Application.Handlers.Requests
{
    public class RollbackConfigCommand
    {
        public string Key { get; }
        public int Version { get; }
        public Guid? TenantId { get; }
        public Guid CorrelationId { get; }

        public RollbackConfigCommand(string key, int version, Guid? tenantId, Guid correlationId)
        {
            Key = key;
            Version = version;
            TenantId = tenantId;
            CorrelationId = correlationId;
        }
    }
}
