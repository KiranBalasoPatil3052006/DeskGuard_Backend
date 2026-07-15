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
    public class BatteryProcessor : IPayloadProcessor
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<BatteryProcessor> _logger;

        public BatteryProcessor(DeskGuardDbContext dbContext, ILogger<BatteryProcessor> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ProcessAsync(Machine machine, JsonElement payload, HealthLog healthLog)
        {
            try
            {
                var batteryProp = payload.GetPropertyOrNull("battery");
                if (batteryProp == null) return;

                var battery = batteryProp.Value;

                var percentage = battery.GetDecimalProperty("batteryPercentage") ?? battery.GetDecimalProperty("battery_percentage");
                var isCharging = battery.GetBooleanProperty("isCharging") ?? battery.GetBooleanProperty("is_charging");
                var wearLevel = battery.GetDecimalProperty("wearLevelPercentage") ?? battery.GetDecimalProperty("wear_level_percentage");
                var isPresent = battery.GetBooleanProperty("isBatteryPresent") ?? battery.GetBooleanProperty("is_battery_present");
                var designCap = battery.GetInt64Property("designCapacity") ?? battery.GetInt64Property("design_capacity");
                var fullCharge = battery.GetInt64Property("fullChargeCapacity") ?? battery.GetInt64Property("full_charge_capacity");

                var currentStatus = await _dbContext.MachineCurrentStatuses
                    .FirstOrDefaultAsync(s => s.MachineId == machine.Id);

                if (currentStatus == null)
                {
                    currentStatus = new MachineCurrentStatus { MachineId = machine.Id };
                    await _dbContext.MachineCurrentStatuses.AddAsync(currentStatus);
                }

                currentStatus.BatteryPercentage = percentage;
                currentStatus.BatteryChargingStatus = isCharging;
                currentStatus.BatteryWearLevel = wearLevel;
                currentStatus.BatteryIsPresent = isPresent;
                currentStatus.BatteryDesignCapacity = designCap;
                currentStatus.BatteryFullChargeCapacity = fullCharge;
                currentStatus.CollectedAt = DateTime.UtcNow;

                // Update shared health log row
                healthLog.BatteryPercentage = percentage;
                healthLog.BatteryChargingStatus = isCharging;

                await _dbContext.SaveChangesAsync();

                _logger.LogDebug("BatteryProcessor: Processed Battery metrics for machine {MachineId}", machine.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BatteryProcessor: Failed to process Battery metrics for machine {MachineId}", machine.Id);
            }
        }
    }
}
