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
    public class MemoryProcessor : IPayloadProcessor
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<MemoryProcessor> _logger;

        public MemoryProcessor(DeskGuardDbContext dbContext, ILogger<MemoryProcessor> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ProcessAsync(Machine machine, JsonElement payload, HealthLog healthLog)
        {
            try
            {
                var memProp = payload.GetPropertyOrNull("memory");
                if (memProp == null) return;

                var memory = memProp.Value;

                var totalBytes = memory.GetInt64Property("totalMemoryBytes") ?? memory.GetInt64Property("total_memory_bytes");
                var usedBytes = memory.GetInt64Property("usedMemoryBytes") ?? memory.GetInt64Property("used_memory_bytes");
                var availableBytes = memory.GetInt64Property("availableMemoryBytes") ?? memory.GetInt64Property("available_memory_bytes");
                var usagePercentage = memory.GetDecimalProperty("usagePercentage") ?? memory.GetDecimalProperty("usage_percentage");

                if (usagePercentage == null && totalBytes.HasValue && totalBytes.Value > 0 && usedBytes.HasValue)
                {
                    usagePercentage = Math.Round((decimal)usedBytes.Value / totalBytes.Value * 100, 2);
                }

                var currentStatus = await _dbContext.MachineCurrentStatuses
                    .FirstOrDefaultAsync(s => s.MachineId == machine.Id);

                if (currentStatus == null)
                {
                    currentStatus = new MachineCurrentStatus { MachineId = machine.Id };
                    await _dbContext.MachineCurrentStatuses.AddAsync(currentStatus);
                }

                currentStatus.CompanyId = machine.CompanyId;
                currentStatus.RamPercentage = usagePercentage;
                currentStatus.RamUsedBytes = usedBytes;
                currentStatus.RamAvailableBytes = availableBytes;
                currentStatus.RamTotalBytes = totalBytes;
                currentStatus.CollectedAt = DateTime.UtcNow;

                // Update shared health log row
                healthLog.RamPercentage = usagePercentage;
                healthLog.RamUsedBytes = usedBytes;
                healthLog.RamAvailableBytes = availableBytes;
                healthLog.RamTotalBytes = totalBytes;

                await _dbContext.SaveChangesAsync();

                _logger.LogDebug("MemoryProcessor: Processed Memory metrics for machine {MachineId}", machine.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MemoryProcessor: Failed to process Memory metrics for machine {MachineId}", machine.Id);
            }
        }
    }
}
