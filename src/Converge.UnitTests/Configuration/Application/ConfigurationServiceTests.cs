using System;
using System.Threading.Tasks;
using Converge.Configuration.DTOs;
using Converge.Configuration.Services;
using Converge.UnitTests.Mocks;
using Xunit;

namespace Converge.Configuration.UnitTests.Configuration.Application
{
    public class ConfigurationServiceTests
    {
        private readonly IConfigService _service;
        private readonly TestScopeContext _scopeContext;

        public ConfigurationServiceTests()
        {
            _scopeContext = new TestScopeContext();
            _service = new InMemoryConfigService(_scopeContext);
        }

        [Fact]
        public async Task Create_Then_GetEffective_ReturnsCreated()
        {
            // Set scope to Global for this test
            _scopeContext.CurrentScope = ConfigurationScope.Global;
            
            var request = new CreateConfigRequest
            {
                Key = "site:title",
                Value = "My Site",
                Scope = ConfigurationScope.Global,
                TenantId = null
            };

            var created = await _service.CreateAsync(request, Guid.Parse("00000000-0000-0000-0000-000000000001"));



            Assert.NotNull(created);
            Assert.Equal("site:title", created.Key);
            Assert.Equal("My Site", created.Value);
            Assert.Equal(ConfigurationScope.Global, created.Scope);
            Assert.Equal(1, created.Version);

            var fetched = await _service.GetEffectiveAsync("site:title", null, null, null, Guid.Parse("00000000-0000-0000-0000-000000000002"));
            Assert.NotNull(fetched);
            Assert.Equal(created.Value, fetched!.Value);
            Assert.Equal(created.Version, fetched.Version);
        }

        [Fact]
        public async Task Update_CreatesNewVersion_And_GetEffectiveReturnsLatest()
        {
            var create = new CreateConfigRequest
            {
                Key = "feature:x",
                Value = "off",
                Scope = ConfigurationScope.Global
            };

            var c = await _service.CreateAsync(create, Guid.Parse("00000000-0000-0000-0000-000000000001"));
            var updateReq = new UpdateConfigRequest
            {
                Value = "on",
                ExpectedVersion = c.Version
            };

            var updated = await _service.UpdateAsync("feature:x", updateReq, Guid.Parse("00000000-0000-0000-0000-000000000002"));
            Assert.NotNull(updated);
            Assert.Equal("on", updated!.Value);
            Assert.True(updated.Version > c.Version);

            var effective = await _service.GetEffectiveAsync("feature:x", null, null, null, Guid.Parse("00000000-0000-0000-0000-000000000003"));
            Assert.NotNull(effective);
            Assert.Equal("on", effective!.Value);
            Assert.Equal(updated.Version, effective.Version);
        }

        [Fact]
        public async Task Rollback_ToPreviousVersion_Works()
        {
            var key = "tenant:welcome";
            var tenantId = Guid.NewGuid();

            // create tenant-specific version 1
            var initial = new CreateConfigRequest
            {
                Key = key,
                Value = "v1",
                Scope = ConfigurationScope.Tenant,
                TenantId = tenantId
            };
            var v1 = await _service.CreateAsync(initial, Guid.Parse("00000000-0000-0000-0000-000000000001"));

            // update -> version 2
            var upd = new UpdateConfigRequest
            {
                Value = "v2",
                ExpectedVersion = v1.Version
            };
            var v2 = await _service.UpdateAsync(key, upd, Guid.Parse("00000000-0000-0000-0000-000000000002"));

            // rollback to v1
            var rolled = await _service.RollbackAsync(key, v1.Version, tenantId, Guid.Parse("00000000-0000-0000-0000-000000000003"));
            Assert.NotNull(rolled);
            Assert.Equal("v1", rolled!.Value);
            Assert.True(rolled.Version > v2!.Version);

            // effective should be rolled value
            var effective = await _service.GetEffectiveAsync(key, tenantId, null, null, Guid.Parse("00000000-0000-0000-0000-000000000004"));
            Assert.NotNull(effective);
            Assert.Equal("v1", effective!.Value);
            Assert.Equal(rolled.Version, effective.Version);
        }
    }
}
