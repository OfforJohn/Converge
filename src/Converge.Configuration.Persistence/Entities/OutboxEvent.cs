using System;
using ConvergeERP.Shared.Domain;

namespace Converge.Configuration.Persistence.Entities
{
    /// <summary>
    /// Outbox event for reliable event publishing (Outbox Pattern)
    /// Events: ConfigCreated, ConfigUpdated, ConfigRolledBack
    /// Inherits from BaseEntity: Id, TenantId, CompanyId, Version
    /// </summary>
    public class OutboxEvent : BaseEntity
    {
        // Configuration data
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int Scope { get; set; }  // 0=Global, 1=Tenant, 2=Company
        public Guid? DomainId { get; set; }  // Foreign key to Domain table
        
        // Event metadata
        public string EventType { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        
        // Outbox pattern field
        public bool Dispatched { get; set; } = false;
    }
}
