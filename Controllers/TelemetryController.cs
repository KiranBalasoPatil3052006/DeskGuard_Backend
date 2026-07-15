using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace DeskGuardBackend.Controllers
{
    [ApiController]
    [Route("api/v1/agent/telemetry")]
    public class TelemetryController : ControllerBase
    {
        private readonly ITelemetryService _telemetryService;
        private readonly ILogger<TelemetryController> _logger;

        public TelemetryController(ITelemetryService telemetryService, ILogger<TelemetryController> logger)
        {
            _telemetryService = telemetryService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitTelemetry([FromBody] JsonElement rawPayload)
        {
            try
            {
                // Verify that machine is authenticated via middleware
                if (HttpContext.Items["Machine"] is not Machine machine)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, ApiResponse.Fail("Unauthorized."));
                }

                var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                await _telemetryService.ProcessTelemetryAsync(rawPayload, sourceIp);

                return Ok(ApiResponse.Ok("Telemetry data processed successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TelemetryController: Failed to submit telemetry");
                return StatusCode(500, ApiResponse.Fail("Failed to process telemetry data."));
            }
        }
    }
}
