using System;
using System.Threading.Tasks;
using Converge.Configuration.Application.Handlers.Implementations;
using Converge.Configuration.Application.Handlers.Requests;
using Converge.Configuration.DTOs;
using Converge.Configuration.Services;
using Xunit;

namespace Converge.Configuration.UnitTests.Handlers
{
    public class ConfigHandlerTests
    {
        [Fact]
        public async Task CreateConfigHandler_CreatesAndReturnsConfig()
        {
            // Arrange
            IConfigService service = new InMemoryConfigService();
            var handler = new CreateConfigHandler(service);

            var request = new CreateConfigRequest
            {
                Key = "test.key",
                Value = "test-value",
                Scope = ConfigurationScope.Global,
                TenantId = null
            };

            var command = new CreateConfigCommand(request, "corr-1");

            // Act
            var result = await handler.Handle(command);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Key, result.Key);
            Assert.Equal(request.Value, result.Value);
            Assert.Equal(1, result.Version);
        }

        [Fact]
        public async Task GetConfigHandler_ReturnsEffectiveConfig()
        {
            // Arrange
            IConfigService service = new InMemoryConfigService();
            var createHandler = new CreateConfigHandler(service);
            var getHandler = new GetConfigHandler(service);

            var request = new CreateConfigRequest
            {
                Key = "sample.key",
                Value = "v1",
                Scope = ConfigurationScope.Global,
                TenantId = null
            };

            var createCmd = new CreateConfigCommand(request, "corr-2");
            var created = await createHandler.Handle(createCmd);

            var query = new GetConfigQuery(created.Key, null, null, "corr-2");

            // Act
            var result = await getHandler.Handle(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(created.Key, result!.Key);
            Assert.Equal(created.Value, result.Value);
            Assert.Equal(created.Version, result.Version);
        }
    }
}
