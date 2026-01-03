using Converge.Configuration.DTOs;

namespace Converge.Configuration.Application.Handlers.Requests
{
    public class UpdateConfigCommand
    {
        public string Key { get; }
        public UpdateConfigRequest Request { get; }
        public string CorrelationId { get; }

        public UpdateConfigCommand(string key, UpdateConfigRequest request, string correlationId)
        {
            Key = key;
            Request = request;
            CorrelationId = correlationId;
        }
    }
}
