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
    public class DiskProcessor : IPayloadProcessor
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<DiskProcessor> _logger;

        public DiskProcessor(DeskGuardDbContext dbContext, ILogger<DiskProcessor> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ProcessAsync(Machine machine, JsonElement payload, HealthLog healthLog)
        {
            var disksProp = payload.GetPropertyOrNull("disks");
            if (disksProp == null || disksProp.Value.ValueKind != JsonValueKind.Array) return;

            var disks = disksProp.Value;

            decimal? aggregateUsage = null;
            long? aggregateFree = null;
            long? aggregateTotal = null;
            long? aggregateUsed = null;
            bool? aggregateHealth = null;

            // Pre-load existing disks for this machine to avoid N+1 read queries.
            var existingDisks = await _dbContext.MachineDisks
                .Where(d => d.MachineId == machine.Id)
                .ToListAsync();

            foreach (var disk in disks.EnumerateArray())
            {
                var driveLetter = disk.GetStringProperty("driveName") ?? disk.GetStringProperty("drive_letter") ?? disk.GetStringProperty("driveLetter") ?? "C:";
                var totalBytes = disk.GetInt64Property("totalSizeBytes") ?? disk.GetInt64Property("total_size_bytes");
                var freeBytes = disk.GetInt64Property("freeSpaceBytes") ?? disk.GetInt64Property("free_space_bytes");
                var usedBytes = disk.GetInt64Property("usedSpaceBytes") ?? disk.GetInt64Property("used_space_bytes");
                var usagePercent = disk.GetDecimalProperty("usagePercentage") ?? disk.GetDecimalProperty("usage_percentage");

                if (usagePercent == null && totalBytes.HasValue && totalBytes.Value > 0 && usedBytes.HasValue)
                {
                    usagePercent = Math.Round((decimal)usedBytes.Value / totalBytes.Value * 100, 2);
                }

                if (usagePercent.HasValue)
                {
                    aggregateUsage = aggregateUsage.HasValue ? Math.Max(aggregateUsage.Value, usagePercent.Value) : usagePercent.Value;
                }
                if (freeBytes.HasValue)
                {
                    aggregateFree = (aggregateFree ?? 0) + freeBytes.Value;
                }
                if (totalBytes.HasValue)
                {
                    aggregateTotal = (aggregateTotal ?? 0) + totalBytes.Value;
                }
                if (usedBytes.HasValue)
                {
                    aggregateUsed = (aggregateUsed ?? 0) + usedBytes.Value;
                }

                var driveType = disk.GetStringProperty("driveType") ?? disk.GetStringProperty("drive_type");
                var fileSystem = disk.GetStringProperty("fileSystem") ?? disk.GetStringProperty("file_system");
                var smartOk = disk.GetBooleanProperty("isSmartHealthOk") ?? disk.GetBooleanProperty("is_smart_health_ok");
                var healthStatus = smartOk.HasValue ? (smartOk.Value ? "Good" : "Bad") : "Unknown";
                var volumeLabel = disk.GetStringProperty("volumeLabel") ?? disk.GetStringProperty("volume_label");

                if (smartOk.HasValue)
                {
                    if (aggregateHealth == null) aggregateHealth = smartOk.Value;
                    else if (!smartOk.Value) aggregateHealth = false;
                }

                // In-memory lookup to avoid N+1 queries.
                var dbDisk = existingDisks.FirstOrDefault(d => d.DriveLetter == driveLetter);

                    if (dbDisk == null)
                    {
                        dbDisk = new MachineDisk { MachineId = machine.Id, DriveLetter = driveLetter };
                        await _dbContext.MachineDisks.AddAsync(dbDisk);
                    }

                    dbDisk.TotalGb = totalBytes.HasValue ? Math.Round((decimal)totalBytes.Value / 1073741824, 2) : null;
                    dbDisk.UsedGb = usedBytes.HasValue ? Math.Round((decimal)usedBytes.Value / 1073741824, 2) : null;
                    dbDisk.FreeGb = freeBytes.HasValue ? Math.Round((decimal)freeBytes.Value / 1073741824, 2) : null;
                    dbDisk.VolumeLabel = volumeLabel;
                    dbDisk.FileSystem = fileSystem;
                    dbDisk.DriveType = driveType;
                    dbDisk.HealthStatus = healthStatus;
                    dbDisk.UpdatedAt = DateTime.UtcNow;
                }

                var healthLabel = aggregateHealth == null ? null : (aggregateHealth.Value ? "Good" : "Bad");

                // Update MachineCurrentStatus
                var currentStatus = await _dbContext.MachineCurrentStatuses
                    .FirstOrDefaultAsync(s => s.MachineId == machine.Id);

                if (currentStatus == null)
                {
                    currentStatus = new MachineCurrentStatus { MachineId = machine.Id };
                    await _dbContext.MachineCurrentStatuses.AddAsync(currentStatus);
                }

                currentStatus.DiskPercentage = aggregateUsage;
                currentStatus.DiskFreeBytes = aggregateFree;
                currentStatus.DiskTotalBytes = aggregateTotal;
                currentStatus.DiskUsedBytes = aggregateUsed;
                currentStatus.DiskHealthStatus = healthLabel;
                currentStatus.CollectedAt = DateTime.UtcNow;

                // Update shared health log row
                healthLog.DiskPercentage = aggregateUsage;
                healthLog.DiskFreeBytes = aggregateFree;
                healthLog.DiskTotalBytes = aggregateTotal;

                await _dbContext.SaveChangesAsync();

                _logger.LogDebug("DiskProcessor: Processed disk telemetry for machine {MachineId}", machine.Id);
        }
    }
}
