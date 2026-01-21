using Converge.Configuration.Application.Services;
using Converge.Configuration.DTOs;

namespace Converge.UnitTests.Mocks
{
    /// <summary>
    /// Test implementation of IScopeContext for unit tests.
    /// Allows setting scope, tenantId, and companyId directly.
    /// </summary>
    public class TestScopeContext : IScopeContext
    {
        public ConfigurationScope CurrentScope { get; set; } = ConfigurationScope.Global;
        public Guid? TenantId { get; set; }
        public Guid? CompanyId { get; set; }

        public TestScopeContext() { }

        public TestScopeContext(ConfigurationScope scope, Guid? tenantId = null, Guid? companyId = null)
        {
            CurrentScope = scope;
            TenantId = tenantId;
            CompanyId = companyId;
        }
    }
}
