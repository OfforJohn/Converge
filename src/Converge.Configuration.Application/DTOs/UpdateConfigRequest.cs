using System;
using System.ComponentModel.DataAnnotations;

namespace Converge.Configuration.DTOs
{
    public class UpdateConfigRequest
    {
        /// <summary>
        /// The new value for the configuration
        /// </summary>
        [Required]
        public string Value { get; set; } = null!;
        
        /// <summary>
        /// Optional: For optimistic concurrency control
        /// </summary>
        public int? ExpectedVersion { get; set; }
        
        /// <summary>
        /// The scope of the configuration to update
        /// </summary>
        public ConfigurationScope Scope { get; set; }
        
        /// <summary>
        /// Required for Tenant and Company scopes to identify the config
        /// </summary>
        public Guid? TenantId { get; set; }
        
        /// <summary>
        /// Optional: CompanyId for Company scoped configs
        /// </summary>
        public Guid? CompanyId { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Value))
                throw new ArgumentException("Value cannot be null or empty.");

            if (ExpectedVersion.HasValue && ExpectedVersion < 0)
                throw new ArgumentException("ExpectedVersion cannot be negative.");

            // Validate Scope
            if (!Enum.IsDefined(typeof(ConfigurationScope), Scope))
                throw new ArgumentException("Invalid configuration scope.");

            // For Tenant/Company scope updates, TenantId is required to identify the config
            if (Scope == ConfigurationScope.Tenant && TenantId == null)
                throw new ValidationException("TenantId is required to update Tenant scoped config.");
            
            if (Scope == ConfigurationScope.Company && TenantId == null)
                throw new ValidationException("TenantId is required to update Company scoped config.");
        }
    }
}
