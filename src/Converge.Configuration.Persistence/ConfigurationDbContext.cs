using Microsoft.EntityFrameworkCore;
using Converge.Configuration.Persistence.Entities;

namespace Converge.Configuration.Persistence
{
    public class ConfigurationDbContext : DbContext
    {
        public ConfigurationDbContext(DbContextOptions<ConfigurationDbContext> options)
            : base(options)
        {
        }

        public DbSet<OutboxEvent> OutboxEvents { get; set; } = null!;
        public DbSet<ConfigurationItem> ConfigurationItems { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Map OutboxEvent explicitly to lowercase table
            modelBuilder.Entity<OutboxEvent>(entity =>
            {
                entity.ToTable("outboxevents", "public");

                entity.HasKey(e => e.Id);

                // Map inherited properties
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.CompanyId).HasColumnName("companyid");
                entity.Property(e => e.TenantId).HasColumnName("tenantid");
                entity.Property(e => e.CreatorId).HasColumnName("creatorid");
                entity.Property(e => e.CreatedAt).HasColumnName("createdat");
                entity.Property(e => e.UpdaterId).HasColumnName("updaterid");
                entity.Property(e => e.UpdatedAt).HasColumnName("updatedat");
                entity.Property(e => e.DeleterId).HasColumnName("deleterid");
                entity.Property(e => e.DeletedAt).HasColumnName("deletedat");
                entity.Property(e => e.Version).HasColumnName("version");
                entity.Property(e => e.ExternalRef).HasColumnName("externalref");
                entity.Property(e => e.ImportBatchId).HasColumnName("importbatchid");
                entity.Property(e => e.SourceSystem).HasColumnName("sourcesystem");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.EffectiveDate).HasColumnName("effectivedate");
                entity.Property(e => e.Notes).HasColumnName("notes");

                // Map OutboxEvent-specific properties
                entity.Property(e => e.EventType).HasColumnName("eventtype");
                entity.Property(e => e.Payload).HasColumnName("payload");
                entity.Property(e => e.CorrelationId).HasColumnName("correlationid");
                entity.Property(e => e.OccurredAt).HasColumnName("occurredat");
                entity.Property(e => e.Dispatched).HasColumnName("dispatched");
                entity.Property(e => e.DispatchedAt).HasColumnName("dispatchedat");
                entity.Property(e => e.Attempts).HasColumnName("attempts");

                entity.HasIndex(e => e.Dispatched);
                entity.HasIndex(e => e.OccurredAt);
            });

            modelBuilder.Entity<ConfigurationItem>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.HasIndex(c => new { c.Key, c.Scope, c.TenantId, c.Version });
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
