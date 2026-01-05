using System;
using Microsoft.EntityFrameworkCore;
using ConvergeErp.Configuration.Domain.Entities;
using Converge.Configuration.Domain.Enums;

namespace Converge.Configuration.Persistence
{
    /// <summary>
    /// EF Core DbContext for configuration entities.
    /// Keep mapping minimal — adjust column names/lengths as needed for your schema.
    /// </summary>
    public class ConfigurationDbContext : DbContext
    {
        public DbSet<Configuration> Configurations { get; set; } = null!;

        public ConfigurationDbContext(DbContextOptions<ConfigurationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var cfg = modelBuilder.Entity<Configuration>();

            cfg.ToTable("configurations");

            // Map properties that come from BaseEntity via shadow properties if necessary
            cfg.Property<string>("Key").IsRequired();
            cfg.Property<string>("Value").IsRequired();
            cfg.Property<int>("Version").IsRequired();
            cfg.Property<Guid>("TenantId");
            cfg.Property<int>("ConfigStatus").IsRequired();
            cfg.Property<int>("Scope").IsRequired();
            cfg.Property<DateTime>("CreatedAt").IsRequired();
            cfg.Property<DateTime?>("UpdatedAt").IsRequired(false);

            cfg.HasIndex("Key");
            cfg.HasIndex("TenantId");
            cfg.HasIndex("Version");
        }
    }
}
