using System.Threading.Tasks;
using System;

namespace Converge.Configuration.Application.Events
{
    public class InMemoryEventPublisher : IEventPublisher
    {
        public InMemoryEventPublisher()
        {
        }

        public Task PublishAsync(string eventName, object payload, string correlationId)
        {
            Console.WriteLine($"EVENT {eventName} correlation={correlationId} payload={payload}");
            return Task.CompletedTask;
        }
    }
}
