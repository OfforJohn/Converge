using System.Text.Json;
using System.Threading.Tasks;
using Converge.Configuration.Persistence;
using Converge.Configuration.Persistence.Entities;
using System;

namespace Converge.Configuration.Application.Events
{
    // Publishes events by writing them to OutboxEvents table. A separate dispatcher will read and send to Kafka.
    public class OutboxEventPublisher : IEventPublisher
    {
        private readonly ConfigurationDbContext _db;

        public OutboxEventPublisher(ConfigurationDbContext db)
        {
            _db = db;
        }

        public async Task PublishAsync(string eventName, object payload, string correlationId)
        {
            var entry = new OutboxEvent
            {
                EventType = eventName,
                Payload = JsonSerializer.Serialize(payload),
                CorrelationId = correlationId,
                OccurredAt = DateTime.UtcNow,
                Dispatched = false,
                Attempts = 0
            };

            _db.OutboxEvents.Add(entry);
            await _db.SaveChangesAsync();
        }
    }
}
