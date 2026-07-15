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
    public class AntivirusProcessor : IPayloadProcessor
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<AntivirusProcessor> _logger;

        public AntivirusProcessor(DeskGuardDbContext dbContext, ILogger<AntivirusProcessor> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ProcessAsync(Machine machine, JsonElement payload, HealthLog healthLog)
        {
            try
            {
                var avProp = payload.GetPropertyOrNull("antivirus");
                if (avProp?.ValueKind != JsonValueKind.Object) return;

                var av = avProp.Value;

                var displayName = av.GetStringProperty("displayName") ?? av.GetStringProperty("display_name") ?? "Unknown";
                var isEnabled = av.GetBooleanProperty("isRealTimeProtectionEnabled") ?? av.GetBooleanProperty("is_real_time_protection_enabled");
                var isUpdated = av.GetBooleanProperty("isSignatureUpToDate") ?? av.GetBooleanProperty("is_signature_up_to_date");
                var productVersion = av.GetStringProperty("productVersion") ?? av.GetStringProperty("product_version");

                // Upsert AntivirusStatus table
                var existing = await _dbContext.AntivirusStatuses
                    .FirstOrDefaultAsync(s => s.MachineId == machine.Id);

                if (existing == null)
                {
                    existing = new AntivirusStatus { MachineId = machine.Id };
                    await _dbContext.AntivirusStatuses.AddAsync(existing);
                }

                existing.DisplayName = displayName;
                existing.IsRealTimeProtectionEnabled = isEnabled;
                existing.IsSignatureUpToDate = isUpdated;
                existing.ProductVersion = productVersion;
                existing.UpdatedAt = DateTime.UtcNow;

                // Update MachineCurrentStatus security fields
                var currentStatus = await _dbContext.MachineCurrentStatuses
                    .FirstOrDefaultAsync(s => s.MachineId == machine.Id);

                if (currentStatus == null)
                {
                    currentStatus = new MachineCurrentStatus { MachineId = machine.Id };
                    await _dbContext.MachineCurrentStatuses.AddAsync(currentStatus);
                }

                currentStatus.AntivirusName = displayName;
                currentStatus.AntivirusEnabled = isEnabled;
                currentStatus.CollectedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                _logger.LogDebug("AntivirusProcessor: Processed antivirus status for machine {MachineId}: {Name}, Enabled={Enabled}",
                    machine.Id, displayName, isEnabled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AntivirusProcessor: Failed to process antivirus data for machine {MachineId}", machine.Id);
            }
        }
    }
}
