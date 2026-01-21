using Converge.Configuration.DTOs;

namespace Converge.Configuration.Application.Services
{
    /// <summary>
    /// Provides the configuration scope from the current context (e.g., JWT token, HttpContext).
    /// The scope determines whether a configuration is Global, Tenant, or Company-specific.
    /// </summary>
    public interface IScopeContext
    {
        /// <summary>
        /// Gets the current scope from the authentication context/token.
        /// </summary>
        ConfigurationScope CurrentScope { get; }

        /// <summary>
        /// Gets the TenantId from the authentication context/token (if applicable).
        /// Returns null for Global scope.
        /// </summary>
        Guid? TenantId { get; }

        /// <summary>
        /// Gets the CompanyId from the authentication context/token (if applicable).
        /// Returns null for Global and Tenant scopes.
        /// </summary>
        Guid? CompanyId { get; }
    }
}
