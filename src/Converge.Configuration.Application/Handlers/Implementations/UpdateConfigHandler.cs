using System;
using System.Threading.Tasks;
using Converge.Configuration.Application.Handlers.Requests;
using Converge.Configuration.DTOs;
using Converge.Configuration.Services;
using Converge.Configuration.Application.Services;
using Converge.Configuration.Application.Events;
using System.ComponentModel.DataAnnotations;

namespace Converge.Configuration.Application.Handlers.Implementations
{
    public class UpdateConfigHandler : IRequestHandler<UpdateConfigCommand, ConfigResponse?>
    {
        private readonly IConfigService _service;
        private readonly IAuditService _audit;
        private readonly IEventPublisher _publisher;

        public UpdateConfigHandler(IConfigService service, IAuditService audit, IEventPublisher publisher)
        {
            _service = service;
            _audit = audit;
            _publisher = publisher;
        }

        // Backwards-compatible ctor used by tests
        public UpdateConfigHandler(IConfigService service)
            : this(service, new ConsoleAuditService(), new ConsoleEventPublisher())
        {
        }

        public async Task<ConfigResponse?> Handle(UpdateConfigCommand request)
        {
            // Validate the request
            request.Request.Validate();

            // Ensure TenantId is generated for Tenant and Company scopes
            if ((request.Request.Scope == ConfigurationScope.Tenant || request.Request.Scope == ConfigurationScope.Company) && request.Request.TenantId == null)
            {
                request.Request.TenantId = Guid.NewGuid();
            }

            // Read current config for audit
            var before = await _service.GetEffectiveAsync(
                request.Key,
                request.Request.TenantId,
                null,
                request.CorrelationId
            );

            // Update config
            var updated = await _service.UpdateAsync(
                request.Key,
                request.Request,
                request.CorrelationId
            );

            // Audit and publish event
            if (updated != null)
            {
                await _audit.AuditAsync(
                    "Update",
                    request.Key,
                    before,
                    updated,
                    null,
                    updated.TenantId,
                    request.CorrelationId
                );

                await _publisher.PublishAsync("ConfigUpdated", updated, request.CorrelationId);
            }

            return updated;
        }
    }
}
