using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Services.Interfaces;

namespace DeskGuardBackend.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(DeskGuardDbContext dbContext, ILogger<AuditLogService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task LogAsync(
            string eventType,
            string description,
            long? companyId = null,
            long? machineId = null,
            User? user = null,
            Machine? machine = null,
            object? oldValues = null,
            object? newValues = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EventType = eventType,
                    Description = description,
                    UserId = user?.Id,
                    MachineId = machineId ?? machine?.Id,
                    CompanyId = companyId ?? user?.CompanyId ?? machine?.CompanyId,
                    OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                    NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _dbContext.AuditLogs.AddAsync(auditLog);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("AuditLog added: {EventType} - {Description}", eventType, description);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log for event type {EventType}", eventType);
            }
        }
    }
}
