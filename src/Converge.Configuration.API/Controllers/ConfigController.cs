using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Converge.Configuration.DTOs;
using Converge.Configuration.Services;

namespace Converge.Configuration.API.Controllers
{
    [ApiController]
    [Route("api/config")]
    public class ConfigController : ControllerBase
    {
        private readonly IConfigService _configService;
        private readonly ILogger<ConfigController> _logger;

        public ConfigController(IConfigService configService, ILogger<ConfigController> logger)
        {
            _configService = configService;
            _logger = logger;
        }

        [HttpGet("{key}")]
        [Authorize(Policy = "CanReadConfig")]
        public async Task<IActionResult> Get(string key, [FromQuery] Guid? tenantId, [FromQuery] int? version)
        {
            var correlationId = Request.Headers["X-Correlation-ID"].ToString();
            var result = await _configService.GetEffectiveAsync(key, tenantId, version, correlationId);
            if (result == null)
                return NotFound(new { Code = "CONFIG_NOT_FOUND", Message = "Configuration not found." });

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "CanWriteConfig")]
        public async Task<IActionResult> Create([FromBody] CreateConfigRequest request)
        {
            var correlationId = Request.Headers["X-Correlation-ID"].ToString();
            var created = await _configService.CreateAsync(request, correlationId);
            return CreatedAtAction(nameof(Get), new { key = created.Key }, created);
        }

        [HttpPut("{key}")]
        [Authorize(Policy = "CanWriteConfig")]
        public async Task<IActionResult> Update(string key, [FromBody] UpdateConfigRequest request)
        {
            var correlationId = Request.Headers["X-Correlation-ID"].ToString();
            var updated = await _configService.UpdateAsync(key, request, correlationId);
            if (updated == null)
                return NotFound(new { Code = "CONFIG_NOT_FOUND", Message = "Configuration not found for update." });

            return Ok(updated);
        }

        [HttpPost("{key}/rollback")]
        [Authorize(Policy = "CanWriteConfig")]
        public async Task<IActionResult> Rollback(string key, [FromBody] RollbackRequest request)
        {
            var correlationId = Request.Headers["X-Correlation-ID"].ToString();
            var rolledBack = await _configService.RollbackAsync(key, request.Version, request.TenantId, correlationId);
            if (rolledBack == null)
                return NotFound(new { Code = "CONFIG_NOT_FOUND", Message = "Target version not found to rollback." });

            return Ok(rolledBack);
        }
    }
}
