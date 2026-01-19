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
            // Extract properties from payload if it's a known type
            var entry = new OutboxEvent
            {
                Id = Guid.NewGuid(),
                EventType = eventName,
                CorrelationId = correlationId,
                OccurredAt = DateTime.UtcNow,
                Dispatched = false
            };

            // Try to extract Key and Value from payload
            if (payload is ConfigurationItem config)
            {
                entry.Key = config.Key;
                entry.Value = config.Value;
                entry.Scope = (int)config.Scope;
                entry.TenantId = config.TenantId ?? Guid.Empty;
                entry.CompanyId = config.CompanyId ?? Guid.Empty;
                entry.Version = config.Version;
            }

            _db.OutboxEvents.Add(entry);
            await _db.SaveChangesAsync();
        }
    }
}
