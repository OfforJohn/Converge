
using Converge.Configuration.Persistence;
using Converge.Configuration.Persistence.Entities;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Converge.Configuration.Application.Events
{
    // Background service that reads OutboxEvents and publishes them to Kafka, marking them dispatched.
    public class KafkaDispatcher : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<KafkaDispatcher> _logger;
        private readonly ProducerConfig _producerConfig;
        private readonly string _topic;

        public KafkaDispatcher(IServiceScopeFactory scopeFactory, ILogger<KafkaDispatcher> logger, ProducerConfig producerConfig, IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _producerConfig = producerConfig;
            _topic = config["Kafka:Topic"] ?? "config-events";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var producer = new ProducerBuilder<string, string>(_producerConfig).Build();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

                    var pending = await db.OutboxEvents
                        .Where(e => !e.Dispatched)
                        .OrderBy(e => e.OccurredAt)
                        .Take(50)
                        .ToListAsync(stoppingToken);


                    foreach (var ev in pending)
                    {
                        try
                        {
                            // Serialize the event data to JSON for Kafka message
                            var payload = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                ev.Id,
                                ev.Key,
                                ev.Value,
                                ev.Scope,
                                ev.TenantId,
                                ev.CompanyId,
                                ev.DomainId,
                                ev.Version,
                                ev.EventType,
                                ev.CorrelationId,
                                ev.OccurredAt
                            });
                            var msg = new Message<string, string> { Key = ev.CorrelationId, Value = payload };
                            var res = await producer.ProduceAsync(_topic, msg, stoppingToken);

                            ev.Dispatched = true;

                            db.OutboxEvents.Update(ev);
                            await db.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation("Dispatched outbox event {EventId} to Kafka topic {Topic}", ev.Id, _topic);
                        }
                        catch (ProduceException<string, string> pex)
                        {
                            _logger.LogError(pex, "Kafka produce failed for outbox event {EventId}", ev.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while dispatching outbox events");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
