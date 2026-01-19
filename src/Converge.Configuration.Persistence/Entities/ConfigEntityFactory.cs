using System;

namespace Converge.Configuration.Persistence.Entities
{
    /// <summary>
    /// Factory for creating configuration entities with consistent data across all tables
    /// </summary>
    public static class ConfigEntityFactory
    {
        public static (ConfigurationItem configItem, CompanyConfigEvent companyEvent, OutboxEvent outboxEvent) CreateAllEntities(
            string key,
            string value,
            ConfigurationScope scope,
            Guid? tenantId,
            Guid? companyId,
            int version,
            string eventType,
            string correlationId,
            Guid? createdBy = null,
            Guid? domainId = null)
        {
            var now = DateTime.UtcNow;

            // 1️⃣ ConfigurationItem - main config storage
            var configItem = new ConfigurationItem
            {
                Id = Guid.NewGuid(),
                Key = key,
                Value = value,
                Scope = scope,
                TenantId = tenantId,
                CompanyId = companyId,
                Version = version,
                Status = "ACTIVE",
                CreatedBy = createdBy,
                CreatedAt = now,
                DomainId = domainId
            };

            // 2️⃣ CompanyConfigEvent - for company/tenant-specific event publishing
            var companyEvent = new CompanyConfigEvent
            {
                Id = Guid.NewGuid(),
                Key = key,
                Value = value,
                Scope = (int)scope,
                TenantId = tenantId,
                CompanyId = companyId,
                Version = version,
                DomainId = domainId,
                EventType = eventType,
                CorrelationId = correlationId,
                OccurredAt = now,
                Dispatched = false,
                Attempts = 0
            };

            // 3️⃣ OutboxEvent - for reliable event publishing (outbox pattern)
            // Inherits from BaseEntity which has non-nullable TenantId, CompanyId
            var outboxEvent = new OutboxEvent
            {
                Id = Guid.NewGuid(),
                Key = key,
                Value = value,
                Scope = (int)scope,
                TenantId = tenantId ?? Guid.Empty,
                CompanyId = companyId ?? Guid.Empty,
                Version = version,
                DomainId = domainId,
                EventType = eventType,
                CorrelationId = correlationId,
                OccurredAt = now,
                Dispatched = false
            };

            return (configItem, companyEvent, outboxEvent);
        }
    }
}
