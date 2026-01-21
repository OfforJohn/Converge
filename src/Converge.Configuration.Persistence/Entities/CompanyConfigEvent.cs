using System;

namespace Converge.Configuration.Persistence.Entities
{
    /// <summary>
    /// Tenant/Company-specific config event for reliable event publishing
    /// Events: ConfigCreated, ConfigUpdated, ConfigRolledBack
    /// </summary>
    public class CompanyConfigEvent
    {
        public Guid Id { get; set; }
        
        // Configuration data
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int Scope { get; set; }  // 0=Global, 1=Tenant, 2=Company
        public Guid? TenantId { get; set; }
        public Guid? CompanyId { get; set; }
        public int? Version { get; set; }
        public Guid? DomainId { get; set; }  // Foreign key to Domain table
        
        // Event metadata
        public string EventType { get; set; } = string.Empty;  // ConfigCreated, ConfigUpdated, ConfigRolledBack
        public Guid CorrelationId { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        
        // Outbox pattern fields
        public bool Dispatched { get; set; } = false;
        public DateTime? DispatchedAt { get; set; }
        public int Attempts { get; set; } = 0;
    }
}
