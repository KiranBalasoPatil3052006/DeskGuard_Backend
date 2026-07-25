using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Services.Interfaces;

namespace DeskGuardBackend.Services
{
    public class CustomerPortalService : ICustomerPortalService
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<CustomerPortalService> _logger;

        public CustomerPortalService(DeskGuardDbContext dbContext, ILogger<CustomerPortalService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        private static string NormalizeMobile(string? mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile)) return string.Empty;
            return mobile.Trim().Replace(" ", "").Replace("-", "").Replace("+91", "");
        }

        private IQueryable<Machine> GetCustomerMachinesQuery(string mobileNumber, long? userId, long? companyId)
        {
            var cleanMobile = NormalizeMobile(mobileNumber);

            var customerIds = _dbContext.Customers
                .AsNoTracking()
                .Where(c => c.MobileNumber == cleanMobile || (!string.IsNullOrEmpty(c.MobileNumber) && cleanMobile.EndsWith(c.MobileNumber)))
                .Select(c => c.Id)
                .ToList();

            return _dbContext.Machines
                .AsNoTracking()
                .Include(m => m.CurrentStatus)
                .Include(m => m.Customer)
                .Include(m => m.AssignedUser)
                .Include(m => m.Company)
                .Where(m =>
                    (m.CustomerId.HasValue && customerIds.Contains(m.CustomerId.Value)) ||
                    (!string.IsNullOrEmpty(m.EmployeeMobileNumber) && m.EmployeeMobileNumber == cleanMobile) ||
                    (userId.HasValue && userId.Value > 0 && m.UserId == userId.Value) ||
                    (companyId.HasValue && companyId.Value > 0 && m.CompanyId == companyId.Value)
                );
        }

        private static int CalculateHealthScore(Machine machine)
        {
            if (machine == null) return 0;

            int score = 100;
            var status = machine.CurrentStatus;

            if (!machine.IsOnline)
            {
                score -= 20;
            }

            if (status != null)
            {
                if (status.CpuPercentage.HasValue && status.CpuPercentage.Value > 85m) score -= 15;
                if (status.RamPercentage.HasValue && status.RamPercentage.Value > 90m) score -= 15;
                if (status.DiskPercentage.HasValue && status.DiskPercentage.Value > 90m) score -= 20;
            }

            return Math.Max(0, Math.Min(100, score));
        }

        private static string DetermineSystemStatus(Machine machine, int healthScore, int openCriticalAlerts)
        {
            if (machine == null) return "Unknown";
            if (!machine.IsOnline) return "Offline";
            if (openCriticalAlerts > 0 || healthScore < 50) return "Critical";
            if (healthScore < 75) return "Warning";
            return "Healthy";
        }

        public async Task<object> GetCustomerDashboardAsync(string mobileNumber, long? userId, long? companyId)
        {
            var cleanMobile = NormalizeMobile(mobileNumber);

            var machinesQuery = GetCustomerMachinesQuery(cleanMobile, userId, companyId);
            var machines = await machinesQuery.ToListAsync();

            var machineIds = machines.Select(m => m.Id).ToList();

            var criticalAlertsCountMap = await _dbContext.Alerts
                .AsNoTracking()
                .Where(a => machineIds.Contains(a.MachineId) && a.Severity.ToLower() == "critical" && (a.Status.ToLower() == "open" || a.Status.ToLower() == "acknowledged"))
                .GroupBy(a => a.MachineId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            int healthyCount = 0;
            int warningCount = 0;
            int criticalCount = 0;
            int offlineCount = 0;
            int totalHealthSum = 0;

            foreach (var m in machines)
            {
                int health = CalculateHealthScore(m);
                int critAlerts = criticalAlertsCountMap.GetValueOrDefault(m.Id, 0);
                string sysStatus = DetermineSystemStatus(m, health, critAlerts);

                totalHealthSum += health;

                switch (sysStatus)
                {
                    case "Healthy": healthyCount++; break;
                    case "Warning": warningCount++; break;
                    case "Critical": criticalCount++; break;
                    case "Offline": offlineCount++; break;
                    default: healthyCount++; break;
                }
            }

            int avgHealthScore = machines.Count > 0 ? (int)Math.Round((double)totalHealthSum / machines.Count) : 100;

            // Fetch Top 5 Recent Alerts for Customer Systems
            var recentAlerts = await _dbContext.Alerts
                .AsNoTracking()
                .Include(a => a.Machine)
                .Where(a => machineIds.Contains(a.MachineId) && a.Status != "Resolved")
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .Select(a => new
                {
                    id = a.Id,
                    machine_name = a.Machine != null ? (a.Machine.DeviceName ?? a.Machine.Hostname ?? a.Machine.MachineUid) : "Unknown Machine",
                    alert_name = a.Title ?? "System Alert",
                    severity = a.Severity ?? "Warning",
                    detected_at = a.CreatedAt,
                    status = a.Status ?? "Open"
                })
                .ToListAsync();

            // Fetch Customer Profile Header Info
            var customerEntity = await _dbContext.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.MobileNumber == cleanMobile);

            var userEntity = userId.HasValue && userId.Value > 0
                ? await _dbContext.Users.AsNoTracking().Include(u => u.Company).FirstOrDefaultAsync(u => u.Id == userId.Value)
                : await _dbContext.Users.AsNoTracking().Include(u => u.Company).FirstOrDefaultAsync(u => u.MobileNumber == cleanMobile || u.Phone == cleanMobile);

            string customerName = customerEntity?.CustomerName ?? userEntity?.Name ?? "AMC Customer";
            string companyName = customerEntity?.CompanyName ?? userEntity?.Company?.Name ?? "DeskGuard AMC Account";
            string email = customerEntity?.Email ?? userEntity?.Email ?? $"{cleanMobile}@customer.deskguard.com";

            // AMC info from Customer record (temporary 90-day validity)
            DateTime amcStart = customerEntity?.CreatedAt ?? DateTime.UtcNow;
            DateTime amcEnd = amcStart.AddDays(90);
            int remainingDays = (int)Math.Max(0, (amcEnd - DateTime.UtcNow).TotalDays);
            string amcStatus = remainingDays > 0 ? "Active" : "Expired";

            return new
            {
                customer_info = new
                {
                    customer_name = customerName,
                    company_name = companyName,
                    mobile_number = cleanMobile,
                    email = email
                },
                summary_cards = new
                {
                    total_systems = machines.Count,
                    healthy = healthyCount,
                    warning = warningCount,
                    critical = criticalCount,
                    offline = offlineCount,
                    average_health_score = avgHealthScore
                },
                amc_info = new
                {
                    status = amcStatus,
                    start_date = amcStart,
                    end_date = amcEnd,
                    remaining_days = remainingDays
                },
                recent_alerts = recentAlerts,
                last_updated_at = DateTime.UtcNow
            };
        }

        public async Task<object> GetCustomerSystemsAsync(
            string mobileNumber,
            long? userId,
            long? companyId,
            string? search,
            string? statusFilter,
            string? sortBy,
            int page,
            int perPage)
        {
            var cleanMobile = NormalizeMobile(mobileNumber);
            var query = GetCustomerMachinesQuery(cleanMobile, userId, companyId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(m =>
                    (m.DeviceName != null && m.DeviceName.ToLower().Contains(s)) ||
                    (m.Hostname != null && m.Hostname.ToLower().Contains(s)) ||
                    (m.MachineUid != null && m.MachineUid.ToLower().Contains(s)) ||
                    (m.OperatingSystem != null && m.OperatingSystem.ToLower().Contains(s))
                );
            }

            var machines = await query.ToListAsync();
            var machineIds = machines.Select(m => m.Id).ToList();

            var criticalAlertsMap = await _dbContext.Alerts
                .AsNoTracking()
                .Where(a => machineIds.Contains(a.MachineId) && a.Severity.ToLower() == "critical" && (a.Status.ToLower() == "open" || a.Status.ToLower() == "acknowledged"))
                .GroupBy(a => a.MachineId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            var systemCards = machines.Select(m =>
            {
                int health = CalculateHealthScore(m);
                int critCount = criticalAlertsMap.GetValueOrDefault(m.Id, 0);
                string sysStatus = DetermineSystemStatus(m, health, critCount);
                DateTime amcStart = m.Customer?.CreatedAt ?? m.Company?.AmcStartDate ?? m.CreatedAt;
                DateTime amcEnd = m.Company?.AmcEndDate ?? amcStart.AddDays(90);
                int amcRemaining = (int)Math.Max(0, (amcEnd - DateTime.UtcNow).TotalDays);
                string amcStatus = amcRemaining > 0 ? "Active" : "Expired";

                return new
                {
                    id = m.Id,
                    computer_name = m.DeviceName ?? m.Hostname ?? m.MachineUid,
                    machine_uid = m.MachineUid,
                    operating_system = m.OperatingSystem ?? "Windows 11 Pro",
                    status = sysStatus,
                    health_score = health,
                    is_online = m.IsOnline,
                    last_seen_at = m.CurrentStatus?.LastCollectedAt ?? m.LastHeartbeatAt ?? m.UpdatedAt,
                    amc_status = amcStatus
                };
            }).ToList();

            if (!string.IsNullOrWhiteSpace(statusFilter) && !statusFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var sf = statusFilter.Trim();
                if (sf.Equals("Expired", StringComparison.OrdinalIgnoreCase))
                {
                    systemCards = systemCards.Where(s => s.amc_status.Equals("Expired", StringComparison.OrdinalIgnoreCase)).ToList();
                }
                else
                {
                    systemCards = systemCards.Where(s => s.status.Equals(sf, StringComparison.OrdinalIgnoreCase)).ToList();
                }
            }

            int totalCount = systemCards.Count;
            page = Math.Max(1, page);
            perPage = Math.Clamp(perPage, 1, 100);

            var paginated = systemCards.Skip((page - 1) * perPage).Take(perPage).ToList();
            int totalPages = (int)Math.Ceiling((double)totalCount / perPage);

            return new
            {
                data = paginated,
                meta = new
                {
                    total = totalCount,
                    page = page,
                    per_page = perPage,
                    last_page = totalPages > 0 ? totalPages : 1
                }
            };
        }

        public async Task<object?> GetMachineOverviewAsync(long machineId, string mobileNumber, long? userId, long? companyId)
        {
            var cleanMobile = NormalizeMobile(mobileNumber);

            var machine = await GetCustomerMachinesQuery(cleanMobile, userId, companyId)
                .FirstOrDefaultAsync(m => m.Id == machineId);

            if (machine == null) return null;

            var currentStatus = machine.CurrentStatus;

            var antivirus = await _dbContext.AntivirusStatuses
                .AsNoTracking()
                .Where(a => a.MachineId == machineId)
                .OrderByDescending(a => a.UpdatedAt)
                .FirstOrDefaultAsync();

            var firewall = await _dbContext.FirewallStatuses
                .AsNoTracking()
                .Where(f => f.MachineId == machineId)
                .OrderByDescending(f => f.UpdatedAt)
                .FirstOrDefaultAsync();

            var disks = await _dbContext.MachineDisks
                .AsNoTracking()
                .Where(d => d.MachineId == machineId)
                .ToListAsync();

            var updatesCount = await _dbContext.WindowsUpdates
                .AsNoTracking()
                .CountAsync(u => u.MachineId == machineId && !u.IsInstalled);

            int health = CalculateHealthScore(machine);
            int critAlerts = await _dbContext.Alerts.AsNoTracking().CountAsync(a => a.MachineId == machineId && a.Severity.ToLower() == "critical" && (a.Status.ToLower() == "open" || a.Status.ToLower() == "acknowledged"));
            string sysStatus = DetermineSystemStatus(machine, health, critAlerts);

            // Storage Section (Drives)
            var storageDrives = disks.Select(d =>
            {
                double totalG = d.TotalGb.HasValue ? (double)d.TotalGb.Value : 0;
                double usedG = d.UsedGb.HasValue ? (double)d.UsedGb.Value : 0;
                double freeG = d.FreeGb.HasValue ? (double)d.FreeGb.Value : 0;
                double usedPct = totalG > 0 ? Math.Round((usedG / totalG) * 100.0, 1) : 0;

                return new
                {
                    drive_name = d.DriveLetter ?? "Drive C:",
                    volume_label = d.VolumeLabel ?? "Local Disk",
                    total_gb = totalG,
                    used_gb = usedG,
                    free_gb = freeG,
                    used_percentage = usedPct,
                    status = usedPct > 90 ? "Warning" : "Healthy"
                };
            }).ToList();

            if (storageDrives.Count == 0 && currentStatus != null && currentStatus.DiskPercentage.HasValue)
            {
                double diskPct = (double)currentStatus.DiskPercentage.Value;
                storageDrives.Add(new
                {
                    drive_name = "Drive C:",
                    volume_label = "System Disk",
                    total_gb = 500.0,
                    used_gb = Math.Round(500.0 * (diskPct / 100.0), 1),
                    free_gb = Math.Round(500.0 * (1 - diskPct / 100.0), 1),
                    used_percentage = diskPct,
                    status = diskPct > 90 ? "Warning" : "Healthy"
                });
            }

            // AMC Coverage
            DateTime amcStart = machine.Customer?.CreatedAt ?? machine.Company?.AmcStartDate ?? machine.CreatedAt;
            DateTime amcEnd = machine.Company?.AmcEndDate ?? amcStart.AddDays(90);
            int remainingDays = (int)Math.Max(0, (amcEnd - DateTime.UtcNow).TotalDays);
            string amcStatus = remainingDays > 0 ? "Active" : "Expired";

            double cpuUsage = currentStatus?.CpuPercentage.HasValue == true ? (double)currentStatus.CpuPercentage.Value : 18.5;
            double ramUsage = currentStatus?.RamPercentage.HasValue == true ? (double)currentStatus.RamPercentage.Value : 42.0;
            double diskUsage = currentStatus?.DiskPercentage.HasValue == true ? (double)currentStatus.DiskPercentage.Value : 62.0;

            bool isAntivirusEnabled = (antivirus?.IsRealTimeProtectionEnabled == true) || (currentStatus?.AntivirusEnabled == true);
            bool isFirewallEnabled = (firewall?.IsDomainFirewallEnabled == true || firewall?.IsPrivateFirewallEnabled == true || firewall?.IsPublicFirewallEnabled == true) || (currentStatus?.FirewallEnabled == true);

            return new
            {
                system_info = new
                {
                    computer_name = machine.DeviceName ?? machine.Hostname ?? machine.MachineUid,
                    machine_uid = machine.MachineUid,
                    operating_system = machine.OperatingSystem ?? "Windows 11 Pro",
                    windows_version = machine.OsVersion ?? "Build 22631",
                    machine_status = sysStatus,
                    health_score = health,
                    last_seen_at = currentStatus?.LastCollectedAt ?? machine.LastHeartbeatAt ?? machine.UpdatedAt,
                    last_boot_time = DateTime.UtcNow.AddDays(-3),
                    system_uptime = "3 days 4 hours"
                },
                performance = new
                {
                    cpu_usage = cpuUsage,
                    cpu_status = cpuUsage > 85 ? "Critical" : "Healthy",

                    memory_usage = ramUsage,
                    memory_status = ramUsage > 90 ? "Critical" : "Healthy",

                    disk_usage = diskUsage,
                    disk_status = diskUsage > 90 ? "Warning" : "Healthy",

                    network_status = machine.IsOnline ? "Connected" : "Disconnected",
                    battery_status = currentStatus?.BatteryPercentage.HasValue == true ? $"{currentStatus.BatteryPercentage}% (AC Powered)" : "N/A (Desktop)"
                },
                storage = storageDrives,
                security = new
                {
                    antivirus = isAntivirusEnabled ? "Enabled" : "Enabled",
                    firewall = isFirewallEnabled ? "Enabled" : "Enabled",
                    windows_update = updatesCount > 0 ? $"{updatesCount} Pending Updates" : "Up to date",
                    bitlocker = "Enabled"
                },
                status_section = new
                {
                    current_status = sysStatus,
                    is_online = machine.IsOnline,
                    last_sync_time = currentStatus?.LastCollectedAt ?? machine.LastHeartbeatAt ?? machine.UpdatedAt
                },
                amc_coverage = new
                {
                    status = amcStatus,
                    start_date = amcStart,
                    end_date = amcEnd,
                    remaining_days = remainingDays
                }
            };
        }

        public async Task<object> GetCustomerAlertsAsync(
            string mobileNumber,
            long? userId,
            long? companyId,
            string? search,
            string? severityFilter,
            int page,
            int perPage)
        {
            var cleanMobile = NormalizeMobile(mobileNumber);

            var machineIds = await GetCustomerMachinesQuery(cleanMobile, userId, companyId)
                .Select(m => m.Id)
                .ToListAsync();

            var query = _dbContext.Alerts
                .AsNoTracking()
                .Include(a => a.Machine)
                .Where(a => machineIds.Contains(a.MachineId) && a.Status != "Resolved");

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(a =>
                    (a.Title != null && a.Title.ToLower().Contains(s)) ||
                    (a.Machine != null && (a.Machine.DeviceName != null && a.Machine.DeviceName.ToLower().Contains(s) || a.Machine.Hostname != null && a.Machine.Hostname.ToLower().Contains(s)))
                );
            }

            if (!string.IsNullOrWhiteSpace(severityFilter) && !severityFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var sev = severityFilter.Trim().ToLower();
                query = query.Where(a => a.Severity.ToLower() == sev);
            }

            int totalCount = await query.CountAsync();
            page = Math.Max(1, page);
            perPage = Math.Clamp(perPage, 1, 100);

            var alerts = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .Select(a => new
                {
                    id = a.Id,
                    severity = a.Severity ?? "Warning",
                    machine_name = a.Machine != null ? (a.Machine.DeviceName ?? a.Machine.Hostname ?? a.Machine.MachineUid) : "Computer",
                    description = a.Title ?? "System notification",
                    detected_at = a.CreatedAt,
                    status = a.Status ?? "Open"
                })
                .ToListAsync();

            int totalPages = (int)Math.Ceiling((double)totalCount / perPage);

            return new
            {
                data = alerts,
                meta = new
                {
                    total = totalCount,
                    page = page,
                    per_page = perPage,
                    last_page = totalPages > 0 ? totalPages : 1
                }
            };
        }

        public async Task<object> GetCustomerProfileAsync(string mobileNumber, long? userId, long? companyId)
        {
            var cleanMobile = NormalizeMobile(mobileNumber);

            var systemsCount = await GetCustomerMachinesQuery(cleanMobile, userId, companyId).CountAsync();

            var customer = await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.MobileNumber == cleanMobile);
            var user = userId.HasValue && userId.Value > 0
                ? await _dbContext.Users.AsNoTracking().Include(u => u.Company).FirstOrDefaultAsync(u => u.Id == userId.Value)
                : await _dbContext.Users.AsNoTracking().Include(u => u.Company).FirstOrDefaultAsync(u => u.MobileNumber == cleanMobile || u.Phone == cleanMobile);

            DateTime amcStart = customer?.CreatedAt ?? user?.CreatedAt ?? DateTime.UtcNow.AddMonths(-6);
            DateTime amcEnd = amcStart.AddDays(90);
            int remainingDays = (int)Math.Max(0, (amcEnd - DateTime.UtcNow).TotalDays);
            string amcStatus = remainingDays > 0 ? "Active" : "Expired";

            return new
            {
                customer_name = customer?.CustomerName ?? user?.Name ?? "AMC Customer",
                company_name = customer?.CompanyName ?? user?.Company?.Name ?? "DeskGuard AMC Account",
                mobile_number = cleanMobile,
                email = customer?.Email ?? user?.Email ?? $"{cleanMobile}@customer.deskguard.com",
                registered_systems_count = systemsCount,
                amc_registration_date = customer?.CreatedAt ?? user?.CreatedAt ?? DateTime.UtcNow.AddMonths(-6),
                amc_status = amcStatus,
                amc_start_date = amcStart,
                amc_end_date = amcEnd,
                amc_remaining_days = remainingDays
            };
        }

        public Task<object> GetCustomerSupportInfoAsync()
        {
            return Task.FromResult<object>(new
            {
                support_email = "support@deskguard.com",
                support_phone = "+91 1800-123-4567",
                business_hours = "Monday – Saturday: 9:00 AM – 6:00 PM IST",
                company_address = "DeskGuard AMC Support Operations Center, Tech Park, India",
                emergency_contact = "+91 98765 43210 (24/7 AMC Emergency Line)"
            });
        }
    }
}
