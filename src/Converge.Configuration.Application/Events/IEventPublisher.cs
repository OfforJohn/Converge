using System.Threading.Tasks;

namespace Converge.Configuration.Application.Events
{
    public interface IEventPublisher
    {
        Task PublishAsync(string eventName, object payload, Guid correlationId);
    }
}
