
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Converge.Configuration.DTOs;
using Converge.Configuration.Application.Handlers;
using Converge.Configuration.Application.Handlers.Requests;

namespace Converge.Configuration.API.Controllers
{
    [ApiController]
    [Route("api/config")]
    public class ConfigController : ControllerBase
    {
        private readonly IRequestDispatcher _dispatcher;
        private readonly ILogger<ConfigController> _logger;

        public ConfigController(IRequestDispatcher dispatcher, ILogger<ConfigController> logger)
        {
            _dispatcher = dispatcher;
            _logger = logger;
        }

        [HttpGet("{key}")]
        [Authorize(Policy = "CanReadConfig")]
        public async Task<IActionResult> Get(string key, [FromQuery] Guid? tenantId, [FromQuery] int? version)
        {
            var correlationId = Request.Headers["X-Correlation-ID"].ToString();
            var query = new GetConfigQuery(key, tenantId, version, correlationId);
            var result = await _dispatcher.Send<GetConfigQuery, ConfigResponse?>(query);
            if (result == null)
                return NotFound(new { Code = "CONFIG_NOT_FOUND", Message = "Configuration not found." });

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "CanWriteConfig")]
        public async Task<IActionResult> Create([FromBody] CreateConfigRequest request)
        {
            var correlationId = Request.Headers["X-Correlation-ID"].ToString();
            var command = new CreateConfigCommand(request, correlationId);
            var created = await _dispatcher.Send<CreateConfigCommand, ConfigResponse>(command);
            return CreatedAtAction(nameof(Get), new { key = created.Key }, created);
        }

        [HttpPut("{key}")]
        [Authorize(Policy = "CanWriteConfig")]
        public async Task<IActionResult> Update(string key, [FromBody] UpdateConfigRequest request)
        {
            var correlationId = Request.Headers["X-Correlation-ID"].ToString();
            var command = new UpdateConfigCommand(key, request, correlationId);
            var updated = await _dispatcher.Send<UpdateConfigCommand, ConfigResponse?>(command);
            if (updated == null)
                return NotFound(new { Code = "CONFIG_NOT_FOUND", Message = "Configuration not found for update." });

            return Ok(updated);
        }

        [HttpPost("{key}/rollback")]
        [Authorize(Policy = "CanWriteConfig")]
        public async Task<IActionResult> Rollback(string key, [FromBody] RollbackRequest request)
        {
            var correlationId = Request.Headers["X-Correlation-ID"].ToString();
            var command = new RollbackConfigCommand(key, request.Version, request.TenantId, correlationId);
            var rolledBack = await _dispatcher.Send<RollbackConfigCommand, ConfigResponse?>(command);
            if (rolledBack == null)
                return NotFound(new { Code = "CONFIG_NOT_FOUND", Message = "Target version not found to rollback." });

            return Ok(rolledBack);
        }
    }
}
