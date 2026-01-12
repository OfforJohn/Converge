using System.Threading.Tasks;
using Converge.Configuration.Application.Handlers.Requests;
using Converge.Configuration.DTOs;
using Converge.Configuration.Services;
using Converge.Configuration.Application.Services;
using Converge.Configuration.Application.Events;

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
            var before = null as object;

            // Do not trust tenant id supplied by client. If the config scope is Tenant,
            // generate a server-side TenantId for tracking. For Global scope, TenantId stays null.
            var incoming = request.Request;
            CreateConfigRequest toCreate;

            if (incoming.Scope == ConfigurationScope.Tenant)
            {
                toCreate = new CreateConfigRequest
                {
                    Key = incoming.Key,
                    Value = incoming.Value,
                    Scope = incoming.Scope,
                    // generate a new tenant id on server-side
                    TenantId = System.Guid.NewGuid()
                };
            }
            else
            {
                // For Global scope, keep TenantId null
                toCreate = new CreateConfigRequest
                {
                    Key = incoming.Key,
                    Value = incoming.Value,
                    Scope = incoming.Scope,
                    TenantId = null
                };
            }

            var created = await _service.CreateAsync(toCreate, request.CorrelationId);

            // Audit and publish
            await _audit.AuditAsync("Create", created.Key, before, created, null, created.TenantId, request.CorrelationId);
            await _publisher.PublishAsync("ConfigCreated", created, request.CorrelationId);

            return created;
        }
    }
}
