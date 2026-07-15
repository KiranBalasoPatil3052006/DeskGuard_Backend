using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Extensions;

namespace DeskGuardBackend.Services.PayloadProcessors
{
    public class MachineProcessor : IPayloadProcessor
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<MachineProcessor> _logger;

        public MachineProcessor(DeskGuardDbContext dbContext, ILogger<MachineProcessor> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ProcessAsync(Machine machine, JsonElement payload, HealthLog healthLog)
        {
            try
            {
                var sysInfoProp = payload.GetPropertyOrNull("systemInfo");
                var systemInfo = sysInfoProp?.ValueKind == JsonValueKind.Object ? sysInfoProp.Value : default;

                var cpuProp = payload.GetPropertyOrNull("cpu");
                var cpu = cpuProp?.ValueKind == JsonValueKind.Object ? cpuProp.Value : default;

                var memoryProp = payload.GetPropertyOrNull("memory");
                var memory = memoryProp?.ValueKind == JsonValueKind.Object ? memoryProp.Value : default;

                var totalMemoryBytes = memory.ValueKind == JsonValueKind.Object 
                    ? (memory.GetInt64Property("totalMemoryBytes") ?? memory.GetInt64Property("total_memory_bytes")) 
                    : null;
                
                int? ramGb = totalMemoryBytes.HasValue 
                    ? (int?)Math.Round((double)totalMemoryBytes.Value / 1073741824, 0) 
                    : null;

                var computerName = payload.GetStringProperty("computerName") ?? systemInfo.GetStringProperty("computerName");

                // Update Machine properties
                machine.Hostname = computerName ?? machine.Hostname;
                machine.DeviceName = computerName ?? machine.DeviceName;
                machine.OperatingSystem = systemInfo.ValueKind == JsonValueKind.Object 
                    ? (systemInfo.GetStringProperty("operatingSystem") ?? systemInfo.GetStringProperty("operating_system") ?? machine.OperatingSystem)
                    : machine.OperatingSystem;
                machine.OsVersion = systemInfo.ValueKind == JsonValueKind.Object 
                    ? (systemInfo.GetStringProperty("osVersion") ?? systemInfo.GetStringProperty("os_version"))
                    : null;
                machine.Processor = cpu.ValueKind == JsonValueKind.Object
                    ? (cpu.GetStringProperty("processorName") ?? cpu.GetStringProperty("processor_name"))
                    : null;
                machine.RamGb = ramGb ?? machine.RamGb;
                machine.IsOnline = true;
                machine.LastHeartbeatAt = DateTime.UtcNow;

                // Update MachineCurrentStatus dates
                var currentStatus = await _dbContext.MachineCurrentStatuses
                    .FirstOrDefaultAsync(s => s.MachineId == machine.Id);

                if (currentStatus == null)
                {
                    currentStatus = new MachineCurrentStatus { MachineId = machine.Id };
                    await _dbContext.MachineCurrentStatuses.AddAsync(currentStatus);
                }

                currentStatus.CompanyId = machine.CompanyId;
                currentStatus.LastCollectedAt = DateTime.UtcNow;
                currentStatus.CollectedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("MachineProcessor: Machine {MachineId} ({MachineUid}) updated successfully", machine.Id, machine.MachineUid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MachineProcessor: Failed to update machine {MachineId}", machine.Id);
            }
        }
    }
}
