using FluentValidation;
using Converge.Configuration.Domain.Enums;
using ConfigurationEntity = ConvergeErp.Configuration.Domain.Entities.Configuration;

namespace Converge.Configuration.Domain.Validators
{
    public class ConfigurationValidator : AbstractValidator<ConfigurationEntity>
    {
        public ConfigurationValidator()
        {
            RuleFor(x => x.Key)
                .NotEmpty()
                .WithMessage("Config key is required");

            RuleFor(x => x.Value)
                .NotEmpty()
                .WithMessage("Config value is required");

            RuleFor(x => x.TenantId)
                .NotEqual(Guid.Empty)
                .When(x => x.Scope == ConfigurationScope.Tenant)
                .WithMessage("TenantId is required for TENANT scoped config");

            RuleFor(x => x.TenantId)
                .Equal(Guid.Empty)
                .When(x => x.Scope == ConfigurationScope.Global)
                .WithMessage("TenantId must be null for GLOBAL scoped config");

            RuleFor(x => x.CompanyId)
                .NotNull()
                .When(x => x.Scope == ConfigurationScope.Company)
                .WithMessage("CompanyId is required for COMPANY scoped config");

        }
    }

    /// <summary>
    /// Validator for configuration creation parameters (before entity is created)
    /// </summary>
    public class CreateConfigurationValidator : AbstractValidator<CreateConfigurationParams>
    {
        public CreateConfigurationValidator()
        {
            RuleFor(x => x.Key)
                .NotEmpty()
                .WithMessage("Config key is required");

            RuleFor(x => x.Value)
                .NotEmpty()
                .WithMessage("Config value is required");

            RuleFor(x => x.TenantId)
                .NotNull()
                .When(x => x.Scope == ConfigurationScope.Tenant)
                .WithMessage("TenantId is required for TENANT scoped config");

            RuleFor(x => x.TenantId)
                .Null()
                .When(x => x.Scope == ConfigurationScope.Global)
                .WithMessage("TenantId must be null for GLOBAL scoped config");

            RuleFor(x => x.TenantId)
                .NotNull()
                .When(x => x.Scope == ConfigurationScope.Company)
                .WithMessage("TenantId is required for COMPANY scoped config");
        }
    }

    /// <summary>
    /// Parameters for creating a configuration (used for validation before entity creation)
    /// </summary>
    public record CreateConfigurationParams(
        string Key,
        string Value,
        ConfigurationScope Scope,
        Guid? TenantId,
        Guid? CompanyId,
        int Version,
        Guid CreatorId
    );
}
