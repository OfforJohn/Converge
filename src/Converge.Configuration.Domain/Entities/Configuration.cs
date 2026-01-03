using Converge.Configuration.Domain.Enums;
using ConvergeERP.Shared.Domain;

namespace ConvergeErp.Configuration.Domain.Entities
{
    public class Configuration : BaseEntity
    {
        public string Key { get; private set; } = null!;
        public string Value { get; private set; } = null!;
        public ConfigurationScope Scope { get; private set; }

        // ✅ Renamed to avoid BaseEntity.Status collision
        public ConfigurationStatus ConfigStatus { get; private set; }

        private Configuration() { } // EF Core

        public Configuration(
            string key,
            string value,
            ConfigurationScope scope,
            Guid? tenantId,
            int version,
            Guid creatorId)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Config key is required", nameof(key));

            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Config value is required", nameof(value));

            if (scope == ConfigurationScope.Tenant && tenantId == null)
                throw new InvalidOperationException("TenantId is required for TENANT scoped config");

            if (scope == ConfigurationScope.Global && tenantId != null)
                throw new InvalidOperationException("TenantId must be null for GLOBAL scoped config");

            Key = key;
            Value = value;
            Scope = scope;
            TenantId = scope == ConfigurationScope.Global
    ? Guid.Empty
    : tenantId!.Value;

            Version = version;

            ConfigStatus = ConfigurationStatus.Active;

            CreatorId = creatorId;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Domain-controlled state transition.
        /// Used internally by repository during version replacement.
        /// </summary>
        internal void Deprecate()
        {
            if (ConfigStatus == ConfigurationStatus.Deprecated)
                return;

            ConfigStatus = ConfigurationStatus.Deprecated;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
