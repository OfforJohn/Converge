using System;
using System.ComponentModel.DataAnnotations;
using ConvergeERP.Shared.Domain;

namespace Converge.Configuration.Persistence.Entities
{
    public enum ConfigurationScope
    {
        Global,
        Tenant
    }

    public class ConfigurationItem : BaseEntity
    {
        // Id, CreatedAt, CreatedBy, UpdatedAt, Status come from BaseEntity in ConvergeERP.Shared.Domain

        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!; // JSON stored as text
        public ConfigurationScope Scope { get; set; }
        public Guid? TenantId { get; set; }
        public int Version { get; set; }
    }
}
