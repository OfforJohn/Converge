using ConvergeERP.Shared.Domain;

public class OutboxEvent : BaseEntity
{
    public string EventType { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public string CorrelationId { get; set; } = null!;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public bool Dispatched { get; set; } = false;
    public DateTime? DispatchedAt { get; set; }
    public int Attempts { get; set; } = 0;

    // Inherited from BaseEntity / GlobalBaseEntity:
    // - Id, CompanyId, TenantId, CreatorId, CreatedAt, UpdaterId, UpdatedAt, DeleterId, DeletedAt, Version
    // - ExternalRef, ImportBatchId, SourceSystem, Status, EffectiveDate, Notes

    // Fix types here to match DB
    public Guid? ImportBatchId { get; set; }   // <- match uuid
    public string? ExternalRef { get; set; }   // <- match text
}
