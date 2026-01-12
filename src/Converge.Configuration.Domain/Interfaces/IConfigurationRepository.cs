using ConvergeErp.Configuration.Domain.Entities;

public interface IConfigurationRepository
{
    // Get latest configuration for an exact scope
    // Global  -> tenantId = null, companyId = null
    // Tenant  -> tenantId = set,  companyId = null
    // Company -> tenantId = set,  companyId = set
    Task<Configuration?> GetLatestAsync(
        string key,
        Guid? tenantId,
        Guid? companyId
    );

    // Add a new configuration version
    Task AddAsync(Configuration configuration);

    // Check if a specific version exists within a scope
    Task<bool> ExistsVersionAsync(
        string key,
        Guid? tenantId,
        Guid? companyId,
        int version
    );

    // Get a specific configuration version
    Task<Configuration?> GetByKeyVersionAsync(
        string key,
        Guid? tenantId,
        Guid? companyId,
        int version
    );

    // Deprecate (soft-retire) a configuration version
    Task DeprecateAsync(Configuration configuration);

    // Get the maximum version for a key within a scope
    Task<int> GetMaxVersionAsync(
        string key,
        Guid? tenantId,
        Guid? companyId
    );
}
