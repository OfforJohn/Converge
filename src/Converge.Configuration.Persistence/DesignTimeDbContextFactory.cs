using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Converge.Configuration.Persistence
{
    // Provides a way for EF tools to create the DbContext at design-time without depending on the API project's startup
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ConfigurationDbContext>
    {
        public ConfigurationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ConfigurationDbContext>();

            // Prefer environment variable CONVERGE_DB for CI/Dev overrides, otherwise fall back to local dev connection
            var connectionString = Environment.GetEnvironmentVariable("CONVERGE_DB")
                                   ?? "Host=localhost;Port=5432;Database=postgres;Username=myuser;Password=secret";

            optionsBuilder.UseNpgsql(connectionString);
            optionsBuilder.EnableSensitiveDataLogging();
            return new ConfigurationDbContext(optionsBuilder.Options);
        }
    }
}
