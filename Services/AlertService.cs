using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Exceptions;
using DeskGuardBackend.Services.Interfaces;
using DeskGuardBackend.Enums;

namespace DeskGuardBackend.Services
{
    public class AlertService : IAlertService
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;
        private readonly IAlertProfileService _alertProfileService;
        private readonly ILogger<AlertService> _logger;

        public AlertService(
            DeskGuardDbContext dbContext,
            INotificationService notificationService,
            IAuditLogService auditLogService,
            IAlertProfileService alertProfileService,
            ILogger<AlertService> logger)
        {
            _dbContext = dbContext;
            _notificationService = notificationService;
            _auditLogService = auditLogService;
            _alertProfileService = alertProfileService;
            _logger = logger;
        }

        public async Task EvaluateMachineAlertsAsync(Machine machine, MachineCurrentStatus status)
        {
            try
            {
                // Resolve effective thresholds from profile chain
                var thresholds = await _alertProfileService.ResolveThresholdsForMachineAsync(machine.Id);
                if (thresholds == null) return;

                var newAlerts = new List<Alert>();

                // ── Performance Thresholds ──
                // CPU
                if (thresholds.CpuWarningPercent.HasValue && status.CpuPercentage >= thresholds.CpuWarningPercent.Value)
                    newAlerts.Add(CreateAlert(machine, "warning", "High CPU Usage", $"CPU usage is {status.CpuPercentage}% (warning threshold: {thresholds.CpuWarningPercent}%).", "cpu_warning"));
                if (thresholds.CpuCriticalPercent.HasValue && status.CpuPercentage >= thresholds.CpuCriticalPercent.Value)
                    newAlerts.Add(CreateAlert(machine, "critical", "Critical CPU Usage", $"CPU usage is {status.CpuPercentage}% (critical threshold: {thresholds.CpuCriticalPercent}%).", "cpu_critical"));

                // RAM
                if (thresholds.RamWarningPercent.HasValue && status.RamPercentage >= thresholds.RamWarningPercent.Value)
                    newAlerts.Add(CreateAlert(machine, "warning", "High Memory Usage", $"Memory usage is {status.RamPercentage}% (warning threshold: {thresholds.RamWarningPercent}%).", "ram_warning"));
                if (thresholds.RamCriticalPercent.HasValue && status.RamPercentage >= thresholds.RamCriticalPercent.Value)
                    newAlerts.Add(CreateAlert(machine, "critical", "Critical Memory Usage", $"Memory usage is {status.RamPercentage}% (critical threshold: {thresholds.RamCriticalPercent}%).", "ram_critical"));

                // CPU Temperature
                if (thresholds.CpuTempWarning.HasValue && status.CpuTemperature >= thresholds.CpuTempWarning.Value)
                    newAlerts.Add(CreateAlert(machine, "warning", "High CPU Temperature", $"CPU temperature is {status.CpuTemperature}°C (warning threshold: {thresholds.CpuTempWarning}°C).", "cpu_temp_warning"));
                if (thresholds.CpuTempCritical.HasValue && status.CpuTemperature >= thresholds.CpuTempCritical.Value)
                    newAlerts.Add(CreateAlert(machine, "critical", "Critical CPU Temperature", $"CPU temperature is {status.CpuTemperature}°C (critical threshold: {thresholds.CpuTempCritical}°C).", "cpu_temp_critical"));

                // ── Storage Thresholds ──
                if (thresholds.DiskWarningPercent.HasValue && status.DiskPercentage >= thresholds.DiskWarningPercent.Value)
                    newAlerts.Add(CreateAlert(machine, "warning", "High Disk Usage", $"Disk usage is {status.DiskPercentage}% (warning threshold: {thresholds.DiskWarningPercent}%).", "disk_warning"));
                if (thresholds.DiskCriticalPercent.HasValue && status.DiskPercentage >= thresholds.DiskCriticalPercent.Value)
                    newAlerts.Add(CreateAlert(machine, "critical", "Critical Disk Usage", $"Disk usage is {status.DiskPercentage}% (critical threshold: {thresholds.DiskCriticalPercent}%).", "disk_critical"));

                // Deduplicate: skip if an open/acknowledged alert already exists for the same metric
                var existingAlerts = await _dbContext.Alerts
                    .Where(a => a.MachineId == machine.Id && (a.Status == "open" || a.Status == "acknowledged"))
                    .Select(a => a.Metadata)
                    .ToListAsync();

                var existingMetrics = new HashSet<string>();
                foreach (var meta in existingAlerts)
                {
                    if (string.IsNullOrEmpty(meta)) continue;
                    try
                    {
                        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(meta);
                        if (parsed != null && parsed.TryGetValue("metric", out var metric) && !string.IsNullOrEmpty(metric))
                            existingMetrics.Add(metric);
                    }
                    catch { }
                }

                var savedAlerts = new List<Alert>();
                foreach (var alert in newAlerts)
                {
                    var metricKey = ExtractMetricKey(alert.Metadata);
                    if (!string.IsNullOrEmpty(metricKey) && existingMetrics.Contains(metricKey))
                        continue;

                    await _dbContext.Alerts.AddAsync(alert);
                    savedAlerts.Add(alert);
                }

                if (savedAlerts.Count > 0)
                {
                    await _dbContext.SaveChangesAsync();

                    foreach (var alert in savedAlerts)
                    {
                        await _auditLogService.LogAsync(
                            EventType.Create.ToString(),
                            $"Alert generated: {alert.Title} for machine: {machine.MachineUid}",
                            machine: machine,
                            newValues: alert
                        );

                        await _notificationService.SendAlertNotificationAsync(alert);

                        if (alert.Severity == "critical" || alert.Severity == "warning")
                        {
                            await _notificationService.SendEmailNotificationAsync(alert);
                        }
                    }

                    _logger.LogInformation("AlertService::EvaluateMachineAlertsAsync - Created {Count} alert(s) for machine {MachineId} using profile thresholds", savedAlerts.Count, machine.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AlertService::EvaluateMachineAlertsAsync - Failed to evaluate alerts for machine: {MachineId}", machine.Id);
                throw new AlertGenerationException("Failed to evaluate alert rules for machine.", 500);
            }
        }

        private static Alert CreateAlert(Machine machine, string severity, string title, string description, string metric)
        {
            var metadata = new Dictionary<string, string>
            {
                { "metric", metric },
                { "severity", severity }
            };

            return new Alert
            {
                CompanyId = machine.CompanyId ?? 0,
                MachineId = machine.Id,
                Severity = severity,
                Title = title,
                Description = description,
                Status = AlertStatus.Open.ToString().ToLowerInvariant(),
                Metadata = JsonSerializer.Serialize(metadata)
            };
        }

        private static string? ExtractMetricKey(string? metadata)
        {
            if (string.IsNullOrEmpty(metadata)) return null;
            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(metadata);
                return parsed != null && parsed.TryGetValue("metric", out var metric) ? metric : null;
            }
            catch { return null; }
        }

        public async Task<Alert> AcknowledgeAlertAsync(long alertId, long userId)
        {
            try
            {
                var alert = await _dbContext.Alerts
                    .Include(a => a.Machine)
                    .FirstOrDefaultAsync(a => a.Id == alertId);

                if (alert == null)
                {
                    throw new KeyNotFoundException($"Alert not found: {alertId}");
                }

                alert.Status = AlertStatus.Acknowledged.ToString().ToLowerInvariant();
                alert.AcknowledgedBy = userId;
                alert.AcknowledgedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    EventType.Acknowledge.ToString(),
                    $"Alert acknowledged: {alert.Title}",
                    user: await _dbContext.Users.FindAsync(userId),
                    machine: alert.Machine
                );

                _logger.LogInformation("Alert {AlertId} acknowledged by user {UserId}", alertId, userId);
                return alert;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AlertService::AcknowledgeAlertAsync failed for alert ID: {AlertId}", alertId);
                throw;
            }
        }

        public async Task<Alert> ResolveAlertAsync(long alertId, long userId, string? resolution)
        {
            try
            {
                var alert = await _dbContext.Alerts
                    .Include(a => a.Machine)
                    .FirstOrDefaultAsync(a => a.Id == alertId);

                if (alert == null)
                {
                    throw new KeyNotFoundException($"Alert not found: {alertId}");
                }

                var resolutionMetadata = new Dictionary<string, string>();
                if (alert.Metadata != null)
                {
                    try
                    {
                        resolutionMetadata = JsonSerializer.Deserialize<Dictionary<string, string>>(alert.Metadata) ?? new Dictionary<string, string>();
                    }
                    catch { }
                }

                if (resolution != null)
                {
                    resolutionMetadata["resolution"] = resolution;
                }

                alert.Status = AlertStatus.Resolved.ToString().ToLowerInvariant();
                alert.ResolvedBy = userId;
                alert.ResolvedAt = DateTime.UtcNow;
                alert.Metadata = JsonSerializer.Serialize(resolutionMetadata);
                await _dbContext.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    EventType.Resolve.ToString(),
                    $"Alert resolved: {alert.Title} {(resolution != null ? " - " + resolution : "")}",
                    user: await _dbContext.Users.FindAsync(userId),
                    machine: alert.Machine
                );

                _logger.LogInformation("Alert {AlertId} resolved by user {UserId}", alertId, userId);
                return alert;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AlertService::ResolveAlertAsync failed for alert ID: {AlertId}", alertId);
                throw;
            }
        }

        public async Task<PaginatedResponseDto<Alert>> GetCompanyAlertsAsync(
            long companyId, 
            string? severity, 
            string? status, 
            int page, 
            int perPage)
        {
            try
            {
                var query = _dbContext.Alerts
                    .Include(a => a.Machine)
                    .Include(a => a.Acknowledger)
                    .Include(a => a.Resolver)
                    .Where(a => a.CompanyId == companyId);

                if (!string.IsNullOrEmpty(severity))
                {
                    query = query.Where(a => a.Severity == severity.ToLowerInvariant());
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(a => a.Status == status.ToLowerInvariant());
                }

                var total = await query.CountAsync();
                perPage = Math.Min(Math.Max(1, perPage), 100);
                var lastPage = (int)Math.Ceiling((double)total / perPage);
                page = Math.Min(Math.Max(1, page), Math.Max(1, lastPage));

                var items = await query
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((page - 1) * perPage)
                    .Take(perPage)
                    .ToListAsync();

                return new PaginatedResponseDto<Alert>
                {
                    Data = items,
                    CurrentPage = page,
                    PerPage = perPage,
                    Total = total,
                    LastPage = lastPage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AlertService::GetCompanyAlertsAsync failed for company: {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<IEnumerable<Alert>> GetMachineAlertsAsync(long machineId)
        {
            return await _dbContext.Alerts
                .Include(a => a.Acknowledger)
                .Include(a => a.Resolver)
                .Where(a => a.MachineId == machineId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Alert>> GetCriticalAlertsAsync(long companyId)
        {
            return await _dbContext.Alerts
                .Include(a => a.Machine)
                .Include(a => a.Acknowledger)
                .Include(a => a.Resolver)
                .Where(a => a.CompanyId == companyId && a.Severity == "critical" && (a.Status == "open" || a.Status == "acknowledged"))
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<AlertRule>> GetAlertRulesAsync(long companyId)
        {
            return await _dbContext.AlertRules
                .Where(r => r.CompanyId == companyId)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<AlertRule> UpdateAlertRuleAsync(long ruleId, IDictionary<string, object> data)
        {
            try
            {
                var rule = await _dbContext.AlertRules.FindAsync(ruleId);
                if (rule == null)
                {
                    throw new KeyNotFoundException($"Alert rule not found: {ruleId}");
                }

                if (data.TryGetValue("name", out var nameVal)) rule.Name = nameVal.ToString() ?? rule.Name;
                if (data.TryGetValue("is_enabled", out var enabledVal) && enabledVal is bool isEnabled) rule.IsEnabled = isEnabled;
                if (data.TryGetValue("severity", out var sevVal)) rule.Severity = sevVal.ToString() ?? rule.Severity;
                if (data.TryGetValue("threshold_value", out var threshVal) && decimal.TryParse(threshVal.ToString(), out var decVal)) rule.ThresholdValue = decVal;

                await _dbContext.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    EventType.Update.ToString(),
                    $"Alert rule updated: {rule.Name}"
                );

                _logger.LogInformation("Alert rule {RuleId} updated successfully", ruleId);
                return rule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AlertService::UpdateAlertRuleAsync failed for rule ID: {RuleId}", ruleId);
                throw;
            }
        }
    }
}
