using Converge.Configuration.Domain.Enums;
using Converge.Configuration.Domain.Validators;
using ConvergeERP.Shared.Domain;
using FluentValidation;

namespace ConvergeErp.Configuration.Domain.Entities
{
    public class Configuration : BaseEntity
    {
        private static readonly CreateConfigurationValidator _validator = new();

        public string Key { get; private set; } = null!;
        public string Value { get; private set; } = null!;
        public ConfigurationScope Scope { get; private set; }
        public Guid? CompanyId { get; private set; }

        // ✅ Renamed to avoid BaseEntity.Status collision
        public ConfigurationStatus ConfigStatus { get; private set; }

        private Configuration() { } // EF Core

        public Configuration(
            string key,
            string value,
            ConfigurationScope scope,
            Guid? tenantId,
            Guid? companyId,
            int version,
            Guid creatorId)
        {
            // Validate using FluentValidation
            var parameters = new CreateConfigurationParams(key, value, scope, tenantId, companyId, version, creatorId);
            var validationResult = _validator.Validate(parameters);
            
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                throw new ValidationException(errors, validationResult.Errors);
            }

            Key = key;
            Value = value;
            Scope = scope;
            TenantId = scope == ConfigurationScope.Global
                ? Guid.Empty
                : tenantId!.Value;
            CompanyId = scope == ConfigurationScope.Company ? companyId : null;
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
