using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Extensions;
using DeskGuardBackend.Services.Interfaces;

namespace DeskGuardBackend.Services.PayloadProcessors
{
    public class AlertProcessor : IPayloadProcessor
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly IAlertProfileService _alertProfileService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AlertProcessor> _logger;

        public AlertProcessor(
            DeskGuardDbContext dbContext,
            IAlertProfileService alertProfileService,
            INotificationService notificationService,
            ILogger<AlertProcessor> logger)
        {
            _dbContext = dbContext;
            _alertProfileService = alertProfileService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ProcessAsync(Machine machine, JsonElement payload, HealthLog healthLog)
        {
            try
            {
                var cpuProp = payload.GetPropertyOrNull("cpu");
                var cpu = cpuProp?.ValueKind == JsonValueKind.Object ? cpuProp.Value : default;

                var memoryProp = payload.GetPropertyOrNull("memory");
                var memory = memoryProp?.ValueKind == JsonValueKind.Object ? memoryProp.Value : default;

                var disksProp = payload.GetPropertyOrNull("disks");
                var antivirusProp = payload.GetPropertyOrNull("antivirus");
                var antivirus = antivirusProp?.ValueKind == JsonValueKind.Object ? antivirusProp.Value : default;

                var firewallProp = payload.GetPropertyOrNull("firewall");
                var firewall = firewallProp?.ValueKind == JsonValueKind.Object ? firewallProp.Value : default;

                var cpuUsage = cpu.ValueKind == JsonValueKind.Object ? (cpu.GetDecimalProperty("usagePercentage") ?? cpu.GetDecimalProperty("usage_percentage")) : null;
                var memUsage = memory.ValueKind == JsonValueKind.Object ? (memory.GetDecimalProperty("usagePercentage") ?? memory.GetDecimalProperty("usage_percentage")) : null;
                var avEnabled = antivirus.ValueKind == JsonValueKind.Object ? (antivirus.GetBooleanProperty("isRealTimeProtectionEnabled") ?? antivirus.GetBooleanProperty("is_real_time_protection_enabled")) : null;
                var fwEnabled = firewall.ValueKind == JsonValueKind.Object ? (firewall.GetBooleanProperty("isEnabled") ?? firewall.GetBooleanProperty("is_enabled")) : null;

                // Resolve profile thresholds
                var thresholds = await _alertProfileService.ResolveThresholdsForMachineAsync(machine.Id);

                // Determine active thresholds (fallback to defaults if profile missing)
                decimal cpuWarningLimit = thresholds?.CpuWarningPercent ?? 80m;
                decimal cpuCriticalLimit = thresholds?.CpuCriticalPercent ?? 90m;
                decimal ramWarningLimit = thresholds?.RamWarningPercent ?? 80m;
                decimal ramCriticalLimit = thresholds?.RamCriticalPercent ?? 90m;
                decimal diskWarningLimit = thresholds?.DiskWarningPercent ?? 85m;
                decimal diskCriticalLimit = thresholds?.DiskCriticalPercent ?? 95m;

                // ── 1. ANTIVIRUS PROTECTION EVALUATION ──
                if (avEnabled.HasValue)
                {
                    bool isAvTripped = !avEnabled.Value;
                    await EvaluateIncidentAsync(
                        machine: machine,
                        alertType: "antivirus_disabled",
                        resource: "Antivirus",
                        isTripped: isAvTripped,
                        severity: "critical",
                        title: "Antivirus Protection Disabled",
                        description: $"Antivirus real-time protection is disabled on {machine.Hostname}."
                    );
                }

                // ── 2. FIREWALL EVALUATION ──
                if (fwEnabled.HasValue)
                {
                    bool isFwTripped = !fwEnabled.Value;
                    await EvaluateIncidentAsync(
                        machine: machine,
                        alertType: "firewall_disabled",
                        resource: "Firewall",
                        isTripped: isFwTripped,
                        severity: "warning",
                        title: "Windows Firewall Disabled",
                        description: $"Windows Firewall protection is disabled on {machine.Hostname}."
                    );
                }

                // ── 3. CPU USAGE EVALUATION ──
                if (cpuUsage.HasValue)
                {
                    bool isCpuCritical = cpuUsage.Value >= cpuCriticalLimit;
                    bool isCpuWarning = cpuUsage.Value >= cpuWarningLimit;
                    bool isCpuTripped = isCpuWarning || isCpuCritical;
                    string cpuSeverity = isCpuCritical ? "critical" : "warning";
                    string cpuTitle = isCpuCritical ? "Critical High CPU Usage" : "High CPU Usage Warning";

                    await EvaluateIncidentAsync(
                        machine: machine,
                        alertType: "cpu_usage",
                        resource: "CPU",
                        isTripped: isCpuTripped,
                        severity: cpuSeverity,
                        title: cpuTitle,
                        description: $"CPU utilization is at {cpuUsage.Value}% on {machine.Hostname}.",
                        currentValue: cpuUsage.Value,
                        thresholdValue: isCpuCritical ? cpuCriticalLimit : cpuWarningLimit
                    );
                }

                // ── 4. RAM USAGE EVALUATION ──
                if (memUsage.HasValue)
                {
                    bool isRamCritical = memUsage.Value >= ramCriticalLimit;
                    bool isRamWarning = memUsage.Value >= ramWarningLimit;
                    bool isRamTripped = isRamWarning || isRamCritical;
                    string ramSeverity = isRamCritical ? "critical" : "warning";
                    string ramTitle = isRamCritical ? "Critical High Memory Usage" : "High Memory Usage Warning";

                    await EvaluateIncidentAsync(
                        machine: machine,
                        alertType: "ram_usage",
                        resource: "RAM",
                        isTripped: isRamTripped,
                        severity: ramSeverity,
                        title: ramTitle,
                        description: $"Memory utilization is at {memUsage.Value}% on {machine.Hostname}.",
                        currentValue: memUsage.Value,
                        thresholdValue: isRamCritical ? ramCriticalLimit : ramWarningLimit
                    );
                }

                // ── 5. DISK DRIVES EVALUATION ──
                if (disksProp.HasValue && disksProp.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var disk in disksProp.Value.EnumerateArray())
                    {
                        var usagePercent = disk.GetDecimalProperty("usagePercentage") ?? disk.GetDecimalProperty("usage_percentage");
                        var driveName = disk.GetStringProperty("driveName") ?? disk.GetStringProperty("drive_letter") ?? "Drive C:";

                        if (usagePercent.HasValue)
                        {
                            bool isDiskCritical = usagePercent.Value >= diskCriticalLimit;
                            bool isDiskWarning = usagePercent.Value >= diskWarningLimit;
                            bool isDiskTripped = isDiskWarning || isDiskCritical;
                            string diskSeverity = isDiskCritical ? "critical" : "warning";
                            string diskTitle = isDiskCritical ? $"Critical Low Disk Space ({driveName})" : $"Low Disk Space Warning ({driveName})";

                            await EvaluateIncidentAsync(
                                machine: machine,
                                alertType: "disk_usage",
                                resource: driveName,
                                isTripped: isDiskTripped,
                                severity: diskSeverity,
                                title: diskTitle,
                                description: $"Disk drive {driveName} is {usagePercent.Value}% full on {machine.Hostname}.",
                                currentValue: usagePercent.Value,
                                thresholdValue: isDiskCritical ? diskCriticalLimit : diskWarningLimit
                            );
                        }
                    }
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogDebug("AlertProcessor: Successfully processed intelligent alert lifecycle for machine {MachineId}", machine.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AlertProcessor: Error during alert evaluation for machine {MachineId}", machine.Id);
            }
        }

        /// <summary>
        /// Evaluates a single metric incident.
        /// If active: updates existing alert (occurrence count++, peak value, last seen) without inserting duplicate rows.
        /// If resolved: auto-resolves existing alert when metric returns below threshold.
        /// If new: inserts a new Alert record and sends a notification ONCE.
        /// </summary>
        private async Task EvaluateIncidentAsync(
            Machine machine,
            string alertType,
            string resource,
            bool isTripped,
            string severity,
            string title,
            string description,
            decimal? currentValue = null,
            decimal? thresholdValue = null)
        {
            var existingAlert = await _dbContext.Alerts
                .FirstOrDefaultAsync(a =>
                    a.MachineId == machine.Id &&
                    a.AlertType == alertType &&
                    a.Resource == resource &&
                    (a.Status == "open" || a.Status == "acknowledged"));

            if (isTripped)
            {
                if (existingAlert != null)
                {
                    // ONGOING INCIDENT: Update existing alert in-place
                    existingAlert.OccurrenceCount++;
                    existingAlert.LastDetectedAt = DateTime.UtcNow;
                    existingAlert.CurrentValue = currentValue;
                    if (currentValue.HasValue)
                    {
                        existingAlert.MaxRecordedValue = Math.Max(existingAlert.MaxRecordedValue ?? currentValue.Value, currentValue.Value);
                    }
                    existingAlert.Severity = severity;
                    existingAlert.Title = title;
                    existingAlert.Description = $"{description} (Occurrences: {existingAlert.OccurrenceCount}, Peak: {existingAlert.MaxRecordedValue ?? currentValue}%)";
                    existingAlert.UpdatedAt = DateTime.UtcNow;

                    _logger.LogDebug("AlertProcessor: Updated ongoing alert {AlertId} for machine {MachineId} ({AlertType}, Count: {Count})",
                        existingAlert.Id, machine.Id, alertType, existingAlert.OccurrenceCount);
                }
                else
                {
                    // NEW INCIDENT: Insert single new alert record
                    var newAlert = new Alert
                    {
                        MachineId = machine.Id,
                        CompanyId = machine.CompanyId ?? 0,
                        AlertType = alertType,
                        Resource = resource,
                        Severity = severity,
                        Title = title,
                        Description = description,
                        Status = "open",
                        CurrentValue = currentValue,
                        ThresholdValue = thresholdValue,
                        MaxRecordedValue = currentValue,
                        FirstDetectedAt = DateTime.UtcNow,
                        LastDetectedAt = DateTime.UtcNow,
                        OccurrenceCount = 1,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _dbContext.Alerts.AddAsync(newAlert);
                    _logger.LogInformation("AlertProcessor: Created NEW incident alert for machine {MachineId} ({AlertType}, Resource: {Resource})",
                        machine.Id, alertType, resource);

                    // Send notification ONCE for new incident
                    try
                    {
                        await _notificationService.SendAlertNotificationAsync(newAlert);
                        if (severity == "critical" || severity == "warning")
                        {
                            await _notificationService.SendEmailNotificationAsync(newAlert);
                        }
                    }
                    catch (Exception notifEx)
                    {
                        _logger.LogWarning(notifEx, "Failed to send notification for new alert");
                    }
                }
            }
            else
            {
                // ISSUE IS NOW RESOLVED: Auto-resolve existing active alert
                if (existingAlert != null)
                {
                    existingAlert.Status = "resolved";
                    existingAlert.ResolvedAt = DateTime.UtcNow;
                    existingAlert.ResolutionNote = $"Automatically resolved: {resource} returned to normal levels ({currentValue ?? 0}%).";
                    var firstDetected = existingAlert.FirstDetectedAt ?? existingAlert.CreatedAt;
                    existingAlert.DurationSeconds = (int)Math.Max(0, (DateTime.UtcNow - firstDetected).TotalSeconds);
                    existingAlert.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation("AlertProcessor: Auto-resolved alert {AlertId} for machine {MachineId} ({AlertType}, Duration: {Duration}s)",
                        existingAlert.Id, machine.Id, alertType, existingAlert.DurationSeconds);
                }
            }
        }
    }
}
