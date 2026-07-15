using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Data;
using DeskGuardBackend.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DeskGuardBackend.Controllers
{
    [ApiController]
    [Route("api/v1/inventory")]
    public class InventoryController : ControllerBase
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(
            DeskGuardDbContext dbContext,
            IConfiguration configuration,
            ILogger<InventoryController> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("hardware")]
        public async Task<IActionResult> SubmitHardwareInventory([FromBody] JsonElement rawPayload)
        {
            try
            {
                var machineUid = ResolveMachineUid(rawPayload);
                if (string.IsNullOrEmpty(machineUid))
                    return UnprocessableEntity(ApiResponse.Fail("Machine identifier is required."));

                var machine = await _dbContext.Machines.FirstOrDefaultAsync(m => m.MachineUid == machineUid);
                if (machine == null)
                    return NotFound(ApiResponse.Fail("Machine not found. Send health payload first."));

                var items = rawPayload.GetPropertyOrNull("items");
                if (items == null || items.Value.ValueKind != JsonValueKind.Object)
                    return BadRequest(ApiResponse.Fail("items (object) is required."));

                var hw = items.Value;

                machine.Manufacturer = hw.GetStringProperty("manufacturer") ?? machine.Manufacturer;
                machine.Model = hw.GetStringProperty("model") ?? machine.Model;
                machine.SerialNumber = hw.GetStringProperty("serialNumber") ?? machine.SerialNumber;
                machine.BiosVersion = hw.GetStringProperty("biosVersion") ?? machine.BiosVersion;
                machine.Processor = hw.GetStringProperty("processorName") ?? machine.Processor;

                if (hw.TryGetProperty("processorCores", out var cores) && cores.ValueKind == JsonValueKind.Number)
                    machine.Processor = $"{machine.Processor} ({cores.GetInt32()} cores)";

                if (hw.TryGetProperty("totalMemoryBytes", out var mem) && mem.ValueKind == JsonValueKind.Number)
                    machine.RamGb = (int?)(mem.GetInt64() / (1024 * 1024 * 1024));

                var inventory = new HardwareInventory
                {
                    MachineId = machine.Id,
                    Manufacturer = machine.Manufacturer,
                    Model = machine.Model,
                    SerialNumber = machine.SerialNumber,
                    BiosVersion = machine.BiosVersion,
                    CpuModel = hw.GetStringProperty("processorName"),
                    CpuCores = hw.GetInt32Property("processorCores"),
                    CpuThreads = hw.GetInt32Property("processorLogicalThreads"),
                    TotalRamBytes = hw.GetInt64Property("totalMemoryBytes") ?? machine.RamGb * 1024L * 1024 * 1024,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _dbContext.HardwareInventories.Add(inventory);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Hardware inventory saved for machine {MachineId}", machine.Id);
                return Ok(ApiResponse.Ok("Hardware inventory saved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process hardware inventory");
                return StatusCode(500, ApiResponse.Fail("Failed to process hardware inventory."));
            }
        }

        [HttpPost("software")]
        public async Task<IActionResult> SubmitSoftwareInventory([FromBody] JsonElement rawPayload)
        {
            try
            {
                var machineUid = ResolveMachineUid(rawPayload);
                if (string.IsNullOrEmpty(machineUid))
                    return UnprocessableEntity(ApiResponse.Fail("Machine identifier is required."));

                var machine = await _dbContext.Machines.FirstOrDefaultAsync(m => m.MachineUid == machineUid);
                if (machine == null)
                    return NotFound(ApiResponse.Fail("Machine not found. Send health payload first."));

                var itemsProp = rawPayload.GetPropertyOrNull("items");
                if (itemsProp == null || itemsProp.Value.ValueKind != JsonValueKind.Array)
                    return BadRequest(ApiResponse.Fail("items (array) is required."));

                var incoming = new List<SoftwareInventory>();
                foreach (var item in itemsProp.Value.EnumerateArray())
                {
                    incoming.Add(new SoftwareInventory
                    {
                        MachineId = machine.Id,
                        Name = item.GetStringProperty("displayName") ?? item.GetStringProperty("name") ?? "Unknown",
                        Version = item.GetStringProperty("displayVersion") ?? item.GetStringProperty("version"),
                        Publisher = item.GetStringProperty("publisher"),
                        InstallDate = item.GetStringProperty("installDate"),
                        InstallLocation = item.GetStringProperty("installLocation"),
                        EstimatedSize = item.GetInt64Property("estimatedSizeMB") != null
                            ? (long?)(item.GetDecimalProperty("estimatedSizeMB") * 1024 * 1024)
                            : null,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                var existing = await _dbContext.SoftwareInventories
                    .Where(s => s.MachineId == machine.Id)
                    .ToListAsync();
                _dbContext.SoftwareInventories.RemoveRange(existing);

                _dbContext.SoftwareInventories.AddRange(incoming);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Software inventory saved for machine {MachineId} ({Count} items)", machine.Id, incoming.Count);
                return Ok(ApiResponse.Ok($"Software inventory saved ({incoming.Count} items)."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process software inventory");
                return StatusCode(500, ApiResponse.Fail("Failed to process software inventory."));
            }
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
