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
        /// The scope of the configuration to update (extracted from Bearer token)
        /// </summary>
        public ConfigurationScope? Scope { get; set; }
        
        /// <summary>
        /// Tenant ID (extracted from Bearer token)
        /// </summary>
        public Guid? TenantId { get; set; }
        
        /// <summary>
        /// Company ID (extracted from Bearer token)
        /// </summary>
        public Guid? CompanyId { get; set; }

        /// <summary>
        /// Domain for domain-scoped global configurations
        /// </summary>
        public string? Domain { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Value))
                throw new ArgumentException("Value cannot be null or empty.");

            if (ExpectedVersion.HasValue && ExpectedVersion < 0)
                throw new ArgumentException("ExpectedVersion cannot be negative.");

            // Validate Scope (if provided)
            if (Scope.HasValue && !Enum.IsDefined(typeof(ConfigurationScope), Scope.Value))
                throw new ArgumentException("Invalid configuration scope.");

            // For Tenant scope, TenantId is required
            if (Scope == ConfigurationScope.Tenant && TenantId == null)
                throw new ValidationException("TenantId is required to update Tenant scoped config.");
            
            // For Company scope, CompanyId is required (not TenantId)
            if (Scope == ConfigurationScope.Company && CompanyId == null)
                throw new ValidationException("CompanyId is required to update Company scoped config.");
        }
    }
}
