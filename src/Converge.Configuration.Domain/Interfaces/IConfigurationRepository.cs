using ConvergeErp.Configuration.Domain.Entities;

public interface IConfigurationRepository
{
    Task<Configuration?> GetLatestAsync(string key, Guid? tenantId);
    Task AddAsync(Configuration configuration);
    Task<bool> ExistsVersionAsync(string key, Guid? tenantId, int version);
    Task<Configuration?> GetByKeyVersionAsync(string key, Guid? tenantId, int version);
    Task DeprecateAsync(Configuration configuration);
    Task<int> GetMaxVersionAsync(string key);
}
