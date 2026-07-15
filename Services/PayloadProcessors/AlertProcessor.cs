using System;
using System.Collections.Generic;
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
        private readonly ILogger<AlertProcessor> _logger;

        public AlertProcessor(DeskGuardDbContext dbContext, IAlertProfileService alertProfileService, ILogger<AlertProcessor> logger)
        {
            _dbContext = dbContext;
            _alertProfileService = alertProfileService;
            _logger = logger;
        }

        public async Task ProcessAsync(Machine machine, JsonElement payload, HealthLog healthLog)
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

            // ── FIXED RULES (always fire regardless of profile) ──
            var newAlerts = new List<Alert>();

            if (avEnabled.HasValue && !avEnabled.Value)
            {
                newAlerts.Add(CreateAlert(machine, "critical", "Antivirus Disabled", $"Antivirus real-time protection is disabled on {machine.Hostname}.", "antivirus_disabled"));
            }

            if (fwEnabled.HasValue && !fwEnabled.Value)
            {
                newAlerts.Add(CreateAlert(machine, "warning", "Firewall Disabled", $"Windows Firewall is disabled on {machine.Hostname}.", "firewall_disabled"));
            }

            // ── PROFILE-BASED THRESHOLDS ──
            if (thresholds != null)
            {
                // CPU
                if (thresholds.CpuWarningPercent.HasValue && cpuUsage.HasValue && cpuUsage.Value >= thresholds.CpuWarningPercent.Value)
                    newAlerts.Add(CreateAlert(machine, "warning", "High CPU Usage", $"CPU usage is {cpuUsage.Value}% on {machine.Hostname}.", $"cpu_warning_{machine.Id}"));
                if (thresholds.CpuCriticalPercent.HasValue && cpuUsage.HasValue && cpuUsage.Value >= thresholds.CpuCriticalPercent.Value)
                    newAlerts.Add(CreateAlert(machine, "critical", "Critical CPU Usage", $"CPU usage is {cpuUsage.Value}% on {machine.Hostname}.", $"cpu_critical_{machine.Id}"));

                // RAM
                if (thresholds.RamWarningPercent.HasValue && memUsage.HasValue && memUsage.Value >= thresholds.RamWarningPercent.Value)
                    newAlerts.Add(CreateAlert(machine, "warning", "High Memory Usage", $"Memory usage is {memUsage.Value}% on {machine.Hostname}.", $"ram_warning_{machine.Id}"));
                if (thresholds.RamCriticalPercent.HasValue && memUsage.HasValue && memUsage.Value >= thresholds.RamCriticalPercent.Value)
                    newAlerts.Add(CreateAlert(machine, "critical", "Critical Memory Usage", $"Memory usage is {memUsage.Value}% on {machine.Hostname}.", $"ram_critical_{machine.Id}"));

                // Disk
                if (disksProp.HasValue && disksProp.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var disk in disksProp.Value.EnumerateArray())
                    {
                        var usagePercent = disk.GetDecimalProperty("usagePercentage") ?? disk.GetDecimalProperty("usage_percentage");
                        var driveName = disk.GetStringProperty("driveName") ?? disk.GetStringProperty("drive_letter") ?? "Unknown";

                        if (thresholds.DiskWarningPercent.HasValue && usagePercent.HasValue && usagePercent.Value >= thresholds.DiskWarningPercent.Value)
                            newAlerts.Add(CreateAlert(machine, "warning", "High Disk Usage", $"Drive {driveName} is {usagePercent.Value}% full on {machine.Hostname}.", $"disk_warning_{machine.Id}_{driveName}"));
                        if (thresholds.DiskCriticalPercent.HasValue && usagePercent.HasValue && usagePercent.Value >= thresholds.DiskCriticalPercent.Value)
                            newAlerts.Add(CreateAlert(machine, "critical", "Critical Disk Usage", $"Drive {driveName} is {usagePercent.Value}% full on {machine.Hostname}.", $"disk_critical_{machine.Id}_{driveName}"));
                    }
                }
            }
            else
            {
                // Fallback hardcoded thresholds if no profile is configured
                if (cpuUsage.HasValue && cpuUsage.Value > 90)
                    newAlerts.Add(CreateAlert(machine, "critical", "High CPU Usage", $"CPU usage is {cpuUsage.Value}% on {machine.Hostname}.", "cpu_critical"));

                if (memUsage.HasValue && memUsage.Value > 90)
                    newAlerts.Add(CreateAlert(machine, "critical", "High Memory Usage", $"Memory usage is {memUsage.Value}% on {machine.Hostname}.", "ram_critical"));

                if (disksProp.HasValue && disksProp.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var disk in disksProp.Value.EnumerateArray())
                    {
                        var usagePercent = disk.GetDecimalProperty("usagePercentage") ?? disk.GetDecimalProperty("usage_percentage");
                        var driveName = disk.GetStringProperty("driveName") ?? disk.GetStringProperty("drive_letter") ?? "Unknown";

                        if (usagePercent.HasValue && usagePercent.Value > 95)
                            newAlerts.Add(CreateAlert(machine, "warning", "Disk Almost Full", $"Drive {driveName} is {usagePercent.Value}% full on {machine.Hostname}.", $"disk_full_{driveName}"));
                    }
                }
            }

            // Deduplicate against existing open alerts
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

            foreach (var alert in newAlerts)
            {
                var metricKey = ExtractMetricKey(alert.Metadata);
                if (!string.IsNullOrEmpty(metricKey) && existingMetrics.Contains(metricKey))
                    continue;

                await _dbContext.Alerts.AddAsync(alert);
            }

            if (newAlerts.Count > 0)
            {
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("AlertProcessor: Created {Count} alert(s) for machine {MachineId}", newAlerts.Count, machine.Id);
            }

            _logger.LogDebug("AlertProcessor: Alerts evaluated for machine {MachineId}", machine.Id);
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
                MachineId = machine.Id,
                CompanyId = machine.CompanyId ?? 0,
                Severity = severity,
                Title = title,
                Description = description,
                Status = "open",
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
    }
}
