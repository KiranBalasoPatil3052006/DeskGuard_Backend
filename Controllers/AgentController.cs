using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DeskGuardBackend.DTOs.Machine;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using DeskGuardBackend.Data;

namespace DeskGuardBackend.Controllers
{
    [ApiController]
    [Route("api/v1/agent")]
    public class AgentController : ControllerBase
    {
        private readonly IAgentRegistrationService _registrationService;
        private readonly IMachineService _machineService;
        private readonly IAlertService _alertService;
        private readonly ILogger<AgentController> _logger;
        private readonly DeskGuardDbContext _dbContext;

        public AgentController(
            IAgentRegistrationService registrationService,
            IMachineService machineService,
            IAlertService alertService,
            ILogger<AgentController> logger,
            DeskGuardDbContext dbContext)
        {
            _registrationService = registrationService;
            _machineService = machineService;
            _alertService = alertService;
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] MachineRegistrationDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.MachineUid) || string.IsNullOrEmpty(dto.ActivationToken))
                {
                    return BadRequest(ApiResponse.Fail("Machine UID and activation token are required."));
                }

                var machine = await _registrationService.RegisterAsync(dto);

                // Replicate Laravel response structure containing plain-text token
                var responseData = new
                {
                    id = machine.Id,
                    machine_uid = machine.MachineUid,
                    api_token = machine.ApiToken
                };

                return Ok(ApiResponse<object>.Ok(responseData, "Machine registered successfully."));
            }
            catch (Exceptions.MachineRegistrationException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Machine registration failed for UID {Uid}", dto.MachineUid);
                return StatusCode(500, ApiResponse.Fail("Machine registration failed."));
            }
        }

        [HttpPost("uninstall")]
        [AllowAnonymous]
        public async Task<IActionResult> Uninstall([FromBody] System.Text.Json.JsonElement body)
        {
            try
            {
                string? reason = null;
                if (body.TryGetProperty("reason", out var reasonProp))
                {
                    reason = reasonProp.GetString();
                }

                string? machineUid = null;
                if (body.TryGetProperty("agentId", out var agentIdProp))
                {
                    machineUid = agentIdProp.GetString();
                }
                else if (HttpContext.Items["Machine"] is Machine machine)
                {
                    machineUid = machine.MachineUid;
                }

                if (string.IsNullOrEmpty(machineUid))
                {
                    return BadRequest(ApiResponse.Fail("Agent ID (machineUid) is required."));
                }

                var existingMachine = await _machineService.GetMachineByUidAsync(machineUid);
                await _alertService.CreateMachineUninstalledAlertAsync(existingMachine, reason);
                await _machineService.UninstallMachineAsync(machineUid, reason);

                return Ok(ApiResponse.Ok("Agent uninstalled successfully. The system administrator has been notified."));
            }
            catch (Exceptions.MachineNotFoundException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent uninstall failed");
                return StatusCode(500, ApiResponse.Fail("Failed to process uninstall."));
            }
        }

        [HttpPost("shutdown")]
        public async Task<IActionResult> Shutdown()
        {
            try
            {
                if (HttpContext.Items["Machine"] is not Machine machine)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, ApiResponse.Fail("Unauthorized."));
                }

                machine.IsOnline = false;
                machine.UpdatedAt = DateTime.UtcNow;
                using var scope = HttpContext.RequestServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DeskGuardBackend.Data.DeskGuardDbContext>();
                dbContext.Machines.Update(machine);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation("Agent shutdown detected: {MachineUid} | {Hostname}", machine.MachineUid, machine.Hostname);
                return Ok(ApiResponse.Ok("Shutdown acknowledged."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Shutdown handler failed");
                return StatusCode(500, ApiResponse.Fail("Shutdown processing failed."));
            }
        }

        [HttpPost("heartbeat")]
        public async Task<IActionResult> Heartbeat()
        {
            try
            {
                // Machine details attached by MachineAuthMiddleware
                if (HttpContext.Items["Machine"] is not Machine machine)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, ApiResponse.Fail("Unauthorized."));
                }

                await _machineService.UpdateHeartbeatAsync(machine.MachineUid);
                return Ok(ApiResponse.Ok("Heartbeat acknowledged."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Heartbeat update failed");
                return StatusCode(500, ApiResponse.Fail("Heartbeat failed."));
            }
        }

        [HttpPost("validate-key")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateApiKey([FromBody] System.Text.Json.JsonElement body)
        {
            try
            {
                string? apiKey = null;
                if (body.TryGetProperty("apiKey", out var keyProp))
                {
                    apiKey = keyProp.GetString();
                }

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return BadRequest(ApiResponse.Fail("API key is required."));
                }

                var hashedToken = HashToken(apiKey);
                var token = await _dbContext.MachineTokens
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Token == hashedToken && (t.ExpiresAt == null || t.ExpiresAt > DateTime.UtcNow));

                if (token == null)
                {
                    return Ok(ApiResponse<object>.Ok(new { valid = false, message = "Invalid or expired API key." }));
                }

                var machine = await _dbContext.Machines
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == token.MachineId);

                return Ok(ApiResponse<object>.Ok(new 
                { 
                    valid = true, 
                    message = "API key is valid.",
                    machineId = machine?.Id,
                    machineUid = machine?.MachineUid,
                    hostname = machine?.Hostname
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API key validation failed");
                return StatusCode(500, ApiResponse.Fail("API key validation failed."));
            }
        }

        private static string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
