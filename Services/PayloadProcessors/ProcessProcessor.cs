using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Extensions;

namespace DeskGuardBackend.Services.PayloadProcessors
{
    public class ProcessProcessor : IPayloadProcessor
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<ProcessProcessor> _logger;

        public ProcessProcessor(DeskGuardDbContext dbContext, ILogger<ProcessProcessor> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ProcessAsync(Machine machine, JsonElement payload, HealthLog healthLog)
        {
            try
            {
                var processesProp = payload.GetPropertyOrNull("processes");
                if (processesProp == null || processesProp.Value.ValueKind != JsonValueKind.Array) return;

                // Remove old process logs for this machine and insert fresh batch
                var existing = await _dbContext.ProcessLogs
                    .Where(p => p.MachineId == machine.Id)
                    .ToListAsync();

                _dbContext.ProcessLogs.RemoveRange(existing);

                foreach (var proc in processesProp.Value.EnumerateArray())
                {
                    var processName = proc.GetStringProperty("processName") ?? proc.GetStringProperty("process_name");
                    if (string.IsNullOrEmpty(processName)) continue;

                    var cpuUsage = proc.GetDecimalProperty("cpuUsagePercentage") ?? proc.GetDecimalProperty("cpu_usage_percentage") ?? proc.GetDecimalProperty("cpuUsage") ?? proc.GetDecimalProperty("cpu_usage");
                    var workingSet = proc.GetInt64Property("workingSetBytes") ?? proc.GetInt64Property("working_set_bytes") ?? proc.GetInt64Property("memoryUsage") ?? proc.GetInt64Property("memory_usage");
                    var memoryMb = proc.GetDecimalProperty("memoryUsageMb") ?? proc.GetDecimalProperty("memory_usage_mb");

                    var processLog = new ProcessLog
                    {
                        MachineId = machine.Id,
                        ProcessName = processName,
                        ProcessId = proc.GetInt32Property("processId") ?? proc.GetInt32Property("process_id"),
                        CpuUsagePercentage = cpuUsage,
                        WorkingSetBytes = workingSet,
                        MemoryUsageMb = memoryMb,
                        ThreadCount = proc.GetInt32Property("threadCount") ?? proc.GetInt32Property("thread_count"),
                        UserName = proc.GetStringProperty("userName") ?? proc.GetStringProperty("user_name"),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _dbContext.ProcessLogs.AddAsync(processLog);
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogDebug("ProcessProcessor: Processed {Count} processes for machine {MachineId}", processesProp.Value.GetArrayLength(), machine.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessProcessor: Failed to process processes for machine {MachineId}", machine.Id);
            }
        }
    }
}
