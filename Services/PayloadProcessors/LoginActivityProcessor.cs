using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Extensions;

namespace DeskGuardBackend.Services.PayloadProcessors
{
    public class LoginActivityProcessor : IPayloadProcessor
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<LoginActivityProcessor> _logger;

        public LoginActivityProcessor(DeskGuardDbContext dbContext, ILogger<LoginActivityProcessor> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ProcessAsync(Machine machine, JsonElement payload, HealthLog healthLog)
        {
            try
            {
                var loginProp = payload.GetPropertyOrNull("loginActivity");
                if (loginProp == null || loginProp.Value.ValueKind != JsonValueKind.Array) return;

                foreach (var entry in loginProp.Value.EnumerateArray())
                {
                    var username = entry.GetStringProperty("userName") ?? entry.GetStringProperty("user_name");
                    var eventId = entry.GetInt32Property("eventId") ?? entry.GetInt32Property("event_id");

                    if (string.IsNullOrEmpty(username) && eventId == null)
                        continue;

                    var eventType = entry.GetStringProperty("eventType") ?? entry.GetStringProperty("event_type");
                    if (string.IsNullOrEmpty(eventType))
                    {
                        eventType = eventId switch
                        {
                            4624 => "Logon",
                            4625 => "Failed Logon",
                            4634 => "Logoff",
                            4647 => "Logoff",
                            _ => "Unknown"
                        };
                    }

                    var loginActivity = new LoginActivity
                    {
                        MachineId = machine.Id,
                        CompanyId = machine.CompanyId,
                        Username = username,
                        EventType = eventType,
                        IsSuccess = entry.GetBooleanProperty("isSuccess") ?? (eventId == 4624),
                        LogonType = entry.GetStringProperty("logonType") ?? entry.GetStringProperty("logon_type"),
                        SourceIp = entry.GetStringProperty("sourceIp") ?? entry.GetStringProperty("source_ip") ?? entry.GetStringProperty("ipAddress") ?? entry.GetStringProperty("ip_address"),
                        SessionId = entry.GetStringProperty("sessionId") ?? entry.GetStringProperty("session_id"),
                        EventTime = ParseDateTime(entry.GetStringProperty("timeGenerated") ?? entry.GetStringProperty("time_generated") ?? entry.GetStringProperty("eventTime") ?? entry.GetStringProperty("event_time")),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _dbContext.LoginActivities.AddAsync(loginActivity);
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogDebug("LoginActivityProcessor: Processed login activity for machine {MachineId}", machine.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoginActivityProcessor: Failed to process login activity for machine {MachineId}", machine.Id);
            }
        }

        private static DateTime? ParseDateTime(string? value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            if (DateTime.TryParse(value, out var dt))
            {
                return dt.Kind == DateTimeKind.Utc ? dt : dt.Kind == DateTimeKind.Local ? dt.ToUniversalTime() : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }
            return null;
        }
    }
}
