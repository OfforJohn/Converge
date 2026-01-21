using System;
using Converge.Configuration.DTOs;

namespace Converge.Configuration.Application.Handlers.Requests
{
    public class UpdateConfigCommand
    {
        public string Key { get; }
        public UpdateConfigRequest Request { get; }
        public Guid CorrelationId { get; }

        public UpdateConfigCommand(string key, UpdateConfigRequest request, Guid correlationId)
        {
            Key = key;
            Request = request;
            CorrelationId = correlationId;
        }
    }
}
