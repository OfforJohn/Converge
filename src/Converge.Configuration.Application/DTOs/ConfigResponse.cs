using System;

namespace Converge.Configuration.DTOs
{
    public class ConfigResponse
    {
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
        public ConfigurationScope Scope { get; set; }
        public Guid? TenantId { get; set; }
        public Guid? CompanyId { get; set; }
        public int Version { get; set; }
        public string? Domain { get; set; }
    }
}
