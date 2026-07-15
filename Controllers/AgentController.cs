using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DeskGuardBackend.DTOs.Machine;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace DeskGuardBackend.Controllers
{
    [ApiController]
    [Route("api/v1/agent")]
    public class AgentController : ControllerBase
    {
        private readonly IAgentRegistrationService _registrationService;
        private readonly IMachineService _machineService;
        private readonly ILogger<AgentController> _logger;

        public AgentController(
            IAgentRegistrationService registrationService,
            IMachineService machineService,
            ILogger<AgentController> logger)
        {
            _registrationService = registrationService;
            _machineService = machineService;
            _logger = logger;
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
    }
}
