using System.Threading.Tasks;
using Converge.Configuration.Application.Handlers.Requests;
using Converge.Configuration.DTOs;
using Converge.Configuration.Services;

namespace Converge.Configuration.Application.Handlers.Implementations
{
    /// <summary>
    /// Command handler for creating configurations. Delegates to IConfigService for now.
    /// </summary>
    public class CreateConfigHandler : IRequestHandler<CreateConfigCommand, ConfigResponse>
    {
        private readonly IConfigService _service;

        public CreateConfigHandler(IConfigService service)
        {
            _service = service;
        }

        public Task<ConfigResponse> Handle(CreateConfigCommand request)
        {
            return _service.CreateAsync(request.Request, request.CorrelationId);
        }
    }
}
