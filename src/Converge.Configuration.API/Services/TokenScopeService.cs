using System;
using System.Linq;
using System.Security.Claims;
using Converge.Configuration.DTOs;

namespace Converge.Configuration.API.Services
{
    /// <summary>
    /// Service to extract scope from Bearer token
    /// The token value itself can be: "Tenant", "Company", or "Global"
    /// Or it can be a JWT with claims for scope, tenantId, companyId
    /// </summary>
    public interface ITokenScopeService
    {
        ScopeContext GetScopeFromToken(string authorizationHeader);
        ScopeContext GetScopeFromToken(ClaimsPrincipal user);
    }

    public class TokenScopeService : ITokenScopeService
    {
        /// <summary>
        /// Extracts scope from raw Authorization header
        /// Expected format: "Bearer {Tenant|Company|Global}"
        /// </summary>
        public ScopeContext GetScopeFromToken(string authorizationHeader)
        {
            if (string.IsNullOrWhiteSpace(authorizationHeader))
                return new ScopeContext { Scope = ConfigurationScope.Global };

            var parts = authorizationHeader.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return new ScopeContext { Scope = ConfigurationScope.Global };

            var token = parts[1];

            // Try to parse token as scope directly (e.g., "Tenant", "Company", "Global")
            if (Enum.TryParse<ConfigurationScope>(token, ignoreCase: true, out var scope))
            {
                return new ScopeContext { Scope = scope };
            }

            // If not a simple scope string, default to Global
            return new ScopeContext { Scope = ConfigurationScope.Global };
        }

        /// <summary>
        /// Extracts scope from ClaimsPrincipal (for JWT tokens with claims)
        /// Token can contain claims:
        /// - "scope": "Global" | "Tenant" | "Company"
        /// - "tenantId": Guid (optional, required for Tenant/Company scopes)
        /// - "companyId": Guid (optional, for Company scope)
        /// </summary>
        public ScopeContext GetScopeFromToken(ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
            {
                // Default to Global scope if no token
                return new ScopeContext { Scope = ConfigurationScope.Global };
            }

            var scopeClaim = user.FindFirst("scope")?.Value ?? user.FindFirst(ClaimTypes.Role)?.Value ?? "Global";
            var tenantIdClaim = user.FindFirst("tenantId")?.Value;
            var companyIdClaim = user.FindFirst("companyId")?.Value;

            var context = new ScopeContext();

            // Parse scope
            if (Enum.TryParse<ConfigurationScope>(scopeClaim, ignoreCase: true, out var parsedScope))
            {
                context.Scope = parsedScope;
            }
            else
            {
                context.Scope = ConfigurationScope.Global;
            }

            // Parse tenantId
            if (!string.IsNullOrWhiteSpace(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                context.TenantId = tenantId;
            }

            // Parse companyId
            if (!string.IsNullOrWhiteSpace(companyIdClaim) && Guid.TryParse(companyIdClaim, out var companyId))
            {
                context.CompanyId = companyId;
            }

            return context;
        }
    }

    /// <summary>
    /// Represents the scope context extracted from a token
    /// </summary>
    public class ScopeContext
    {
        public ConfigurationScope Scope { get; set; } = ConfigurationScope.Global;
        public Guid? TenantId { get; set; }
        public Guid? CompanyId { get; set; }
    }
}
