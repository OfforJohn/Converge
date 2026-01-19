using System;
using System.ComponentModel.DataAnnotations;

namespace Converge.Configuration.Persistence.Entities
{
    /// <summary>
    /// Scope for configuration: GLOBAL, TENANT, or COMPANY
    /// </summary>
    public enum ConfigurationScope
    {
        Global = 0,
        Tenant = 1,
        Company = 2
    }

    /// <summary>
    /// Configuration entity as per user story:
    /// - key, value (JSON), scope, tenant_id, company_id, version, status, created_by, created_at
    /// - Configurations are immutable - updates create new versions
    /// </summary>
    public class ConfigurationItem
    {
        public Guid Id { get; set; }
        
        // Core configuration fields
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;  // JSON stored as text
        public ConfigurationScope Scope { get; set; }
        public Guid? TenantId { get; set; }  // Nullable for GLOBAL scope
        public Guid? CompanyId { get; set; }  // Nullable, used when Scope is Company
        public int? Version { get; set; }
        public string Status { get; set; } = "ACTIVE";  // ACTIVE | DEPRECATED
        
        // Audit fields
        public Guid? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Domain/Module namespacing - uses DomainEntity
        public Guid? DomainId { get; set; }
        public DomainEntity? Domain { get; set; }  // Navigation property to DomainEntity
    }
}
