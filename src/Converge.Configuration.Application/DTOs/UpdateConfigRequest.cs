using System;
using System.ComponentModel.DataAnnotations;

namespace Converge.Configuration.DTOs
{
    public class UpdateConfigRequest
    {
        public string Value { get; set; } = null!;
        public int? ExpectedVersion { get; set; }
        public ConfigurationScope Scope { get; set; }
        public Guid? TenantId { get; set; }

        /// <summary>
        /// Required for Tenant and Company scopes
        /// </summary>
        public string? Domain { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Value))
                throw new ArgumentException("Value cannot be null or empty.");

            if (ExpectedVersion.HasValue && ExpectedVersion < 0)
                throw new ArgumentException("ExpectedVersion cannot be negative.");

            // Validate Scope
            if (!Enum.IsDefined(typeof(ConfigurationScope), Scope))
                throw new ArgumentException("Invalid configuration scope.");

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
