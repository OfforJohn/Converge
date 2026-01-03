using System;

namespace Converge.Configuration.Application.Handlers.Requests
{
    public class RollbackConfigCommand
    {
        public string Key { get; }
        public int Version { get; }
        public Guid? TenantId { get; }
        public string CorrelationId { get; }

        public RollbackConfigCommand(string key, int version, Guid? tenantId, string correlationId)
        {
            Key = key;
            Version = version;
            TenantId = tenantId;
            CorrelationId = correlationId;
        }
    }
}
