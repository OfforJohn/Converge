using System.Threading.Tasks;
using Converge.Configuration.Application.Handlers.Requests;
using Converge.Configuration.DTOs;
using Converge.Configuration.Services;
using Converge.Configuration.Application.Services;
using Converge.Configuration.Application.Events;
using System.ComponentModel.DataAnnotations;

namespace Converge.Configuration.Application.Handlers.Implementations
{
    /// <summary>
    /// Command handler for creating configurations. Delegates to IConfigService for now.
    /// </summary>
    public class CreateConfigHandler : IRequestHandler<CreateConfigCommand, ConfigResponse>
    {
        private readonly IConfigService _service;
        private readonly IAuditService _audit;
        private readonly IEventPublisher _publisher;

        public CreateConfigHandler(IConfigService service, IAuditService audit, IEventPublisher publisher)
        {
            _service = service;
            _audit = audit;
            _publisher = publisher;
        }

        // Backwards-compatible constructor used by unit tests
        public CreateConfigHandler(IConfigService service)
            : this(service, new ConsoleAuditService(), new ConsoleEventPublisher())
        {
        }

        public async Task<ConfigResponse> Handle(CreateConfigCommand request)
        {
            // Validate the request
            request.Request.Validate();

            // Ensure TenantId is generated for Tenant and Company scopes
            if ((request.Request.Scope == ConfigurationScope.Tenant || request.Request.Scope == ConfigurationScope.Company) && request.Request.TenantId == null)
            {
                request.Request.TenantId = System.Guid.NewGuid();
            }

            var created = await _service.CreateAsync(request.Request, request.CorrelationId);

            // Audit and publish
            await _audit.AuditAsync("Create", created.Key, null, created, null, created.TenantId, request.CorrelationId);
            await _publisher.PublishAsync("ConfigCreated", created, request.CorrelationId);

            return created;
        }
    }
}
