using System;
using System.Threading.Tasks;
using Converge.Configuration.Application.Handlers.Implementations;
using Converge.Configuration.Application.Handlers.Requests;
using Converge.Configuration.DTOs;
using Converge.Configuration.Services;
using Converge.UnitTests.Mocks;
using Xunit;

namespace Converge.Configuration.UnitTests.Handlers
{
    public class ConfigHandlerTests
    {
        #region Global Scope Tests

        [Fact]
        public async Task CreateConfigHandler_GlobalScope_CreatesAndReturnsConfig()
        {
            // Arrange - Set scope to Global (no tenantId, no companyId)
            var scopeContext = new TestScopeContext(ConfigurationScope.Global);
            IConfigService service = new InMemoryConfigService(scopeContext);
            var handler = new CreateConfigHandler(service);

            var request = new CreateConfigRequest
            {
                Key = "global.config.key",
                Value = "global-value",
                Domain = "Settings"
            };

            var command = new CreateConfigCommand(request, Guid.Parse("00000000-0000-0000-0000-000000000001"));

            // Act
            var result = await handler.Handle(command);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Key, result.Key);
            Assert.Equal(request.Value, result.Value);
            Assert.Equal(ConfigurationScope.Global, result.Scope);
            Assert.Null(result.TenantId);
            Assert.Null(result.CompanyId);
            Assert.Equal(1, result.Version);
        }

        [Fact]
        public async Task GetConfigHandler_GlobalScope_ReturnsEffectiveConfig()
        {
            // Arrange
            var scopeContext = new TestScopeContext(ConfigurationScope.Global);
            IConfigService service = new InMemoryConfigService(scopeContext);
            var createHandler = new CreateConfigHandler(service);
            var getHandler = new GetConfigHandler(service);

            var request = new CreateConfigRequest
            {
                Key = "global.sample.key",
                Value = "v1"
            };

            var createCmd = new CreateConfigCommand(request, Guid.Parse("00000000-0000-0000-0000-000000000002"));
            var created = await createHandler.Handle(createCmd);

            var query = new GetConfigQuery(created.Key, null, null, null, Guid.Parse("00000000-0000-0000-0000-000000000002"));

            // Act
            var result = await getHandler.Handle(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(created.Key, result!.Key);
            Assert.Equal(created.Value, result.Value);
            Assert.Equal(ConfigurationScope.Global, result.Scope);
        }

        #endregion

        #region Tenant Scope Tests

        [Fact]
        public async Task CreateConfigHandler_TenantScope_CreatesWithTenantId()
        {
            // Arrange - Set scope to Tenant with a specific tenantId
            var tenantId = Guid.NewGuid();
            var scopeContext = new TestScopeContext(ConfigurationScope.Tenant, tenantId: tenantId);
            IConfigService service = new InMemoryConfigService(scopeContext);
            var handler = new CreateConfigHandler(service);

            var request = new CreateConfigRequest
            {
                Key = "tenant.config.key",
                Value = "tenant-value",
                Domain = "HR"
            };

            var command = new CreateConfigCommand(request, Guid.Parse("00000000-0000-0000-0000-000000000003"));

            // Act
            var result = await handler.Handle(command);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Key, result.Key);
            Assert.Equal(request.Value, result.Value);
            Assert.Equal(ConfigurationScope.Tenant, result.Scope);
            Assert.Equal(tenantId, result.TenantId);
            Assert.Null(result.CompanyId);
            Assert.Equal(1, result.Version);
        }

        [Fact]
        public async Task GetConfigHandler_TenantScope_ReturnsConfigForTenant()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var scopeContext = new TestScopeContext(ConfigurationScope.Tenant, tenantId: tenantId);
            IConfigService service = new InMemoryConfigService(scopeContext);
            var createHandler = new CreateConfigHandler(service);
            var getHandler = new GetConfigHandler(service);

            var request = new CreateConfigRequest
            {
                Key = "tenant.sample.key",
                Value = "tenant-v1",
                Domain = "Finance"
            };

            var createCmd = new CreateConfigCommand(request, Guid.Parse("00000000-0000-0000-0000-000000000004"));
            var created = await createHandler.Handle(createCmd);

            // Query with tenantId
            var query = new GetConfigQuery(created.Key, tenantId, null, null, Guid.Parse("00000000-0000-0000-0000-000000000004"));

            // Act
            var result = await getHandler.Handle(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(created.Key, result!.Key);
            Assert.Equal(ConfigurationScope.Tenant, result.Scope);
            Assert.Equal(tenantId, result.TenantId);
        }

        #endregion

        #region Company Scope Tests

        [Fact]
        public async Task CreateConfigHandler_CompanyScope_CreatesWithTenantAndCompanyId()
        {
            // Arrange - Set scope to Company with tenantId and companyId
            var tenantId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var scopeContext = new TestScopeContext(ConfigurationScope.Company, tenantId: tenantId, companyId: companyId);
            IConfigService service = new InMemoryConfigService(scopeContext);
            var handler = new CreateConfigHandler(service);

            var request = new CreateConfigRequest
            {
                Key = "company.config.key",
                Value = "company-value",
                Domain = "Sales"
            };

            var command = new CreateConfigCommand(request, Guid.Parse("00000000-0000-0000-0000-000000000005"));

            // Act
            var result = await handler.Handle(command);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Key, result.Key);
            Assert.Equal(request.Value, result.Value);
            Assert.Equal(ConfigurationScope.Company, result.Scope);
            Assert.NotNull(result.TenantId);  // Should have tenantId
            Assert.NotNull(result.CompanyId); // Should have companyId
            Assert.Equal(1, result.Version);
        }

        [Fact]
        public async Task GetConfigHandler_CompanyScope_ReturnsConfigForCompany()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var scopeContext = new TestScopeContext(ConfigurationScope.Company, tenantId: tenantId, companyId: companyId);
            IConfigService service = new InMemoryConfigService(scopeContext);
            var createHandler = new CreateConfigHandler(service);
            var getHandler = new GetConfigHandler(service);

            var request = new CreateConfigRequest
            {
                Key = "company.sample.key",
                Value = "company-v1",
                Domain = "Operations"
            };

            var createCmd = new CreateConfigCommand(request, Guid.Parse("00000000-0000-0000-0000-000000000006"));
            var created = await createHandler.Handle(createCmd);

            // Query with companyId (not tenantId for Company scope)
            var query = new GetConfigQuery(created.Key, null, created.CompanyId, null, Guid.Parse("00000000-0000-0000-0000-000000000006"));

            // Act
            var result = await getHandler.Handle(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(created.Key, result!.Key);
            Assert.Equal(ConfigurationScope.Company, result.Scope);
            Assert.Equal(created.CompanyId, result.CompanyId);
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task UpdateConfigHandler_TenantScope_UpdatesConfig()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var scopeContext = new TestScopeContext(ConfigurationScope.Tenant, tenantId: tenantId);
            IConfigService service = new InMemoryConfigService(scopeContext);
            var createHandler = new CreateConfigHandler(service);
            var updateHandler = new UpdateConfigHandler(service);

            // Create initial config
            var createRequest = new CreateConfigRequest
            {
                Key = "tenant.update.key",
                Value = "initial-value"
            };
            var created = await createHandler.Handle(new CreateConfigCommand(createRequest, Guid.Parse("00000000-0000-0000-0000-000000000007")));

            // Update request
            var updateRequest = new UpdateConfigRequest
            {
                Value = "updated-value",
                ExpectedVersion = created.Version
            };

            // Act
            var updated = await updateHandler.Handle(new UpdateConfigCommand(created.Key, updateRequest, Guid.Parse("00000000-0000-0000-0000-000000000008")));

            // Assert
            Assert.NotNull(updated);
            Assert.Equal("updated-value", updated!.Value);
            Assert.Equal(created.Version + 1, updated.Version);
            Assert.Equal(tenantId, updated.TenantId);
        }

        [Fact]
        public async Task UpdateConfigHandler_CompanyScope_UpdatesConfig()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var scopeContext = new TestScopeContext(ConfigurationScope.Company, tenantId: tenantId, companyId: companyId);
            IConfigService service = new InMemoryConfigService(scopeContext);
            var createHandler = new CreateConfigHandler(service);
            var updateHandler = new UpdateConfigHandler(service);

            // Create initial config
            var createRequest = new CreateConfigRequest
            {
                Key = "company.update.key",
                Value = "initial-company-value"
            };
            var created = await createHandler.Handle(new CreateConfigCommand(createRequest, Guid.Parse("00000000-0000-0000-0000-000000000009")));

            // Update request
            var updateRequest = new UpdateConfigRequest
            {
                Value = "updated-company-value",
                ExpectedVersion = created.Version
            };

            // Act
            var updated = await updateHandler.Handle(new UpdateConfigCommand(created.Key, updateRequest, Guid.Parse("00000000-0000-0000-0000-000000000010")));

            // Assert
            Assert.NotNull(updated);
            Assert.Equal("updated-company-value", updated!.Value);
            Assert.NotNull(updated.CompanyId);
        }

        #endregion

        #region Scope Isolation Tests

        [Fact]
        public async Task Configs_AreSeparatedByScope()
        {
            // Arrange - Same key but different scopes
            var tenantId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var key = "shared.config.key";

            // Create Global config
            var globalContext = new TestScopeContext(ConfigurationScope.Global);
            var globalService = new InMemoryConfigService(globalContext);
            var globalHandler = new CreateConfigHandler(globalService);
            var globalConfig = await globalHandler.Handle(new CreateConfigCommand(
                new CreateConfigRequest { Key = key, Value = "global-value" }, Guid.Parse("00000000-0000-0000-0000-000000000011")));

            // Create Tenant config (different service instance to simulate different context)
            var tenantContext = new TestScopeContext(ConfigurationScope.Tenant, tenantId: tenantId);
            var tenantService = new InMemoryConfigService(tenantContext);
            var tenantHandler = new CreateConfigHandler(tenantService);
            var tenantConfig = await tenantHandler.Handle(new CreateConfigCommand(
                new CreateConfigRequest { Key = key, Value = "tenant-value" }, Guid.Parse("00000000-0000-0000-0000-000000000012")));

            // Create Company config
            var companyContext = new TestScopeContext(ConfigurationScope.Company, tenantId: tenantId, companyId: companyId);
            var companyService = new InMemoryConfigService(companyContext);
            var companyHandler = new CreateConfigHandler(companyService);
            var companyConfig = await companyHandler.Handle(new CreateConfigCommand(
                new CreateConfigRequest { Key = key, Value = "company-value" }, Guid.Parse("00000000-0000-0000-0000-000000000013")));

            // Assert - Each scope has its own value
            Assert.Equal("global-value", globalConfig.Value);
            Assert.Equal(ConfigurationScope.Global, globalConfig.Scope);

            Assert.Equal("tenant-value", tenantConfig.Value);
            Assert.Equal(ConfigurationScope.Tenant, tenantConfig.Scope);
            Assert.Equal(tenantId, tenantConfig.TenantId);

            Assert.Equal("company-value", companyConfig.Value);
            Assert.Equal(ConfigurationScope.Company, companyConfig.Scope);
            Assert.NotNull(companyConfig.CompanyId);
        }

        #endregion
    }
}
