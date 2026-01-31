namespace ConvergeERP.Shared.Abstractions;

public interface ICurrentUser
{
    Guid? TenantId { get; }
    Guid? CompanyId { get; }
    Guid? UserId { get; }
}
