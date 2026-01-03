using System;
using System.Threading.Tasks;
using Converge.Configuration.DTOs;
using Converge.Configuration.Services;
using Xunit;

namespace Converge.Configuration.UnitTests.Configuration.Application
{
    public class ConfigurationServiceTests
    {
        private readonly IConfigService _service;

        public ConfigurationServiceTests()
        {
            _service = new InMemoryConfigService();
        }

        [Fact]
        public async Task Create_Then_GetEffective_ReturnsCreated()
        {
            var request = new CreateConfigRequest
            {
                Key = "site:title",
                Value = "My Site",
                Scope = ConfigurationScope.Global,
                TenantId = null
            };

            var created = await _service.CreateAsync(request, "corr-1");

            Assert.NotNull(created);
            Assert.Equal("site:title", created.Key);
            Assert.Equal("My Site", created.Value);
            Assert.Equal(ConfigurationScope.Global, created.Scope);
            Assert.Equal(1, created.Version);

            var fetched = await _service.GetEffectiveAsync("site:title", null, null, "corr-2");
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

            var c = await _service.CreateAsync(create, "corr-1");
            var updateReq = new UpdateConfigRequest
            {
                Value = "on",
                ExpectedVersion = c.Version
            };

            var updated = await _service.UpdateAsync("feature:x", updateReq, "corr-2");
            Assert.NotNull(updated);
            Assert.Equal("on", updated!.Value);
            Assert.True(updated.Version > c.Version);

            var effective = await _service.GetEffectiveAsync("feature:x", null, null, "corr-3");
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
            var v1 = await _service.CreateAsync(initial, "c1");

            // update -> version 2
            var upd = new UpdateConfigRequest
            {
                Value = "v2",
                ExpectedVersion = v1.Version
            };
            var v2 = await _service.UpdateAsync(key, upd, "c2");

            // rollback to v1
            var rolled = await _service.RollbackAsync(key, v1.Version, tenantId, "c3");
            Assert.NotNull(rolled);
            Assert.Equal("v1", rolled!.Value);
            Assert.True(rolled.Version > v2!.Version);

            // effective should be rolled value
            var effective = await _service.GetEffectiveAsync(key, tenantId, null, "c4");
            Assert.NotNull(effective);
            Assert.Equal("v1", effective!.Value);
            Assert.Equal(rolled.Version, effective.Version);
        }
    }
}
