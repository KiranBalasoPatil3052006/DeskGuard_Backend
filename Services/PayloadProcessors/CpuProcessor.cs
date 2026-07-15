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
    public class CpuProcessor : IPayloadProcessor
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<CpuProcessor> _logger;

        public CpuProcessor(DeskGuardDbContext dbContext, ILogger<CpuProcessor> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ProcessAsync(Machine machine, JsonElement payload, HealthLog healthLog)
        {
            try
            {
                var cpuProp = payload.GetPropertyOrNull("cpu");
                if (cpuProp == null) return;

                var cpu = cpuProp.Value;

                var usagePercentage = cpu.GetDecimalProperty("usagePercentage") ?? cpu.GetDecimalProperty("usage_percentage");
                var temperature = cpu.GetDecimalProperty("temperatureCelsius") ?? cpu.GetDecimalProperty("temperature_celsius");
                var clockSpeed = cpu.GetDecimalProperty("currentClockSpeedMHz") ?? cpu.GetDecimalProperty("current_clock_speed_mhz");
                var coreCount = cpu.GetInt32Property("numberOfLogicalProcessors") ?? cpu.GetInt32Property("number_of_logical_processors");

                // Update current status
                var currentStatus = await _dbContext.MachineCurrentStatuses
                    .FirstOrDefaultAsync(s => s.MachineId == machine.Id);

                if (currentStatus == null)
                {
                    currentStatus = new MachineCurrentStatus { MachineId = machine.Id };
                    await _dbContext.MachineCurrentStatuses.AddAsync(currentStatus);
                }

                currentStatus.CompanyId = machine.CompanyId;
                currentStatus.CpuPercentage = usagePercentage;
                currentStatus.CpuTemperature = temperature;
                currentStatus.CpuClockSpeed = clockSpeed;
                currentStatus.CpuCoreCount = coreCount;
                currentStatus.CollectedAt = DateTime.UtcNow;

                // Update shared health log row
                healthLog.CpuPercentage = usagePercentage;
                healthLog.CpuTemperature = temperature;
                healthLog.CpuClockSpeed = clockSpeed;

                await _dbContext.SaveChangesAsync();

                _logger.LogDebug("CpuProcessor: Processed CPU metrics for machine {MachineId}", machine.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CpuProcessor: Failed to process CPU metrics for machine {MachineId}", machine.Id);
            }
        }
    }
}
