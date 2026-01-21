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
            // Get current config for audit
            var before = await _service.GetEffectiveAsync(
                request.Key,
                request.TenantId,
                null,
                null,
                request.CorrelationId
            );

            // Perform rollback
            var result = await _service.RollbackAsync(
                request.Key,
                request.Version,
                request.TenantId,
                request.CorrelationId
            );

            // Audit (event is already created in DbConfigService)
            if (result != null)
            {
                await _audit.AuditAsync(
                    "Rollback",
                    request.Key,
                    before,
                    result,
                    null,
                    result.TenantId ?? result.CompanyId,
                    request.CorrelationId
                );
            }

            return result;
        }
    }
}
