using System;
using Converge.Configuration.DTOs;

namespace Converge.Configuration.Application.Handlers.Requests
{
    /// <summary>
    /// Command to create a configuration value.
    /// </summary>
    public class CreateConfigCommand
    {
        public CreateConfigRequest Request { get; }
        public Guid CorrelationId { get; }

        public CreateConfigCommand(CreateConfigRequest request, Guid correlationId)
        {
            Request = request;
            CorrelationId = correlationId;
        }
    }
}
