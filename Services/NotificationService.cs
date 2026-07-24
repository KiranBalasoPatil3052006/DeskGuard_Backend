using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Services.Interfaces;
using DeskGuardBackend.Enums;
using DeskGuardBackend.SignalR;

namespace DeskGuardBackend.Services
{
    public class NotificationService : INotificationService
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly IAuditLogService _auditLogService;
        private readonly IHubContext<AlertHub> _hubContext;
        private readonly IEmailQueueService _emailQueueService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            DeskGuardDbContext dbContext,
            IAuditLogService auditLogService,
            IHubContext<AlertHub> hubContext,
            IEmailQueueService emailQueueService,
            ILogger<NotificationService> logger)
        {
            _dbContext = dbContext;
            _auditLogService = auditLogService;
            _hubContext = hubContext;
            _emailQueueService = emailQueueService;
            _logger = logger;
        }

        public async Task<Notification> SendNotificationAsync(long userId, string title, string message, string type, object? metadata = null)
        {
            try
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                {
                    throw new Exception($"User not found: {userId}");
                }

                var notification = new Notification
                {
                    CompanyId = user.CompanyId,
                    UserId = user.Id,
                    Title = title,
                    Message = message,
                    Type = type,
                    IsRead = false,
                    ReferenceType = metadata != null ? "json" : null
                };

                await _dbContext.Notifications.AddAsync(notification);
                await _dbContext.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    EventType.Create.ToString(),
                    $"Notification sent to user: {user.Email} - {title}",
                    user: user,
                    newValues: notification
                );

                _logger.LogInformation("Notification sent to user ID {UserId}: {Title}", userId, title);
                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationService::SendNotificationAsync failed for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> MarkAsReadAsync(long notificationId)
        {
            try
            {
                var notification = await _dbContext.Notifications.FindAsync(notificationId);
                if (notification == null) return false;

                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Notification {NotificationId} marked as read", notificationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationService::MarkAsReadAsync failed for notification: {NotificationId}", notificationId);
                throw;
            }
        }

        public async Task<bool> MarkAllAsReadAsync(long userId)
        {
            try
            {
                var unread = await _dbContext.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var n in unread)
                {
                    n.IsRead = true;
                    n.ReadAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("All notifications for user {UserId} marked as read. Count: {Count}", userId, unread.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationService::MarkAllAsReadAsync failed for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(long userId, bool unreadOnly = false)
        {
            try
            {
                var query = _dbContext.Notifications.Where(n => n.UserId == userId);

                if (unreadOnly)
                {
                    query = query.Where(n => !n.IsRead);
                }

                return await query
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationService::GetUserNotificationsAsync failed for user: {UserId}", userId);
                throw;
            }
        }

        public async Task SendAlertNotificationAsync(Alert alert)
        {
            try
            {
                // Reload machine to access assigned user
                var machine = await _dbContext.Machines
                    .Include(m => m.AssignedUser)
                    .FirstOrDefaultAsync(m => m.Id == alert.MachineId);

                if (machine == null)
                {
                    _logger.LogWarning("NotificationService::SendAlertNotificationAsync - No machine associated with alert ID {AlertId}", alert.Id);
                    return;
                }

                var assignedUser = machine.AssignedUser;
                if (assignedUser == null)
                {
                    _logger.LogInformation("NotificationService::SendAlertNotificationAsync - No user assigned to machine ID {MachineId}", machine.Id);
                    return;
                }

                await SendNotificationAsync(
                    assignedUser.Id,
                    alert.Title,
                    alert.Description ?? "An alert has been triggered for your machine.",
                    "alert",
                    new { alert_id = alert.Id, severity = alert.Severity, machine_id = alert.MachineId }
                );

                await BroadcastAlertEventAsync("alert_created", alert);

                _logger.LogInformation("Alert notification sent to assigned user ID {UserId}", assignedUser.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationService::SendAlertNotificationAsync failed for alert ID {AlertId}", alert.Id);
            }
        }

        public async Task SendEmailNotificationAsync(Alert alert)
        {
            try
            {
                var machine = await _dbContext.Machines.FindAsync(alert.MachineId);
                if (machine == null)
                {
                    _logger.LogWarning("NotificationService::SendEmailNotificationAsync - No machine for alert ID {AlertId}", alert.Id);
                    return;
                }

                var companyId = machine.CompanyId ?? alert.CompanyId;
                if (companyId == 0)
                {
                    _logger.LogWarning("NotificationService::SendEmailNotificationAsync - No company for machine ID {MachineId}", machine.Id);
                    return;
                }

                // Check notification rules for event type
                var alertType = alert.AlertType ?? "general";
                var rule = await _dbContext.NotificationRules
                    .FirstOrDefaultAsync(r => (r.CompanyId == companyId || r.CompanyId == null) && r.EventType == alertType);

                if (rule != null && !rule.SendEmail)
                {
                    _logger.LogInformation("NotificationService::SendEmailNotificationAsync - Email notifications disabled by rule for event type '{EventType}'", alertType);
                    return;
                }

                var recipients = await _dbContext.EmailRecipients
                    .Where(r => r.CompanyId == companyId && r.IsActive)
                    .ToListAsync();

                if (recipients.Count == 0)
                {
                    _logger.LogInformation("NotificationService::SendEmailNotificationAsync - No email recipients configured for company ID {CompanyId}", companyId);
                    return;
                }

                foreach (var recipient in recipients)
                {
                    _emailQueueService.QueueEmail(alert, recipient.Email);
                }

                _logger.LogInformation("Alert email work items enqueued for {Count} recipients (Alert ID: {AlertId})", recipients.Count, alert.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationService::SendEmailNotificationAsync failed for alert ID {AlertId}", alert.Id);
            }
        }

        public async Task BroadcastAlertEventAsync(string eventType, Alert alert)
        {
            try
            {
                var companyId = alert.CompanyId;
                if (companyId == 0)
                {
                    var machine = await _dbContext.Machines.FindAsync(alert.MachineId);
                    companyId = machine?.CompanyId ?? 0;
                }

                if (companyId == 0) return;

                var payload = new
                {
                    event_type = eventType,
                    alert = new
                    {
                        id = alert.Id,
                        machine_id = alert.MachineId,
                        severity = alert.Severity,
                        title = alert.Title,
                        description = alert.Description,
                        status = alert.Status,
                        created_at = alert.CreatedAt
                    },
                    timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.Group($"Company_{companyId}").SendAsync("AlertEvent", payload);
                _logger.LogDebug("SignalR broadcast AlertEvent '{EventType}' to Company_{CompanyId}", eventType, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationService::BroadcastAlertEventAsync failed for alert ID {AlertId}", alert.Id);
            }
        }

        public async Task BroadcastDashboardUpdateAsync(long companyId, string updateType, object? data = null)
        {
            try
            {
                var payload = new
                {
                    update_type = updateType,
                    data = data,
                    timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.Group($"Company_{companyId}").SendAsync("DashboardUpdate", payload);
                _logger.LogDebug("SignalR broadcast DashboardUpdate '{UpdateType}' to Company_{CompanyId}", updateType, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationService::BroadcastDashboardUpdateAsync failed for company: {CompanyId}", companyId);
            }
        }
    }
}
