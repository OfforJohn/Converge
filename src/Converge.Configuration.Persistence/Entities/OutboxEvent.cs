using System;
using ConvergeERP.Shared.Domain;

namespace Converge.Configuration.Persistence.Entities
{
    /// <summary>
    /// OutboxEvent is the single source of truth for all configurations.
    /// Supports multiple configs with same key but different tenant/company/domain IDs.
    /// Events: ConfigCreated, ConfigUpdated, ConfigRolledBack
    /// </summary>
    public class OutboxEvent : BaseEntity
    {
        // Override TenantId and CompanyId to make them nullable
        public new Guid? TenantId { get; set; }
        public new Guid? CompanyId { get; set; }
        
        // Configuration data
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int Scope { get; set; }  // 0=Global, 1=Tenant, 2=Company
        public Guid? DomainId { get; set; }  // Foreign key to Domain table
        
        // Event metadata
        public string EventType { get; set; } = string.Empty;
        public Guid CorrelationId { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        
        // Outbox pattern field
        public bool Dispatched { get; set; } = false;

        // Number of dispatch attempts
        public int Attempts { get; set; } = 0;
    }
}
