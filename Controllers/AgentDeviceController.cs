using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Data;
using DeskGuardBackend.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DeskGuardBackend.Controllers
{
    [ApiController]
    [Route("api/v1/agent")]
    public class AgentDeviceController : ControllerBase
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<AgentDeviceController> _logger;

        public AgentDeviceController(DeskGuardDbContext dbContext, ILogger<AgentDeviceController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpPost("device-sync")]
        public async Task<IActionResult> SubmitDeviceSync([FromBody] JsonElement rawPayload)
        {
            try
            {
                var machineUid = ResolveMachineUid(rawPayload);
                if (string.IsNullOrEmpty(machineUid))
                    return UnprocessableEntity(ApiResponse.Fail("Machine identifier is required."));

                var machine = await _dbContext.Machines.FirstOrDefaultAsync(m => m.MachineUid == machineUid);
                if (machine == null)
                    return NotFound(ApiResponse.Fail("Machine not found. Send health payload first."));

                var devicesProp = rawPayload.GetPropertyOrNull("devices");
                if (devicesProp == null || devicesProp.Value.ValueKind != JsonValueKind.Array)
                    return BadRequest(ApiResponse.Fail("devices (array) is required."));

                var incoming = new List<MachineConnectedDevice>();
                foreach (var d in devicesProp.Value.EnumerateArray())
                {
                    incoming.Add(new MachineConnectedDevice
                    {
                        MachineId = machine.Id,
                        DeviceName = d.GetStringProperty("device_name") ?? d.GetStringProperty("deviceName"),
                        DeviceType = d.GetStringProperty("device_type") ?? d.GetStringProperty("deviceType"),
                        DeviceId = d.GetStringProperty("device_id") ?? d.GetStringProperty("deviceId"),
                        Manufacturer = d.GetStringProperty("manufacturer"),
                        ConnectionType = d.GetStringProperty("connection_type") ?? d.GetStringProperty("connectionType"),
                        Status = d.GetStringProperty("device_status") ?? d.GetStringProperty("status") ?? "connected",
                        DriverVersion = d.GetStringProperty("driver_version") ?? d.GetStringProperty("driverVersion"),
                        LastSeen = ParseDateTime(d.GetStringProperty("last_seen") ?? d.GetStringProperty("lastSeen")) ?? DateTime.UtcNow,
                        HasProblem = d.GetBooleanProperty("has_problem") ?? d.GetBooleanProperty("hasProblem"),
                        ProblemDescription = d.GetStringProperty("problem_description") ?? d.GetStringProperty("problemDescription"),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                var existing = await _dbContext.MachineConnectedDevices
                    .Where(d => d.MachineId == machine.Id)
                    .ToListAsync();
                _dbContext.MachineConnectedDevices.RemoveRange(existing);

                _dbContext.MachineConnectedDevices.AddRange(incoming);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Device sync saved for machine {MachineId} ({Count} devices)", machine.Id, incoming.Count);
                return Ok(ApiResponse.Ok($"Device sync saved ({incoming.Count} devices)."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process device sync");
                return StatusCode(500, ApiResponse.Fail("Failed to process device sync."));
            }
        }

        [HttpPost("device-events")]
        public async Task<IActionResult> SubmitDeviceEvent([FromBody] JsonElement rawPayload)
        {
            try
            {
                var machineUid = ResolveMachineUid(rawPayload);
                if (string.IsNullOrEmpty(machineUid))
                    return UnprocessableEntity(ApiResponse.Fail("Machine identifier is required."));

                var machine = await _dbContext.Machines.FirstOrDefaultAsync(m => m.MachineUid == machineUid);
                if (machine == null)
                    return NotFound(ApiResponse.Fail("Machine not found. Send health payload first."));

                var deviceEvent = new DeviceEvent
                {
                    MachineId = machine.Id,
                    DeviceName = rawPayload.GetStringProperty("device_name") ?? rawPayload.GetStringProperty("deviceName"),
                    DeviceType = rawPayload.GetStringProperty("device_type") ?? rawPayload.GetStringProperty("deviceType"),
                    DeviceId = rawPayload.GetStringProperty("device_id") ?? rawPayload.GetStringProperty("deviceId"),
                    EventType = rawPayload.GetStringProperty("event_type") ?? rawPayload.GetStringProperty("eventType"),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _dbContext.DeviceEvents.Add(deviceEvent);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Device event saved for machine {MachineId}: {EventType} - {DeviceName}",
                    machine.Id, deviceEvent.EventType, deviceEvent.DeviceName);
                return Ok(ApiResponse.Ok("Device event recorded."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process device event");
                return StatusCode(500, ApiResponse.Fail("Failed to process device event."));
            }
        }

        private static DateTime? ParseDateTime(string? value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            if (DateTime.TryParse(value, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt)) return dt;
            return null;
        }

        private string ResolveMachineUid(JsonElement payload)
        {
            var candidates = new[]
            {
                payload.GetStringProperty("machineId"),
                payload.GetStringProperty("machine_uid"),
                payload.GetStringProperty("machineUid"),
                payload.GetStringProperty("agentId"),
                Request.Headers["X-Agent-Id"].ToString()
            };

            foreach (var candidate in candidates)
            {
                if (!string.IsNullOrEmpty(candidate)) return candidate;
            }

            return string.Empty;
        }
    }
}
