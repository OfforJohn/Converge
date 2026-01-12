using System;

namespace Converge.Configuration.DTOs
{
    public class UpdateConfigRequest
    {
        public string Value { get; set; } = null!;
        public int? ExpectedVersion { get; set; }
        public ConfigurationScope Scope { get; set; }
        public Guid? TenantId { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Value))
                throw new ArgumentException("Value cannot be null or empty.");

            if (ExpectedVersion.HasValue && ExpectedVersion < 0)
                throw new ArgumentException("ExpectedVersion cannot be negative.");

            // Optional: validate Scope
            if (!Enum.IsDefined(typeof(ConfigurationScope), Scope))
                throw new ArgumentException("Invalid configuration scope.");
        }
    }
}
