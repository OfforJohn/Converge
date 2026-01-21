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
        public Guid? CompanyId { get; }
        public int? Version { get; }
        public Guid CorrelationId { get; }

        public GetConfigQuery(string key, Guid? tenantId, Guid? companyId, int? version, Guid correlationId)
        {
            Key = key;
            TenantId = tenantId;
            CompanyId = companyId;
            Version = version;
            CorrelationId = correlationId;
        }
    }
}
