using System;

namespace Converge.Configuration.Application.Handlers.Requests
{
    /// <summary>
    /// Request to read the effective configuration for a key.
    /// </summary>
    public class GetConfigQuery
    {
        public string Key { get; }
        public Guid? TenantId { get; }
        public int? Version { get; }
        public string CorrelationId { get; }

        public GetConfigQuery(string key, Guid? tenantId, int? version, string correlationId)
        {
            Key = key;
            TenantId = tenantId;
            Version = version;
            CorrelationId = correlationId;
        }
    }
}
