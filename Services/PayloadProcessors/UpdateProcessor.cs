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
    public class UpdateProcessor : IPayloadProcessor
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<UpdateProcessor> _logger;

        public UpdateProcessor(DeskGuardDbContext dbContext, ILogger<UpdateProcessor> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ProcessAsync(Machine machine, JsonElement payload, HealthLog healthLog)
        {
            try
            {
                var updatesProp = payload.GetPropertyOrNull("updates");
                if (updatesProp == null || updatesProp.Value.ValueKind != JsonValueKind.Object) return;

                var updates = updatesProp.Value;

                // Handle pending update count
                var pendingUpdateCount = updates.GetInt32Property("pendingUpdateCount") ?? updates.GetInt32Property("pending_update_count");
                var lastCheckedAt = ParseDateTime(updates.GetStringProperty("lastCheckedAt") ?? updates.GetStringProperty("last_checked_at") ?? updates.GetStringProperty("lastInstallationDate") ?? updates.GetStringProperty("last_installation_date") ?? updates.GetStringProperty("collectedAt") ?? updates.GetStringProperty("collected_at"));

                // Process pending updates array if present
                var pendingUpdatesProp = updates.GetPropertyOrNull("pendingUpdates") ?? updates.GetPropertyOrNull("pending_updates");
                if (pendingUpdatesProp.HasValue && pendingUpdatesProp.Value.ValueKind == JsonValueKind.Array)
                {
                    // Remove existing pending updates and re-insert
                    var existing = await _dbContext.WindowsUpdates
                        .Where(u => u.MachineId == machine.Id)
                        .ToListAsync();

                    _dbContext.WindowsUpdates.RemoveRange(existing);

                    foreach (var update in pendingUpdatesProp.Value.EnumerateArray())
                    {
                        var title = update.GetStringProperty("title");
                        if (string.IsNullOrEmpty(title)) continue;

                        var kbId = update.GetStringProperty("kbId") ?? update.GetStringProperty("kb_id") ?? update.GetStringProperty("KbArticleId") ?? update.GetStringProperty("kb_article_id");

                        var winUpdate = new WindowsUpdate
                        {
                            MachineId = machine.Id,
                            UpdateTitle = title,
                            KbArticleId = kbId,
                            IsInstalled = update.GetBooleanProperty("isInstalled") ?? update.GetBooleanProperty("is_installed") ?? update.GetBooleanProperty("isSecurity") ?? update.GetBooleanProperty("is_security") ?? false,
                            IsMandatory = update.GetBooleanProperty("isMandatory") ?? update.GetBooleanProperty("is_mandatory"),
                            Severity = update.GetStringProperty("severity"),
                            PendingUpdateCount = pendingUpdateCount,
                            LastCheckedAt = lastCheckedAt ?? DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        await _dbContext.WindowsUpdates.AddAsync(winUpdate);
                    }
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogDebug("UpdateProcessor: Processed updates for machine {MachineId}", machine.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateProcessor: Failed to process updates for machine {MachineId}", machine.Id);
            }
        }

        private static DateTime? ParseDateTime(string? value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            if (DateTime.TryParse(value, out var dt)) return dt;
            return null;
        }
    }
}
