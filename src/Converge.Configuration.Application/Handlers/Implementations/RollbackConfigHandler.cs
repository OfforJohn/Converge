using System.Threading.Tasks;
using Converge.Configuration.Application.Handlers.Requests;
using Converge.Configuration.DTOs;
using Converge.Configuration.Services;

namespace Converge.Configuration.Application.Handlers.Implementations
{
    public class RollbackConfigHandler : IRequestHandler<RollbackConfigCommand, ConfigResponse?>
    {
        private readonly IConfigService _service;

        public RollbackConfigHandler(IConfigService service)
        {
            _service = service;
        }

        public Task<ConfigResponse?> Handle(RollbackConfigCommand request)
        {
            return _service.RollbackAsync(request.Key, request.Version, request.TenantId, request.CorrelationId);
        }
    }
}
