using System;
using System.ComponentModel.DataAnnotations;

namespace Converge.Configuration.DTOs
{
    public enum ConfigurationScope
    {
        Global,
        Tenant,
        Company
    }

    public class CreateConfigRequest
    {
        [Required]
        public string Key { get; set; } = null!;

        [Required]
        public string Value { get; set; } = null!;

        [Required]
        public ConfigurationScope Scope { get; set; }

        /// <summary>
        /// Required for Tenant and Company scopes
        /// </summary>
        public string? Domain { get; set; }

        /// <summary>
        /// TenantId is server-generated for Tenant and Company scopes
        /// </summary>
        internal Guid? TenantId { get; set; }

        /// <summary>
        /// Validates the request based on the scope
        /// </summary>
        public void Validate()
        {
            switch (Scope)
            {
                case ConfigurationScope.Company:
                case ConfigurationScope.Tenant:
                    if (string.IsNullOrWhiteSpace(Domain))
                        throw new ValidationException("Domain is required for Tenant and Company scopes.");
                    break;

                case ConfigurationScope.Global:
                    if (!string.IsNullOrWhiteSpace(Domain))
                        throw new ValidationException("Domain is not allowed for Global scope.");
                    break;
            }
        }
    }
}
