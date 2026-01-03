using System.Threading.Tasks;
using Converge.Configuration.Application.Handlers.Requests;
using Converge.Configuration.DTOs;
using Converge.Configuration.Services;

namespace Converge.Configuration.Application.Handlers.Implementations
{
    public class UpdateConfigHandler : IRequestHandler<UpdateConfigCommand, ConfigResponse?>
    {
        private readonly IConfigService _service;

        public UpdateConfigHandler(IConfigService service)
        {
            _service = service;
        }

        public Task<ConfigResponse?> Handle(UpdateConfigCommand request)
        {
            return _service.UpdateAsync(request.Key, request.Request, request.CorrelationId);
        }
    }
}
