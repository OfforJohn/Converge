using System.Threading.Tasks;
using Converge.Configuration.Application.Handlers.Requests;
using Converge.Configuration.DTOs;
using Converge.Configuration.Services;

namespace Converge.Configuration.Application.Handlers.Implementations
{
    /// <summary>
    /// Read handler for GetConfigQuery - delegates to existing IConfigService.
    /// </summary>
    public class GetConfigHandler : IRequestHandler<GetConfigQuery, ConfigResponse?>
    {
        private readonly IConfigService _service;

        public GetConfigHandler(IConfigService service)
        {
            _service = service;
        }

        public Task<ConfigResponse?> Handle(GetConfigQuery request)
        {
            return _service.GetEffectiveAsync(request.Key, request.TenantId, request.CompanyId, request.Version, request.CorrelationId);
        }
    }
}
