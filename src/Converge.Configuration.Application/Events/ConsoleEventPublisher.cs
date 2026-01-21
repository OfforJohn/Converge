using System;
using System.Threading.Tasks;

namespace Converge.Configuration.Application.Events
{
    public class ConsoleEventPublisher : IEventPublisher
    {
        public ConsoleEventPublisher()
        {
        }

        public Task PublishAsync(string eventName, object payload, Guid correlationId)
        {
            Console.WriteLine($"EVENT {eventName} correlation={correlationId} payload={payload}");
            return Task.CompletedTask;
        }
    }
}
