using System.Security.Claims;
using Converge.Configuration.DTOs;
using Converge.Configuration.Application.Services;

namespace Converge.Configuration.API.Services
{
    /// <summary>
    /// Implementation of IScopeContext that reads scope from:
    /// 1. First, check custom headers (for development/testing with Postman)
    /// 2. Then, fall back to JWT token claims
    /// 
    /// Custom Headers (for Postman testing):
    /// - X-Config-Scope: "Global" | "Tenant" | "Company"
    /// - X-Tenant-Id: GUID
    /// - X-Company-Id: GUID
    /// 
    /// JWT Claims:
    /// - "scope" or "config_scope": "Global" | "Tenant" | "Company"
    /// - "tenant_id": GUID (for Tenant/Company scope)
    /// - "company_id": GUID (for Company scope)
    /// </summary>
    public class HttpScopeContext : IScopeContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpScopeContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public ConfigurationScope CurrentScope
        {
            get
            {
                // 1. Check custom header first (for Postman/development testing)
                var headerScope = GetHeaderValue("X-Config-Scope");
                if (!string.IsNullOrEmpty(headerScope))
                {
                    return ParseScope(headerScope);
                }

                // 2. Fall back to JWT claims
                var scopeClaim = GetClaimValue("scope") ?? GetClaimValue("config_scope");
                if (!string.IsNullOrEmpty(scopeClaim))
                {
                    return ParseScope(scopeClaim);
                }

                // Default to Global if no scope found
                return ConfigurationScope.Global;
            }
        }

        public Guid? TenantId
        {
            get
            {
                // 1. Check custom header first (for Postman/development testing)
                var headerTenantId = GetHeaderValue("X-Tenant-Id");
                if (!string.IsNullOrEmpty(headerTenantId) && Guid.TryParse(headerTenantId, out var tenantIdFromHeader))
                {
                    return tenantIdFromHeader;
                }

                // 2. Fall back to JWT claims
                var tenantIdClaim = GetClaimValue("tenant_id") ?? GetClaimValue("tenantid") ?? GetClaimValue("tid");
                if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var tenantId))
                {
                    return tenantId;
                }

                return null;
            }
        }

        public Guid? CompanyId
        {
            get
            {
                // 1. Check custom header first (for Postman/development testing)
                var headerCompanyId = GetHeaderValue("X-Company-Id");
                if (!string.IsNullOrEmpty(headerCompanyId) && Guid.TryParse(headerCompanyId, out var companyIdFromHeader))
                {
                    return companyIdFromHeader;
                }

                // 2. Fall back to JWT claims
                var companyIdClaim = GetClaimValue("company_id") ?? GetClaimValue("companyid") ?? GetClaimValue("cid");
                if (!string.IsNullOrEmpty(companyIdClaim) && Guid.TryParse(companyIdClaim, out var companyId))
                {
                    return companyId;
                }

                return null;
            }
        }

        private ConfigurationScope ParseScope(string scopeValue)
        {
            return scopeValue.ToLowerInvariant() switch
            {
                "global" => ConfigurationScope.Global,
                "tenant" => ConfigurationScope.Tenant,
                "company" => ConfigurationScope.Company,
                "0" => ConfigurationScope.Global,
                "1" => ConfigurationScope.Tenant,
                "2" => ConfigurationScope.Company,
                _ => ConfigurationScope.Global
            };
        }

        private string? GetHeaderValue(string headerName)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Request.Headers.TryGetValue(headerName, out var values) == true)
            {
                return values.FirstOrDefault();
            }
            return null;
        }

        private string? GetClaimValue(string claimType)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(claimType)?.Value 
                ?? user?.FindFirst(c => c.Type.Equals(claimType, StringComparison.OrdinalIgnoreCase))?.Value;
        }
    }
}
