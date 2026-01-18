using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Converge.Configuration.Persistence.Entities;
using DomainEntity = Converge.Configuration.Persistence.Entities.Domain;

namespace Converge.Configuration.Persistence
{
    public class ConfigurationDbContext : DbContext
    {
        private readonly IConfiguration? _configuration;

        public ConfigurationDbContext(DbContextOptions<ConfigurationDbContext> options, IConfiguration? configuration = null)
            : base(options)
        {
            _configuration = configuration;
        }

        public DbSet<OutboxEvent> OutboxEvents { get; set; } = null!;
        public DbSet<ConfigurationItem> ConfigurationItems { get; set; } = null!;
        public DbSet<CompanyConfigEvent> CompanyConfigEvents { get; set; } = null!;
        public DbSet<DomainEntity> Domains { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // OutboxEvent - for reliable event publishing (outbox pattern)
            modelBuilder.Entity<OutboxEvent>(entity =>
            {
                entity.ToTable("outboxevents", "public");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Key).HasColumnName("key").IsRequired();
                entity.Property(e => e.Value).HasColumnName("value").IsRequired();
                entity.Property(e => e.Scope).HasColumnName("scope");
                entity.Property(e => e.TenantId).HasColumnName("tenantid");
                entity.Property(e => e.CompanyId).HasColumnName("companyid");
                entity.Property(e => e.Version).HasColumnName("version");
                entity.Property(e => e.EventType).HasColumnName("eventtype").IsRequired();
                entity.Property(e => e.CorrelationId).HasColumnName("correlationid").IsRequired();
                entity.Property(e => e.OccurredAt).HasColumnName("occurredat");
                entity.Property(e => e.Dispatched).HasColumnName("dispatched");
                entity.Property(e => e.DispatchedAt).HasColumnName("dispatchedat");
                entity.Property(e => e.Attempts).HasColumnName("attempts");

                entity.HasIndex(e => e.Dispatched);
                entity.HasIndex(e => e.OccurredAt);
            });

            // ConfigurationItem - main config storage
            modelBuilder.Entity<ConfigurationItem>(entity =>
            {
                entity.ToTable("configurationitems", "public");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Key).HasColumnName("key").IsRequired();
                entity.Property(e => e.Value).HasColumnName("value").IsRequired();
                entity.Property(e => e.Scope).HasColumnName("scope");
                entity.Property(e => e.TenantId).HasColumnName("tenantid");
                entity.Property(e => e.CompanyId).HasColumnName("companyid");
                entity.Property(e => e.Version).HasColumnName("version");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.CreatedBy).HasColumnName("createdby");
                entity.Property(e => e.CreatedAt).HasColumnName("createdat");
                entity.Property(e => e.DomainId).HasColumnName("domainid");

                entity.HasIndex(e => new { e.Key, e.Scope, e.TenantId, e.Version });
                
                // Navigation to Domain
                entity.HasOne(e => e.Domain)
                    .WithMany()
                    .HasForeignKey(e => e.DomainId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // CompanyConfigEvent - for tenant/company-specific event publishing
            modelBuilder.Entity<CompanyConfigEvent>(entity =>
            {
                entity.ToTable("companyconfigevents", "public");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Key).HasColumnName("key").IsRequired();
                entity.Property(e => e.Value).HasColumnName("value").IsRequired();
                entity.Property(e => e.Scope).HasColumnName("scope");
                entity.Property(e => e.TenantId).HasColumnName("tenantid");
                entity.Property(e => e.CompanyId).HasColumnName("companyid");
                entity.Property(e => e.Version).HasColumnName("version");
                entity.Property(e => e.EventType).HasColumnName("eventtype").IsRequired();
                entity.Property(e => e.CorrelationId).HasColumnName("correlationid").IsRequired();
                entity.Property(e => e.OccurredAt).HasColumnName("occurredat");
                entity.Property(e => e.Dispatched).HasColumnName("dispatched");
                entity.Property(e => e.DispatchedAt).HasColumnName("dispatchedat");
                entity.Property(e => e.Attempts).HasColumnName("attempts");
            });

            // Domain - for namespacing config keys by domain/module
            modelBuilder.Entity<DomainEntity>(entity =>
            {
                entity.ToTable("domains", "public");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
