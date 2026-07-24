using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Reports.Interfaces;
using DeskGuardBackend.Reports.Models;
using DeskGuardBackend.Reports.PDF;

namespace DeskGuardBackend.Reports.Services
{
    public class ReportGenerationService : IReportGenerationService
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<ReportGenerationService> _logger;

        public ReportGenerationService(DeskGuardDbContext dbContext, ILogger<ReportGenerationService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
            
            // Set QuestPDF License
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set QuestPDF license in service constructor");
            }
        }

        public async Task<AmcHealthSummaryReportData> GetAmcHealthSummaryDataAsync(AmcHealthSummaryQueryParameters queryParams)
        {
            var companyId = queryParams.CompanyId ?? queryParams.CustomerId ?? 1;
            var dateFrom = queryParams.DateFrom ?? queryParams.StartDate ?? DateTime.UtcNow.AddDays(-30);
            var dateTo = queryParams.DateTo ?? queryParams.EndDate ?? DateTime.UtcNow;

            var company = await _dbContext.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == companyId);

            if (company == null)
            {
                throw new KeyNotFoundException($"Company with ID {companyId} not found.");
            }

            var plan = queryParams.AmcPlan ?? company.AmcPlan;

            var reportData = new AmcHealthSummaryReportData
            {
                ReportId = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                CompanyName = company.Name,
                AmcPlan = string.IsNullOrEmpty(plan) ? "No Data Available" : plan,
                AmcStartDateStr = company.AmcStartDate?.ToString("yyyy-MM-dd") ?? "No Data Available",
                AmcEndDateStr = company.AmcEndDate?.ToString("yyyy-MM-dd") ?? "No Data Available",
                ReportPeriodStr = $"{dateFrom:yyyy-MM-dd} to {dateTo:yyyy-MM-dd}",
                GeneratedAt = DateTime.UtcNow
            };

            // 1. Fetch Machines & Current Statuses (Using AsNoTracking for performance)
            var machines = await _dbContext.Machines
                .AsNoTracking()
                .Include(m => m.CurrentStatus)
                .Where(m => m.CompanyId == companyId && m.IsActive)
                .ToListAsync();

            reportData.TotalSystems = machines.Count;
            if (reportData.TotalSystems == 0)
            {
                return reportData; // Empty dataset handled gracefully
            }

            double totalHealthScore = 0;
            int onlineCount = 0;
            int offlineCount = 0;
            int healthyCount = 0;
            int warningCount = 0;
            int criticalCount = 0;

            var recommendations = new List<string>();
            var agentVersionDist = new Dictionary<string, int>();

            foreach (var m in machines)
            {
                // Determine online/offline status
                bool isOnline = m.IsOnline;
                if (isOnline) onlineCount++;
                else offlineCount++;

                // Health Score calculation (matches UI design logic)
                double score = 100;
                var cs = m.CurrentStatus;
                
                if (cs != null)
                {
                    double cpu = (double)(cs.CpuPercentage ?? 0);
                    double ram = (double)(cs.RamPercentage ?? 0);
                    double disk = (double)(cs.DiskPercentage ?? 0);

                    if (cpu > 90) score -= 30;
                    else if (cpu > 70) score -= 15;

                    if (ram > 90) score -= 25;
                    else if (ram > 70) score -= 10;

                    if (disk > 90) score -= 20;
                    else if (disk > 70) score -= 5;
                }
                else
                {
                    score = isOnline ? 100 : 0;
                }

                score = Math.Max(0, Math.Min(100, score));
                totalHealthScore += score;

                // Determine condition category
                string statusLabel = "Online";
                if (!isOnline)
                {
                    statusLabel = score < 50 ? "Critical" : (score < 80 ? "Warning" : "Offline");
                }
                else
                {
                    statusLabel = score < 50 ? "Critical" : (score < 80 ? "Warning" : "Online");
                }

                if (statusLabel == "Online") healthyCount++;
                else if (statusLabel == "Warning") warningCount++;
                else if (statusLabel == "Critical") criticalCount++;

                // Recommendations from actual data
                if (cs != null)
                {
                    if (cs.FirewallEnabled == false)
                    {
                        recommendations.Add($"Enable Firewall on machine {m.Hostname ?? m.DeviceName ?? "System"} to block unauthorized traffic.");
                    }
                    if (cs.AntivirusEnabled == false || string.IsNullOrEmpty(cs.AntivirusName))
                    {
                        recommendations.Add($"Enable and update Antivirus protection on machine {m.Hostname ?? m.DeviceName ?? "System"}.");
                    }
                    if (cs.RamPercentage > 90)
                    {
                        recommendations.Add($"Increase RAM capacity on machine {m.Hostname ?? m.DeviceName ?? "System"} (current usage is {(int)(cs.RamPercentage ?? 0)}%).");
                    }
                    if (cs.DiskPercentage > 90 || cs.DiskHealthStatus == "Bad")
                    {
                        recommendations.Add($"Replace failing storage drive or clear up disk space on machine {m.Hostname ?? m.DeviceName ?? "System"} (current usage is {(int)(cs.DiskPercentage ?? 0)}%).");
                    }
                }

                // Add Row to PDF systems list
                var row = new SystemHealthRow
                {
                    MachineName = m.Hostname ?? m.DeviceName ?? "Unknown Device",
                    MachineId = m.MachineUid,
                    HealthScore = score,
                    CurrentStatus = isOnline ? "Online" : "Offline",
                    LastHeartbeat = m.LastHeartbeatAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Not Available",
                    CpuStatus = cs?.CpuPercentage != null ? $"{cs.CpuPercentage:F1}%" : "Not Available",
                    RamStatus = cs?.RamPercentage != null ? $"{cs.RamPercentage:F1}%" : "Not Available",
                    DiskStatus = cs?.DiskPercentage != null ? $"{cs.DiskPercentage:F1}%" : "Not Available",
                    SecurityStatus = (cs?.AntivirusEnabled == true && cs?.FirewallEnabled == true) ? "Protected" : "At Risk",
                    OverallCondition = score >= 90 ? "Optimal" : score >= 70 ? "Normal" : "Requires Attention"
                };
                reportData.Systems.Add(row);
            }

            // Summary metrics
            reportData.TotalSystems = machines.Count;
            reportData.HealthySystems = healthyCount;
            reportData.WarningSystems = warningCount;
            reportData.CriticalSystems = criticalCount;
            reportData.OfflineSystems = offlineCount;
            reportData.AverageHealthScore = Math.Round(totalHealthScore / machines.Count, 1);

            // Fetch Recent Alerts for Company
            var alerts = await _dbContext.Alerts
                .AsNoTracking()
                .Where(a => a.CompanyId == companyId && a.CreatedAt >= dateFrom && a.CreatedAt <= dateTo)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            reportData.OpenAlerts = alerts.Count(a => a.Status == "open");
            reportData.ResolvedAlerts = alerts.Count(a => a.Status == "resolved");
            reportData.AlertCriticalCount = alerts.Count(a => a.Severity.Equals("critical", StringComparison.OrdinalIgnoreCase));
            reportData.AlertImportantCount = alerts.Count(a => a.Severity.Equals("important", StringComparison.OrdinalIgnoreCase) || a.Severity.Equals("high", StringComparison.OrdinalIgnoreCase));
            reportData.AlertWarningCount = alerts.Count(a => a.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase) || a.Severity.Equals("medium", StringComparison.OrdinalIgnoreCase));
            reportData.AlertInfoCount = alerts.Count(a => a.Severity.Equals("info", StringComparison.OrdinalIgnoreCase) || a.Severity.Equals("low", StringComparison.OrdinalIgnoreCase));

            // Calculate average alert response time
            var resolvedAlertsWithTime = alerts.Where(a => a.ResolvedAt.HasValue && a.Status == "resolved").ToList();
            if (resolvedAlertsWithTime.Any())
            {
                var totalDuration = TimeSpan.Zero;
                foreach (var ra in resolvedAlertsWithTime)
                {
                    totalDuration += (ra.ResolvedAt!.Value - ra.CreatedAt);
                }
                var avgMinutes = totalDuration.TotalMinutes / resolvedAlertsWithTime.Count;
                if (avgMinutes >= 1440)
                {
                    reportData.AverageResponseTime = $"{avgMinutes / 1440:F1} days";
                }
                else if (avgMinutes >= 60)
                {
                    reportData.AverageResponseTime = $"{avgMinutes / 60:F1} hours";
                }
                else
                {
                    reportData.AverageResponseTime = $"{(int)avgMinutes} minutes";
                }
            }
            else
            {
                reportData.AverageResponseTime = "No Data Available";
            }

            // Recent Critical Alerts List
            var recentCriticals = alerts
                .Where(a => a.Severity.Equals("critical", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .ToList();

            foreach (var rca in recentCriticals)
            {
                var machineObj = machines.FirstOrDefault(m => m.Id == rca.MachineId);
                reportData.RecentCriticalAlerts.Add(new RecentAlertRow
                {
                    Title = rca.Title,
                    MachineName = machineObj?.Hostname ?? machineObj?.DeviceName ?? "System",
                    Severity = rca.Severity,
                    Status = rca.Status,
                    CreatedAt = rca.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                });
            }

            // 3. Fetch Changes (Using Date Range filtering)
            var changes = await _dbContext.ChangeHistories
                .AsNoTracking()
                .Where(c => c.CompanyId == companyId && c.DetectedAt >= dateFrom && c.DetectedAt <= dateTo)
                .ToListAsync();

            reportData.HardwareChangesCount = changes.Count(c => c.Category.Equals("hardware", StringComparison.OrdinalIgnoreCase));
            reportData.SoftwareChangesCount = changes.Count(c => c.Category.Equals("software", StringComparison.OrdinalIgnoreCase));

            reportData.ChangesHardwareCount = reportData.HardwareChangesCount;
            reportData.ChangesSoftwareCount = reportData.SoftwareChangesCount;
            reportData.ChangesUsbCount = changes.Count(c => c.Category.Equals("peripheral", StringComparison.OrdinalIgnoreCase) || c.Category.Equals("usb", StringComparison.OrdinalIgnoreCase));
            reportData.ChangesConfigCount = changes.Count(c => c.Category.Equals("configuration", StringComparison.OrdinalIgnoreCase) || c.Category.Equals("config", StringComparison.OrdinalIgnoreCase));
            reportData.ChangesSecurityCount = changes.Count(c => c.Category.Equals("security", StringComparison.OrdinalIgnoreCase));
            reportData.ChangesNetworkCount = changes.Count(c => c.Category.Equals("network", StringComparison.OrdinalIgnoreCase));

            // Recent Changes list
            var recentChangesList = changes
                .OrderByDescending(c => c.DetectedAt)
                .Take(5)
                .ToList();

            foreach (var ch in recentChangesList)
            {
                var machineObj = machines.FirstOrDefault(m => m.Id == ch.MachineId);
                reportData.RecentChanges.Add(new RecentChangeRow
                {
                    MachineName = machineObj?.Hostname ?? machineObj?.DeviceName ?? "System",
                    Category = ch.Category,
                    ChangeType = ch.ChangeType,
                    Description = ch.Description ?? "No description available",
                    DetectedAt = ch.DetectedAt.ToString("yyyy-MM-dd HH:mm")
                });
            }

            // 4. Security Overview
            int fwEnabled = 0;
            int avEnabled = 0;
            foreach (var m in machines)
            {
                if (m.CurrentStatus?.FirewallEnabled == true) fwEnabled++;
                if (m.CurrentStatus?.AntivirusEnabled == true) avEnabled++;
            }
            reportData.FirewallEnabledCount = fwEnabled;
            reportData.AntivirusEnabledCount = avEnabled;

            // Windows Update status
            var totalPendingUpdates = await _dbContext.WindowsUpdates
                .AsNoTracking()
                .CountAsync(u => u.Machine.CompanyId == companyId && !u.IsInstalled);

            if (totalPendingUpdates == 0)
            {
                reportData.WindowsUpdateStatus = "All systems up-to-date";
            }
            else
            {
                reportData.WindowsUpdateStatus = $"{totalPendingUpdates} pending updates";
                recommendations.Add($"Install {totalPendingUpdates} outstanding Windows Update(s) across systems to resolve security patches.");
            }

            reportData.BitLockerStatus = "No Data Available"; // BitLocker encryption state not captured in schema

            // Unauthorized changes (Hardware/Security changes marked critical)
            var unauthorized = changes.Count(c => c.Severity == "critical" || (c.Category == "security" && c.ChangeType == "disabled"));
            reportData.UnauthorizedChangesCount = unauthorized;
            if (unauthorized > 0)
            {
                recommendations.Add($"Review {unauthorized} unauthorized security/critical changes reported in the system log.");
            }

            // Security score calculation
            double secScore = 100;
            double firewallScoreDeduction = (double)(reportData.TotalSystems - fwEnabled) / reportData.TotalSystems * 30;
            double antivirusScoreDeduction = (double)(reportData.TotalSystems - avEnabled) / reportData.TotalSystems * 30;
            double updatesDeduction = totalPendingUpdates > 0 ? 20 : 0;
            double unauthorizedDeduction = unauthorized > 0 ? 20 : 0;

            secScore = secScore - firewallScoreDeduction - antivirusScoreDeduction - updatesDeduction - unauthorizedDeduction;
            reportData.SecurityScore = Math.Max(0, Math.Min(100, secScore));

            // 5. System Status Summary
            var lastCollectedTime = machines
                .Where(m => m.CurrentStatus?.CollectedAt != null)
                .Max(m => m.CurrentStatus?.CollectedAt);

            reportData.LastCollectionTime = lastCollectedTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "No Data Available";

            var lastSyncTime = machines
                .Where(m => m.LastHeartbeatAt != null)
                .Max(m => m.LastHeartbeatAt);

            reportData.LastSyncTime = lastSyncTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "No Data Available";

            // Default Agent Version distribution (as column is not in DB)
            reportData.AgentVersionDistribution = new Dictionary<string, int>
            {
                { "1.0.0", reportData.TotalSystems }
            };

            // Dedup recommendations
            reportData.HealthRecommendations = recommendations.Distinct().ToList();

            return reportData;
        }

        public byte[] GenerateAmcHealthSummaryPdf(AmcHealthSummaryReportData data)
        {
            try
            {
                var doc = new AmcHealthSummaryReportDocument(data);
                return doc.GeneratePdf();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compile QuestPDF report document");
                throw;
            }
        }

        public async Task<AssetInventoryReportData> GetAssetInventoryDataAsync(AssetInventoryQueryParameters queryParams)
        {
            var companyId = queryParams.CompanyId ?? queryParams.CustomerId ?? 1;
            var dateFrom = queryParams.DateFrom ?? DateTime.UtcNow.AddDays(-30);
            var dateTo = queryParams.DateTo ?? DateTime.UtcNow;

            var company = await _dbContext.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == companyId);

            if (company == null)
            {
                throw new KeyNotFoundException($"Company with ID {companyId} not found.");
            }

            var plan = queryParams.AmcPlan ?? company.AmcPlan;

            var reportData = new AssetInventoryReportData
            {
                ReportId = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                CompanyName = company.Name,
                RegisteredMobileNumber = string.IsNullOrEmpty(company.Phone) ? "Not Available" : company.Phone,
                Email = string.IsNullOrEmpty(company.Email) ? "Not Available" : company.Email,
                AmcPlan = string.IsNullOrEmpty(plan) ? "Not Available" : plan,
                AmcStartDateStr = company.AmcStartDate?.ToString("yyyy-MM-dd") ?? "Not Available",
                AmcEndDateStr = company.AmcEndDate?.ToString("yyyy-MM-dd") ?? "Not Available",
                TotalSystemsCovered = 0, // Set after counting
                ReportPeriodStr = $"{dateFrom:yyyy-MM-dd} to {dateTo:yyyy-MM-dd}",
                GeneratedAt = DateTime.UtcNow
            };

            // Query active machines matching the Company ID and optional Machine ID filter
            var machinesQuery = _dbContext.Machines
                .AsNoTracking()
                .Include(m => m.AssignedUser)
                .Where(m => m.CompanyId == companyId && m.IsActive);

            if (queryParams.MachineId.HasValue)
            {
                machinesQuery = machinesQuery.Where(m => m.Id == queryParams.MachineId.Value);
            }

            var machines = await machinesQuery.ToListAsync();
            reportData.TotalSystems = machines.Count;
            reportData.TotalSystemsCovered = machines.Count;

            if (machines.Count == 0)
            {
                return reportData;
            }

            var machineIds = machines.Select(m => m.Id).ToList();

            // Fetch telemetry sub-tables concurrently using AsNoTracking for speed
            var hardwareList = await _dbContext.HardwareInventories
                .AsNoTracking()
                .Where(h => machineIds.Contains(h.MachineId))
                .ToListAsync();

            var diskList = await _dbContext.MachineDisks
                .AsNoTracking()
                .Where(d => machineIds.Contains(d.MachineId))
                .ToListAsync();

            var adapterList = await _dbContext.MachineNetworkAdapters
                .AsNoTracking()
                .Where(a => machineIds.Contains(a.MachineId))
                .ToListAsync();

            var deviceList = await _dbContext.MachineConnectedDevices
                .AsNoTracking()
                .Where(c => machineIds.Contains(c.MachineId))
                .ToListAsync();

            var statusList = await _dbContext.MachineCurrentStatuses
                .AsNoTracking()
                .Where(s => machineIds.Contains(s.MachineId))
                .ToListAsync();

            var changes = await _dbContext.ChangeHistories
                .AsNoTracking()
                .Where(ch => machineIds.Contains(ch.MachineId) && ch.DetectedAt >= dateFrom && ch.DetectedAt <= dateTo)
                .ToListAsync();

            // Compute executive summary & system inventories
            int totalCpus = 0;
            long totalRamBytes = 0;
            decimal totalStorageCapacityGb = 0;
            int totalSsds = 0;
            int totalHdds = 0;
            int totalPrinters = 0;
            int totalMonitors = 0;
            int totalNetworkAdapters = 0;
            int totalGpus = 0;
            int totalBatteries = 0;
            int totalUsbDevices = 0;

            int laptopCount = 0;
            int desktopCount = 0;

            var cpuBrands = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var gpuBrands = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var recommendations = new List<string>();

            foreach (var m in machines)
            {
                var hw = hardwareList.FirstOrDefault(h => h.MachineId == m.Id);
                var status = statusList.FirstOrDefault(s => s.MachineId == m.Id);
                var disks = diskList.Where(d => d.MachineId == m.Id).ToList();
                var adapters = adapterList.Where(a => a.MachineId == m.Id).ToList();
                var devices = deviceList.Where(c => c.MachineId == m.Id).ToList();

                var item = new SystemInventoryItem
                {
                    Id = m.Id,
                    MachineName = m.Hostname ?? m.DeviceName ?? "Unknown Device",
                    MachineId = m.MachineUid,
                    OperatingSystem = string.IsNullOrEmpty(m.OperatingSystem) ? (string.IsNullOrEmpty(m.OsVersion) ? "Not Available" : m.OsVersion) : m.OperatingSystem,
                    CurrentUser = string.IsNullOrEmpty(m.AssignedUser?.Name) ? "Not Available" : m.AssignedUser.Name,
                    LastHeartbeat = m.LastHeartbeatAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Not Available",
                    LastInventoryUpdate = hw?.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss") ?? "Not Available"
                };

                // Deduce Health Status
                double healthScore = 100;
                if (status != null)
                {
                    double cpuUsage = (double)(status.CpuPercentage ?? 0);
                    double ramUsage = (double)(status.RamPercentage ?? 0);
                    double diskUsage = (double)(status.DiskPercentage ?? 0);

                    if (cpuUsage > 90) healthScore -= 30;
                    else if (cpuUsage > 70) healthScore -= 15;

                    if (ramUsage > 90) healthScore -= 25;
                    else if (ramUsage > 70) healthScore -= 10;

                    if (diskUsage > 90) healthScore -= 20;
                    else if (diskUsage > 70) healthScore -= 5;
                }
                else
                {
                    healthScore = m.IsOnline ? 100 : 0;
                }

                healthScore = Math.Max(0, Math.Min(100, healthScore));
                item.HealthStatus = !m.IsOnline ? "Offline" : (healthScore < 50 ? "Critical" : (healthScore < 80 ? "Warning" : "Healthy"));

                // CPU
                if (hw != null && !string.IsNullOrEmpty(hw.CpuModel))
                {
                    totalCpus++;
                    item.Cpu.Manufacturer = hw.CpuModel.Contains("Intel", StringComparison.OrdinalIgnoreCase) ? "Intel" : (hw.CpuModel.Contains("AMD", StringComparison.OrdinalIgnoreCase) ? "AMD" : "Not Available");
                    item.Cpu.Model = hw.CpuModel;
                    item.Cpu.Generation = ExtractCpuGeneration(hw.CpuModel);
                    item.Cpu.ClockSpeed = hw.CpuMaxClockSpeed.HasValue ? $"{hw.CpuMaxClockSpeed:F2} GHz" : "Not Available";
                    item.Cpu.CoreCount = hw.CpuCores?.ToString() ?? "Not Available";
                    item.Cpu.LogicalProcessors = hw.CpuThreads?.ToString() ?? "Not Available";

                    var brand = item.Cpu.Manufacturer;
                    if (!cpuBrands.ContainsKey(brand)) cpuBrands[brand] = 0;
                    cpuBrands[brand]++;
                }

                // RAM
                if (hw != null && hw.TotalRamBytes.HasValue)
                {
                    totalRamBytes += hw.TotalRamBytes.Value;
                    item.Ram.Manufacturer = string.IsNullOrEmpty(hw.Manufacturer) ? "Not Available" : hw.Manufacturer;
                    item.Ram.TotalCapacity = $"{Math.Round(hw.TotalRamBytes.Value / (1024.0 * 1024 * 1024), 1)} GB";
                    item.Ram.Speed = string.IsNullOrEmpty(hw.RamSpeed) ? "Not Available" : hw.RamSpeed;
                    item.Ram.Type = string.IsNullOrEmpty(hw.RamType) ? "Not Available" : hw.RamType;
                    item.Ram.SlotCount = hw.RamSlots?.ToString() ?? "Not Available";

                    // Split serial number or model if it contains multiple RAM identifiers
                    if (!string.IsNullOrEmpty(hw.SerialNumber))
                    {
                        var parts = hw.SerialNumber.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var part in parts)
                        {
                            item.Ram.IndividualModules.Add(part.Trim());
                        }
                    }
                    if (!item.Ram.IndividualModules.Any() && !string.IsNullOrEmpty(hw.Model))
                    {
                        item.Ram.IndividualModules.Add(hw.Model);
                    }
                }

                // Storage Disks
                if (disks.Any())
                {
                    foreach (var d in disks)
                    {
                        var capacityGb = d.TotalGb ?? 0;
                        totalStorageCapacityGb += capacityGb;

                        var driveType = "HDD";
                        if (d.DriveType != null && d.DriveType.Contains("SSD", StringComparison.OrdinalIgnoreCase))
                        {
                            driveType = "SSD";
                            totalSsds++;
                        }
                        else
                        {
                            totalHdds++;
                        }

                        item.Disks.Add(new StorageDetail
                        {
                            DriveLetter = d.DriveLetter ?? "Not Available",
                            DriveType = driveType,
                            Manufacturer = "Not Available", // Drive manufacturer not directly in schema
                            Model = d.VolumeLabel ?? "Not Available",
                            Capacity = capacityGb > 0 ? $"{capacityGb:F1} GB" : "Not Available",
                            SerialNumber = "Not Available",
                            SmartStatus = string.IsNullOrEmpty(d.HealthStatus) ? "Not Available" : d.HealthStatus
                        });

                        // Storage SMART fail warning trigger
                        if (d.HealthStatus != null && (d.HealthStatus.Equals("Bad", StringComparison.OrdinalIgnoreCase) || d.HealthStatus.Equals("Fail", StringComparison.OrdinalIgnoreCase)))
                        {
                            reportData.MissingDevices.Add(new MissingDeviceItem
                            {
                                MachineName = item.MachineName,
                                Detail = $"Storage Issue: SMART warning on Drive {d.DriveLetter ?? "Primary"}"
                            });
                            recommendations.Add($"Replace failing storage disk on machine {item.MachineName} showing SMART warnings.");
                        }

                        // Low capacity warning
                        if (capacityGb > 0 && d.FreeGb.HasValue && (d.FreeGb.Value / capacityGb) < 0.1m)
                        {
                            reportData.MissingDevices.Add(new MissingDeviceItem
                            {
                                MachineName = item.MachineName,
                                Detail = $"Storage Issue: Drive {d.DriveLetter ?? "Primary"} is under 10% free space"
                            });
                            recommendations.Add($"Free up storage space or replace HDD with larger capacity SSD on machine {item.MachineName}.");
                        }
                    }
                }

                // GPU
                if (hw != null && !string.IsNullOrEmpty(hw.GpuName))
                {
                    totalGpus++;
                    item.Gpu.GpuName = hw.GpuName;
                    item.Gpu.Manufacturer = hw.GpuName.Contains("Nvidia", StringComparison.OrdinalIgnoreCase) ? "NVIDIA" : (hw.GpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase) || hw.GpuName.Contains("Radeon", StringComparison.OrdinalIgnoreCase) ? "AMD" : "Intel");
                    item.Gpu.Memory = hw.GpuMemoryBytes.HasValue ? $"{Math.Round(hw.GpuMemoryBytes.Value / (1024.0 * 1024 * 1024), 1)} GB" : "Not Available";
                    item.Gpu.DriverVersion = string.IsNullOrEmpty(hw.GpuDriverVersion) ? "Not Available" : hw.GpuDriverVersion;

                    var brand = item.Gpu.Manufacturer;
                    if (!gpuBrands.ContainsKey(brand)) gpuBrands[brand] = 0;
                    gpuBrands[brand]++;
                }

                // Motherboard
                if (hw != null)
                {
                    item.Motherboard.Manufacturer = string.IsNullOrEmpty(hw.Manufacturer) ? "Not Available" : hw.Manufacturer;
                    item.Motherboard.Model = string.IsNullOrEmpty(hw.MotherboardModel) ? "Not Available" : hw.MotherboardModel;
                    item.Motherboard.SerialNumber = string.IsNullOrEmpty(hw.SerialNumber) ? "Not Available" : hw.SerialNumber;
                    item.Motherboard.BiosVersion = string.IsNullOrEmpty(hw.BiosVersion) ? "Not Available" : hw.BiosVersion;
                    item.Motherboard.BiosDate = "Not Available"; // Bios Date not stored in schema
                }

                // Battery (Laptop status check)
                if (status != null && status.BatteryIsPresent == true)
                {
                    totalBatteries++;
                    laptopCount++;
                    item.Battery.IsPresent = true;
                    item.Battery.Manufacturer = "Not Available"; // Manufacturer not stored

                    // Health formula: Capacity percentage ratio or wear-level reduction
                    double wear = (double)(status.BatteryWearLevel ?? 0);
                    double health = 100 - wear;
                    if (status.BatteryFullChargeCapacity.HasValue && status.BatteryDesignCapacity.HasValue && status.BatteryDesignCapacity > 0)
                    {
                        health = ((double)status.BatteryFullChargeCapacity.Value / status.BatteryDesignCapacity.Value) * 100;
                    }
                    
                    item.Battery.Health = $"{health:F0}%";
                    item.Battery.Capacity = status.BatteryFullChargeCapacity.HasValue ? $"{status.BatteryFullChargeCapacity.Value} mWh" : "Not Available";
                    item.Battery.CycleCount = "Not Available"; // Cycle count not in schema
                    item.Battery.Status = status.BatteryChargingStatus == true ? "Charging" : "Discharging";

                    if (health < 60)
                    {
                        reportData.MissingDevices.Add(new MissingDeviceItem
                        {
                            MachineName = item.MachineName,
                            Detail = $"Battery health degraded to {health:F0}%"
                        });
                        recommendations.Add($"Replace unhealthy battery on machine {item.MachineName} (health is under 60%).");
                    }
                }
                else
                {
                    desktopCount++;
                }

                // Network Adapters
                if (adapters.Any())
                {
                    foreach (var a in adapters)
                    {
                        totalNetworkAdapters++;
                        item.NetworkAdapters.Add(new NetworkAdapterDetail
                        {
                            AdapterName = a.AdapterName,
                            MacAddress = a.MacAddress ?? "Not Available",
                            AdapterType = a.AdapterType ?? "Ethernet",
                            DriverVersion = "Not Available"
                        });
                    }
                }

                // Peripherals from Connected Devices list
                bool hasWebcam = false;
                bool hasPrinter = false;
                if (devices.Any())
                {
                    foreach (var dev in devices)
                    {
                        var name = dev.DeviceName ?? string.Empty;
                        var type = dev.DeviceType ?? string.Empty;

                        if (type.Equals("printer", StringComparison.OrdinalIgnoreCase) || name.Contains("printer", StringComparison.OrdinalIgnoreCase))
                        {
                            totalPrinters++;
                            hasPrinter = true;
                            item.Peripherals.Printers.Add(name);
                        }
                        else if (type.Equals("monitor", StringComparison.OrdinalIgnoreCase) || name.Contains("monitor", StringComparison.OrdinalIgnoreCase) || type.Equals("display", StringComparison.OrdinalIgnoreCase))
                        {
                            totalMonitors++;
                            item.Peripherals.Monitors.Add(name);
                        }
                        else if (type.Equals("webcam", StringComparison.OrdinalIgnoreCase) || name.Contains("webcam", StringComparison.OrdinalIgnoreCase) || name.Contains("camera", StringComparison.OrdinalIgnoreCase))
                        {
                            hasWebcam = true;
                            item.Peripherals.Webcams.Add(name);
                        }
                        else if (type.Equals("keyboard", StringComparison.OrdinalIgnoreCase))
                        {
                            item.Peripherals.Keyboards.Add(name);
                        }
                        else if (type.Equals("mouse", StringComparison.OrdinalIgnoreCase) || type.Equals("pointing", StringComparison.OrdinalIgnoreCase))
                        {
                            item.Peripherals.Mouses.Add(name);
                        }
                        else if (type.Equals("audio", StringComparison.OrdinalIgnoreCase) || name.Contains("speaker", StringComparison.OrdinalIgnoreCase))
                        {
                            item.Peripherals.Speakers.Add(name);
                        }
                        else if (name.Contains("microphone", StringComparison.OrdinalIgnoreCase) || name.Contains("mic", StringComparison.OrdinalIgnoreCase))
                        {
                            item.Peripherals.Microphones.Add(name);
                        }

                        if (dev.ConnectionType != null && dev.ConnectionType.Contains("USB", StringComparison.OrdinalIgnoreCase))
                        {
                            totalUsbDevices++;
                            item.Peripherals.UsbDevices.Add(name);
                        }
                    }
                }

                // Peripheral audit alerts
                if (!hasPrinter)
                {
                    reportData.MissingDevices.Add(new MissingDeviceItem { MachineName = item.MachineName, Detail = "Missing Printer" });
                }
                if (!hasWebcam)
                {
                    reportData.MissingDevices.Add(new MissingDeviceItem { MachineName = item.MachineName, Detail = "Missing Webcam" });
                }
                if (status != null && status.BatteryIsPresent != true && laptopCount > 0)
                {
                    // Laptop missing battery alert (if expected)
                    if (m.Hostname != null && (m.Hostname.Contains("lap", StringComparison.OrdinalIgnoreCase) || m.Hostname.Contains("note", StringComparison.OrdinalIgnoreCase)))
                    {
                        reportData.MissingDevices.Add(new MissingDeviceItem { MachineName = item.MachineName, Detail = "Missing Battery" });
                    }
                }

                // Add to list
                reportData.Systems.Add(item);

                // Individual recommendations from hardware specs
                if (hw != null && hw.TotalRamBytes.HasValue && hw.TotalRamBytes.Value < 8589934592L) // Less than 8 GB RAM
                {
                    recommendations.Add($"Increase RAM capacity on machine {item.MachineName} (currently has only {item.Ram.TotalCapacity}).");
                }
                if (disks.Any(d => d.DriveType != null && d.DriveType.Contains("HDD", StringComparison.OrdinalIgnoreCase)))
                {
                    recommendations.Add($"Replace primary system HDD with SSD on machine {item.MachineName} to increase productivity.");
                }
                if (m.OperatingSystem != null && (m.OperatingSystem.Contains("Windows 7", StringComparison.OrdinalIgnoreCase) || m.OperatingSystem.Contains("Windows 8", StringComparison.OrdinalIgnoreCase)))
                {
                    recommendations.Add($"Upgrade Windows operating system on machine {item.MachineName} to a supported version.");
                }
            }

            // Summaries mapping
            reportData.TotalCpus = totalCpus;
            reportData.TotalRamGb = Math.Round(totalRamBytes / (1024.0 * 1024 * 1024), 1);
            reportData.TotalStorageGb = (double)Math.Round(totalStorageCapacityGb, 1);
            reportData.TotalSsds = totalSsds;
            reportData.TotalHdds = totalHdds;
            reportData.TotalPrinters = totalPrinters;
            reportData.TotalMonitors = totalMonitors;
            reportData.TotalNetworkAdapters = totalNetworkAdapters;
            reportData.TotalGpus = totalGpus;
            reportData.TotalBatteries = totalBatteries;
            reportData.TotalUsbDevices = totalUsbDevices;

            reportData.LaptopCount = laptopCount;
            reportData.DesktopCount = desktopCount;

            reportData.CpuBrandDistribution = cpuBrands;
            reportData.GpuBrandDistribution = gpuBrands;

            // Load replaced components from Change History
            foreach (var chg in changes)
            {
                var machineObj = machines.FirstOrDefault(m => m.Id == chg.MachineId);
                if (machineObj == null) continue;

                var desc = chg.Description ?? string.Empty;
                var detailType = string.Empty;

                if (desc.Contains("RAM", StringComparison.OrdinalIgnoreCase) || desc.Contains("Memory", StringComparison.OrdinalIgnoreCase))
                {
                    detailType = "RAM Replaced";
                }
                else if (desc.Contains("SSD", StringComparison.OrdinalIgnoreCase) || desc.Contains("HDD", StringComparison.OrdinalIgnoreCase) || desc.Contains("Disk", StringComparison.OrdinalIgnoreCase))
                {
                    detailType = "SSD/HDD Replaced";
                }
                else if (desc.Contains("Motherboard", StringComparison.OrdinalIgnoreCase) || desc.Contains("Board", StringComparison.OrdinalIgnoreCase))
                {
                    detailType = "Motherboard Changed";
                }
                else if (desc.Contains("CPU", StringComparison.OrdinalIgnoreCase) || desc.Contains("Processor", StringComparison.OrdinalIgnoreCase))
                {
                    detailType = "CPU Changed";
                }
                else if (desc.Contains("GPU", StringComparison.OrdinalIgnoreCase) || desc.Contains("Graphics", StringComparison.OrdinalIgnoreCase) || desc.Contains("Video", StringComparison.OrdinalIgnoreCase))
                {
                    detailType = "GPU Changed";
                }
                else if (desc.Contains("Monitor", StringComparison.OrdinalIgnoreCase) || desc.Contains("Display", StringComparison.OrdinalIgnoreCase))
                {
                    detailType = "Monitor Changed";
                }
                else if (desc.Contains("Printer", StringComparison.OrdinalIgnoreCase))
                {
                    detailType = "Printer Changed";
                }

                if (!string.IsNullOrEmpty(detailType))
                {
                    reportData.ReplacedComponents.Add(new ReplacedComponentItem
                    {
                        MachineName = machineObj.Hostname ?? machineObj.DeviceName ?? "System",
                        Detail = $"{detailType}: {desc}",
                        DetectedAt = chg.DetectedAt.ToString("yyyy-MM-dd HH:mm")
                    });
                }
            }

            // General recommendations fallback
            if (!recommendations.Any())
            {
                recommendations.Add("Regularly run agent inventory sweeps to maintain an up-to-date ITAM database.");
                recommendations.Add("Perform security updates and bios driver checkups weekly.");
            }

            reportData.Recommendations = recommendations.Distinct().ToList();

            return reportData;
        }

        public byte[] GenerateAssetInventoryPdf(AssetInventoryReportData data)
        {
            try
            {
                var doc = new AssetInventoryReportDocument(data);
                return doc.GeneratePdf();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compile Asset Inventory QuestPDF report document");
                throw;
            }
        }

        private string ExtractCpuGeneration(string cpuModel)
        {
            if (string.IsNullOrEmpty(cpuModel)) return "Not Available";
            
            // Basic regex or check for Intel core generation
            if (cpuModel.Contains("Intel", StringComparison.OrdinalIgnoreCase) && cpuModel.Contains("Core", StringComparison.OrdinalIgnoreCase))
            {
                var index = cpuModel.IndexOf("i3-", StringComparison.OrdinalIgnoreCase);
                if (index == -1) index = cpuModel.IndexOf("i5-", StringComparison.OrdinalIgnoreCase);
                if (index == -1) index = cpuModel.IndexOf("i7-", StringComparison.OrdinalIgnoreCase);
                if (index == -1) index = cpuModel.IndexOf("i9-", StringComparison.OrdinalIgnoreCase);

                if (index != -1 && index + 7 <= cpuModel.Length)
                {
                    var genStr = cpuModel.Substring(index + 3, 2);
                    if (int.TryParse(genStr, out var genNum))
                    {
                        return $"{genNum}th Gen";
                    }
                    else if (int.TryParse(genStr.Substring(0, 1), out var singleGen))
                    {
                        return $"{singleGen}th Gen";
                    }
                }
            }
            else if (cpuModel.Contains("Ryzen", StringComparison.OrdinalIgnoreCase))
            {
                var parts = cpuModel.Split(' ');
                foreach (var p in parts)
                {
                    if (p.Length >= 4 && int.TryParse(p.Substring(0, 4), out var ryzenSeries))
                    {
                        var genNum = ryzenSeries / 1000;
                        return $"Ryzen Gen {genNum}";
                    }
                }
            }
            return "Not Available";
        }
    }
}
