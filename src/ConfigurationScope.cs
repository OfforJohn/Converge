Converge.Configuration.DTOs\CreateConfigRequest.cs
using System;

namespace Converge.Configuration.DTOs
{
    public enum ConfigurationScope
    {
        Global,
        Tenant
    }

    public class CreateConfigRequest
    {
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
        public ConfigurationScope Scope { get; set; }
        public Guid? TenantId { get; set; }
    }
}