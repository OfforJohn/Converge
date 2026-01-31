using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ConvergeERP.Shared.Abstractions;

namespace Converge.Configuration.API.Infrastructure
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId =>
            Guid.TryParse(
                _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                out var id)
                ? id
                : null;

        public Guid? TenantId =>
            Guid.TryParse(
                _httpContextAccessor.HttpContext?.User?.FindFirst("tenantId")?.Value,
                out var id)
                ? id
                : null;

        public Guid? CompanyId =>
            Guid.TryParse(
                _httpContextAccessor.HttpContext?.User?.FindFirst("companyId")?.Value,
                out var id)
                ? id
                : null;
    }
}
