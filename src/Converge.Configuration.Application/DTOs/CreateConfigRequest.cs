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
        /// Required for Company scope
        /// </summary>
        public string? Domain { get; set; }


        /// <summary>
        /// TenantId is server-generated for Company and Tenant scopes
        /// </summary>
        internal Guid? TenantId { get; set; } // ✅ make nullable

        /// <summary>
        /// Validates domain presence for Company scope
        /// </summary>
        public void Validate()
        {
            switch (Scope)
            {
                case ConfigurationScope.Company:
                    if (string.IsNullOrWhiteSpace(Domain))
                        throw new ValidationException("Domain is required for Company scope.");
                    break;

                case ConfigurationScope.Tenant:
                    // TenantId will be provided/generated server-side if needed
                    break;

                case ConfigurationScope.Global:
                    // No extra validation
                    break;
            }
        }
    }
}
