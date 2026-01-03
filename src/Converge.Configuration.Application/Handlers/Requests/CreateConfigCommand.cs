using Converge.Configuration.DTOs;

namespace Converge.Configuration.Application.Handlers.Requests
{
    /// <summary>
    /// Command to create a configuration value.
    /// </summary>
    public class CreateConfigCommand
    {
        public CreateConfigRequest Request { get; }
        public string CorrelationId { get; }

        public CreateConfigCommand(CreateConfigRequest request, string correlationId)
        {
            Request = request;
            CorrelationId = correlationId;
        }
    }
}
