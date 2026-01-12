using System.Threading.Tasks;
using Converge.Configuration.Application.Handlers.Requests;
using Converge.Configuration.DTOs;
using Converge.Configuration.Services;
using Converge.Configuration.Application.Services;
using Converge.Configuration.Application.Events;

namespace Converge.Configuration.Application.Handlers.Implementations
{
    public class RollbackConfigHandler : IRequestHandler<RollbackConfigCommand, ConfigResponse?>
    {
        private readonly IConfigService _service;
        private readonly IAuditService _audit;
        private readonly IEventPublisher _publisher;

        public RollbackConfigHandler(IConfigService service, IAuditService audit, IEventPublisher publisher)
        {
            _service = service;
            _audit = audit;
            _publisher = publisher;
        }

        // Backwards-compatible ctor used by tests
        public RollbackConfigHandler(IConfigService service)
            : this(service, new ConsoleAuditService(), new ConsoleEventPublisher())
        {
        }

        public async Task<ConfigResponse?> Handle(RollbackConfigCommand request)
        {
            var before = await _service.GetEffectiveAsync(request.Key, request.TenantId, null, request.CorrelationId);

            var rolled = await _service.RollbackAsync(request.Key, request.Version, request.TenantId, request.CorrelationId);

            if (rolled != null)
            {
                await _audit.AuditAsync("Rollback", request.Key, before, rolled, null, rolled.TenantId, request.CorrelationId);
                await _publisher.PublishAsync("ConfigRolledBack", rolled, request.CorrelationId);
            }

            return rolled;
        }
    }
}
