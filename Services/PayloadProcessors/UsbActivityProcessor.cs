using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Extensions;

namespace DeskGuardBackend.Services.PayloadProcessors
{
    public class UsbActivityProcessor : IPayloadProcessor
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<UsbActivityProcessor> _logger;

        public UsbActivityProcessor(DeskGuardDbContext dbContext, ILogger<UsbActivityProcessor> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ProcessAsync(Machine machine, JsonElement payload, HealthLog healthLog)
        {
            try
            {
                var usbProp = payload.GetPropertyOrNull("usbActivity");
                if (usbProp == null || usbProp.Value.ValueKind != JsonValueKind.Array) return;

                foreach (var entry in usbProp.Value.EnumerateArray())
                {
                    var logName = entry.GetStringProperty("logName") ?? entry.GetStringProperty("log_name");
                    var source = entry.GetStringProperty("source");
                    var message = entry.GetStringProperty("message");
                    var eventId = entry.GetInt32Property("eventId") ?? entry.GetInt32Property("event_id");

                    var deviceName = entry.GetStringProperty("deviceName") ?? entry.GetStringProperty("device_name");

                    // Accept either a traditional deviceName or an EventLogInfo-based entry (logName present)
                    if (string.IsNullOrEmpty(deviceName) && string.IsNullOrEmpty(logName))
                        continue;

                    var usbActivity = new UsbActivity
                    {
                        MachineId = machine.Id,
                        CompanyId = machine.CompanyId,
                        DeviceName = deviceName ?? logName ?? "Unknown Device",
                        DeviceSerial = entry.GetStringProperty("deviceSerial") ?? entry.GetStringProperty("device_serial") ?? entry.GetStringProperty("deviceId") ?? entry.GetStringProperty("device_id"),
                        EventType = entry.GetStringProperty("eventType") ?? entry.GetStringProperty("event_type") ?? source ?? (eventId.HasValue ? $"EventId:{eventId}" : null),
                        EventTime = ParseDateTime(entry.GetStringProperty("timeGenerated") ?? entry.GetStringProperty("time_generated") ?? entry.GetStringProperty("eventTime") ?? entry.GetStringProperty("event_time")),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _dbContext.UsbActivities.AddAsync(usbActivity);
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogDebug("UsbActivityProcessor: Processed USB activity for machine {MachineId}", machine.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UsbActivityProcessor: Failed to process USB activity for machine {MachineId}", machine.Id);
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
