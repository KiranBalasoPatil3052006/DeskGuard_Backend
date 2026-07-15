using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Extensions;

namespace DeskGuardBackend.Services.PayloadProcessors
{
    public class EventLogProcessor : IPayloadProcessor
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<EventLogProcessor> _logger;

        public EventLogProcessor(DeskGuardDbContext dbContext, ILogger<EventLogProcessor> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ProcessAsync(Machine machine, JsonElement payload, HealthLog healthLog)
        {
            try
            {
                var eventLogsProp = payload.GetPropertyOrNull("eventLogs");
                if (eventLogsProp == null || eventLogsProp.Value.ValueKind != JsonValueKind.Array) return;

                foreach (var entry in eventLogsProp.Value.EnumerateArray())
                {
                    var logName = entry.GetStringProperty("logName") ?? entry.GetStringProperty("log_name");
                    var source = entry.GetStringProperty("source");
                    var level = entry.GetStringProperty("level");
                    var message = entry.GetStringProperty("message");

                    // Skip empty log entries
                    if (string.IsNullOrEmpty(logName) && string.IsNullOrEmpty(source) && string.IsNullOrEmpty(message))
                        continue;

                    var eventLog = new EventLog
                    {
                        MachineId = machine.Id,
                        LogName = logName,
                        Source = source,
                        EventId = entry.GetInt32Property("eventId") ?? entry.GetInt32Property("event_id"),
                        Level = level,
                        Message = message,
                        TimeGenerated = ParseDateTime(entry.GetStringProperty("timeGenerated") ?? entry.GetStringProperty("time_generated")),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _dbContext.EventLogs.AddAsync(eventLog);
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogDebug("EventLogProcessor: Processed event log entries for machine {MachineId}", machine.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EventLogProcessor: Failed to process event logs for machine {MachineId}", machine.Id);
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
