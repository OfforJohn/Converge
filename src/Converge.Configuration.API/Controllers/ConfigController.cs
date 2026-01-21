



using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Converge.Configuration.DTOs;
using Converge.Configuration.Application.Handlers;
using Converge.Configuration.Application.Handlers.Requests;
using Converge.Configuration.API.Services;

namespace Converge.Configuration.API.Controllers
{
    [ApiController]
    [Route("api/config")]
    public class ConfigController : ControllerBase
    {
        private readonly IRequestDispatcher _dispatcher;
        private readonly ILogger<ConfigController> _logger;
        private readonly ITokenScopeService _tokenScopeService;

        public ConfigController(IRequestDispatcher dispatcher, ILogger<ConfigController> logger, ITokenScopeService tokenScopeService)
        {
            _dispatcher = dispatcher;
            _logger = logger;
            _tokenScopeService = tokenScopeService;
        }

        private Guid GetCorrelationId()
        {
            var headerValue = Request.Headers["X-Correlation-ID"].ToString();
            return Guid.TryParse(headerValue, out var correlationId) ? correlationId : Guid.NewGuid();
        }

        [HttpGet("{key}")]
        [Authorize(Policy = "CanReadConfig")]
        public async Task<IActionResult> Get(string key, [FromQuery] Guid? tenantId, [FromQuery] Guid? companyId, [FromQuery] int? version)
        {
            var correlationId = GetCorrelationId();
            
            // Extract scope from Authorization header if not provided in query
            if (tenantId == null && companyId == null)
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                var tokenScope = _tokenScopeService.GetScopeFromToken(authHeader);
                tenantId = tokenScope.TenantId;
                companyId = tokenScope.CompanyId;
            }
            
            var query = new GetConfigQuery(key, tenantId, companyId, version, correlationId);
            var result = await _dispatcher.Send<GetConfigQuery, ConfigResponse?>(query);
            if (result == null)
                return NotFound(new { Code = "CONFIG_NOT_FOUND", Message = "Configuration not found." });

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "CanWriteConfig")]
        public async Task<IActionResult> Create([FromBody] CreateConfigRequest request)
        {
            var correlationId = GetCorrelationId();
            
            // Extract scope from Authorization header (Bearer token value)
            var authHeader = Request.Headers["Authorization"].ToString();
            var tokenScope = _tokenScopeService.GetScopeFromToken(authHeader);
            request.Scope = tokenScope.Scope;
            request.TenantId = tokenScope.TenantId;
            
            var command = new CreateConfigCommand(request, correlationId);
            var created = await _dispatcher.Send<CreateConfigCommand, ConfigResponse>(command);
            return CreatedAtAction(nameof(Get), new { key = created.Key }, created);
        }

        [HttpPut("{key}")]
        [Authorize(Policy = "CanWriteConfig")]
        public async Task<IActionResult> Update(string key, [FromBody] UpdateConfigRequest request)
        {
            var correlationId = GetCorrelationId();
            
            // Extract scope from Authorization header (Bearer token value)
            var authHeader = Request.Headers["Authorization"].ToString();
            var tokenScope = _tokenScopeService.GetScopeFromToken(authHeader);
            request.Scope = tokenScope.Scope;
            request.TenantId = tokenScope.TenantId;
            request.CompanyId = tokenScope.CompanyId;
            
            var command = new UpdateConfigCommand(key, request, correlationId);
            var updated = await _dispatcher.Send<UpdateConfigCommand, ConfigResponse?>(command);
            if (updated == null)
                return NotFound(new { Code = "CONFIG_NOT_FOUND", Message = "Configuration not found for update." });

            return Ok(updated);
        }
    }
}
