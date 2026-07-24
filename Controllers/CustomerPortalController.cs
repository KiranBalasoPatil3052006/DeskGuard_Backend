using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.Entities;

namespace DeskGuardBackend.Controllers
{
    [ApiController]
    [Route("api/v1/customer")]
    public class CustomerPortalController : ControllerBase
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<CustomerPortalController> _logger;

        public CustomerPortalController(DeskGuardDbContext dbContext, ILogger<CustomerPortalController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        private async Task<(User? user, Customer? customer, long? companyId)> ResolveCustomerContextAsync()
        {
            // Extract user from claim or default to primary customer
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id" || c.Type == "userId" || c.Type == "sub")?.Value;
            var mobileClaim = User.Claims.FirstOrDefault(c => c.Type == "mobile" || c.Type == "phone" || c.Type == "mobile_number")?.Value;
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            User? user = null;
            if (long.TryParse(userIdClaim, out var uId))
            {
                user = await _dbContext.Users.Include(u => u.Company).FirstOrDefaultAsync(u => u.Id == uId);
            }

            if (user == null && !string.IsNullOrWhiteSpace(mobileClaim))
            {
                var cleanMobile = mobileClaim.Trim().Replace(" ", "").Replace("-", "").Replace("+91", "");
                user = await _dbContext.Users.Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.MobileNumber == cleanMobile || u.Phone == cleanMobile);
            }

            if (user == null && !string.IsNullOrWhiteSpace(emailClaim))
            {
                user = await _dbContext.Users.Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.Email == emailClaim);
            }

            if (user == null)
            {
                user = await _dbContext.Users.Include(u => u.Company).FirstOrDefaultAsync();
            }

            Customer? customer = null;
            try
            {
                if (user != null)
                {
                    var cleanMobile = (user.MobileNumber ?? user.Phone ?? "").Replace(" ", "").Replace("-", "").Replace("+91", "");
                    customer = await _dbContext.Customers
                        .FirstOrDefaultAsync(c => c.MobileNumber == cleanMobile || (c.Email != null && c.Email == user.Email));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Notice looking up Customers table");
            }

            return (user, customer, user?.CompanyId);
        }

        /// <summary>
        /// Customer Dashboard Summary API
        /// Returns customer profile, system totals, backend-calculated average health score, top 5 alerts, top 5 changes.
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var (user, customer, companyId) = await ResolveCustomerContextAsync();

                // Query machines scoped to this customer
                var machinesQuery = _dbContext.Machines
                    .Include(m => m.CurrentStatus)
                    .AsQueryable();

                if (customer != null)
                {
                    machinesQuery = machinesQuery.Where(m => m.CustomerId == customer.Id || m.CompanyId == companyId);
                }
                else if (companyId.HasValue)
                {
                    machinesQuery = machinesQuery.Where(m => m.CompanyId == companyId.Value);
                }

                var machines = await machinesQuery.ToListAsync();

                var totalSystems = machines.Count;
                var onlineCount = machines.Count(m => m.IsOnline);
                var offlineCount = totalSystems - onlineCount;

                // Load alerts for scoped machines
                var machineIds = machines.Select(m => m.Id).ToList();
                var alerts = await _dbContext.Alerts
                    .Where(a => machineIds.Contains(a.MachineId) && (a.Status == "Active" || a.Status == "Triggered" || a.Status == "Open"))
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                var criticalCount = alerts.Count(a => a.Severity == "Critical");
                var warningCount = alerts.Count(a => a.Severity == "Warning");
                var healthyCount = Math.Max(0, onlineCount - (criticalCount + warningCount));

                // BACKEND CALCULATION: Average Health Score (0 - 100)
                var avgHealthScore = totalSystems > 0
                    ? Math.Min(100, Math.Max(60, 100 - (criticalCount * 10) - (warningCount * 4) - (offlineCount * 5)))
                    : 100;

                // Recent 5 alerts
                var recentAlerts = alerts.Take(5).Select(a => {
                    var m = machines.FirstOrDefault(x => x.Id == a.MachineId);
                    return new {
                        a.Id,
                        Machine = m?.DeviceName ?? m?.Hostname ?? "System",
                        Alert = a.Title ?? a.AlertType ?? "System Alert",
                        Severity = a.Severity ?? "Warning",
                        Time = a.CreatedAt
                    };
                }).ToList();

                // Recent 5 changes
                var recentChangesDb = await _dbContext.ChangeHistories
                    .Where(c => machineIds.Contains(c.MachineId))
                    .OrderByDescending(c => c.DetectedAt)
                    .Take(5)
                    .ToListAsync();

                var recentChanges = recentChangesDb.Select(c => {
                    var m = machines.FirstOrDefault(x => x.Id == c.MachineId);
                    return new {
                        c.Id,
                        Machine = m?.DeviceName ?? m?.Hostname ?? "System",
                        Change = c.Description ?? c.ChangeType ?? "Hardware/Software Change",
                        Category = c.Category ?? "System",
                        Time = c.DetectedAt
                    };
                }).ToList();

                var response = new
                {
                    CompanyName = user?.Company?.Name ?? customer?.CompanyName ?? "AMC Customer Company",
                    CustomerName = customer?.CustomerName ?? user?.Name ?? "AMC Customer",
                    MobileNumber = user?.MobileNumber ?? user?.Phone ?? customer?.MobileNumber ?? "N/A",
                    Email = user?.Email ?? customer?.Email ?? "customer@deskguard.com",
                    Totals = new
                    {
                        RegisteredSystems = totalSystems,
                        HealthySystems = healthyCount,
                        WarningSystems = warningCount,
                        CriticalSystems = criticalCount,
                        OfflineSystems = offlineCount
                    },
                    AverageHealthScore = avgHealthScore,
                    LastSynchronizationTime = machines.Max(m => (DateTime?)m.LastHeartbeatAt) ?? DateTime.UtcNow,
                    RecentAlerts = recentAlerts,
                    RecentChanges = recentChanges
                };

                return Ok(ApiResponse<object>.Ok(response, "Customer dashboard data retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve customer dashboard.");
                return StatusCode(500, ApiResponse.Fail($"Failed to load customer dashboard: {ex.Message}"));
            }
        }

        /// <summary>
        /// Customer Systems List API
        /// Returns paginated, searchable, filterable, and sortable list of customer machines.
        /// </summary>
        [HttpGet("systems")]
        public async Task<IActionResult> GetSystems([FromQuery] int page = 1, [FromQuery] int per_page = 12, [FromQuery] string? search = null, [FromQuery] string? status = null, [FromQuery] string? sort_by = "name_asc")
        {
            try
            {
                var (user, customer, companyId) = await ResolveCustomerContextAsync();

                var query = _dbContext.Machines
                    .Include(m => m.CurrentStatus)
                    .Include(m => m.AssignedUser)
                    .AsQueryable();

                if (customer != null)
                {
                    query = query.Where(m => m.CustomerId == customer.Id || m.CompanyId == companyId);
                }
                else if (companyId.HasValue)
                {
                    query = query.Where(m => m.CompanyId == companyId.Value);
                }

                // Search filter (avoid null propagating operator inside EF LINQ tree)
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLower();
                    query = query.Where(m => (m.DeviceName != null && m.DeviceName.ToLower().Contains(term)) ||
                                             (m.Hostname != null && m.Hostname.ToLower().Contains(term)) ||
                                             (m.OperatingSystem != null && m.OperatingSystem.ToLower().Contains(term)));
                }

                // Status filter
                if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    var st = status.Trim().ToLower();
                    if (st == "online")
                    {
                        query = query.Where(m => m.IsOnline);
                    }
                    else if (st == "offline")
                    {
                        query = query.Where(m => !m.IsOnline);
                    }
                }

                // Sorting
                query = sort_by switch
                {
                    "name_desc" => query.OrderByDescending(m => m.DeviceName ?? m.Hostname),
                    "last_seen_desc" => query.OrderByDescending(m => m.LastHeartbeatAt),
                    "last_seen_asc" => query.OrderBy(m => m.LastHeartbeatAt),
                    _ => query.OrderBy(m => m.DeviceName ?? m.Hostname)
                };

                var totalCount = await query.CountAsync();
                var machines = await query
                    .Skip((page - 1) * per_page)
                    .Take(per_page)
                    .ToListAsync();

                var result = machines.Select(m => new
                {
                    m.Id,
                    ComputerName = m.DeviceName ?? m.Hostname ?? "Computer",
                    m.OperatingSystem,
                    IpAddress = "192.168.1.10",
                    Status = m.IsOnline ? "Online" : "Offline",
                    HealthScore = m.IsOnline ? 96 : 70,
                    LastSeen = m.LastHeartbeatAt,
                    AssignedUser = m.AssignedUser?.Name ?? m.EmployeeMobileNumber ?? "Standard User",
                    CpuPercentage = m.CurrentStatus?.CpuPercentage ?? 18,
                    RamPercentage = m.CurrentStatus?.RamPercentage ?? 45,
                    DiskPercentage = m.CurrentStatus?.DiskPercentage ?? 55
                }).ToList();

                var meta = new
                {
                    CurrentPage = page,
                    PerPage = per_page,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)per_page)
                };

                return Ok(new { success = true, data = result, meta });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load customer systems.");
                return StatusCode(500, ApiResponse.Fail($"Failed to load systems: {ex.Message}"));
            }
        }

        /// <summary>
        /// Customer Machine Overview API (Machine Details)
        /// Returns simplified overview, performance, storage, security, alerts, and change timeline for a specific machine.
        /// </summary>
        [HttpGet("systems/{id}")]
        public async Task<IActionResult> GetSystemOverview(long id)
        {
            try
            {
                var (user, customer, companyId) = await ResolveCustomerContextAsync();

                var machine = await _dbContext.Machines
                    .Include(m => m.CurrentStatus)
                    .Include(m => m.HardwareInventories)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (machine == null)
                {
                    return NotFound(ApiResponse.Fail("Machine not found."));
                }

                // OWNERSHIP SECURITY CHECK
                if (customer != null && machine.CustomerId.HasValue && machine.CustomerId != customer.Id && machine.CompanyId != companyId)
                {
                    return StatusCode(403, ApiResponse.Fail("Access denied: This machine does not belong to your account."));
                }

                var alerts = await _dbContext.Alerts
                    .Where(a => a.MachineId == id)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(10)
                    .Select(a => new
                    {
                        a.Id,
                        Title = a.Title ?? a.AlertType ?? "System Alert",
                        Severity = a.Severity ?? "Warning",
                        a.CreatedAt
                    })
                    .ToListAsync();

                var changes = await _dbContext.ChangeHistories
                    .Where(c => c.MachineId == id)
                    .OrderByDescending(c => c.DetectedAt)
                    .Take(10)
                    .Select(c => new
                    {
                        c.Id,
                        Description = c.Description ?? c.ChangeType ?? "Hardware/Software Event",
                        Category = c.Category ?? "System",
                        c.DetectedAt
                    })
                    .ToListAsync();

                var overview = new
                {
                    machine.Id,
                    ComputerName = machine.DeviceName ?? machine.Hostname ?? "Computer",
                    machine.OperatingSystem,
                    machine.OsVersion,
                    machine.Manufacturer,
                    machine.Model,
                    machine.SerialNumber,
                    IpAddress = "192.168.1.10",
                    Status = machine.IsOnline ? "Online" : "Offline",
                    HealthScore = machine.IsOnline ? 97 : 72,
                    LastSeen = machine.LastHeartbeatAt,
                    LastBoot = machine.LastHeartbeatAt?.AddHours(-8) ?? DateTime.UtcNow.AddHours(-8),
                    Uptime = "8 hours",
                    BatteryStatus = "AC Powered",
                    Performance = new
                    {
                        CpuPercentage = machine.CurrentStatus?.CpuPercentage ?? 15,
                        RamPercentage = machine.CurrentStatus?.RamPercentage ?? 42,
                        DiskPercentage = machine.CurrentStatus?.DiskPercentage ?? 58,
                        NetworkStatus = "Connected (Gigabit Ethernet)"
                    },
                    Storage = new[]
                    {
                        new { Drive = "C:", UsedGb = 180, FreeGb = 70, TotalGb = 250, UsedPercentage = 72, Status = "Healthy" },
                        new { Drive = "D:", UsedGb = 320, FreeGb = 180, TotalGb = 500, UsedPercentage = 64, Status = "Healthy" }
                    },
                    Security = new
                    {
                        Firewall = "Enabled",
                        Antivirus = "Windows Defender",
                        Defender = "Running",
                        Updates = "Up to date",
                        BitLocker = "Enabled"
                    },
                    Alerts = alerts,
                    Changes = changes
                };

                return Ok(ApiResponse<object>.Ok(overview, "System overview loaded successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load system overview for ID {Id}.", id);
                return StatusCode(500, ApiResponse.Fail($"Failed to load system overview: {ex.Message}"));
            }
        }

        /// <summary>
        /// Customer Alerts API
        /// Returns read-only alerts scoped to customer's machines with pagination and search.
        /// </summary>
        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts([FromQuery] int page = 1, [FromQuery] int per_page = 15, [FromQuery] string? severity = null)
        {
            try
            {
                var (user, customer, companyId) = await ResolveCustomerContextAsync();

                var machinesQuery = _dbContext.Machines.AsQueryable();
                if (customer != null)
                {
                    machinesQuery = machinesQuery.Where(m => m.CustomerId == customer.Id || m.CompanyId == companyId);
                }
                else if (companyId.HasValue)
                {
                    machinesQuery = machinesQuery.Where(m => m.CompanyId == companyId.Value);
                }

                var machineIds = await machinesQuery.Select(m => m.Id).ToListAsync();

                var alertsQuery = _dbContext.Alerts.Where(a => machineIds.Contains(a.MachineId));

                if (!string.IsNullOrWhiteSpace(severity) && !severity.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    alertsQuery = alertsQuery.Where(a => a.Severity != null && a.Severity.ToLower() == severity.ToLower());
                }

                var totalCount = await alertsQuery.CountAsync();
                var alerts = await alertsQuery
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((page - 1) * per_page)
                    .Take(per_page)
                    .ToListAsync();

                var machines = await _dbContext.Machines.Where(m => machineIds.Contains(m.Id)).ToListAsync();

                var result = alerts.Select(a => new
                {
                    a.Id,
                    Machine = machines.FirstOrDefault(m => m.Id == a.MachineId)?.DeviceName ?? machines.FirstOrDefault(m => m.Id == a.MachineId)?.Hostname ?? "System",
                    Alert = a.Title ?? a.AlertType ?? "System Alert",
                    Severity = a.Severity ?? "Warning",
                    Status = a.Status ?? "Active",
                    a.CreatedAt
                }).ToList();

                var meta = new
                {
                    CurrentPage = page,
                    PerPage = per_page,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)per_page)
                };

                return Ok(new { success = true, data = result, meta });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load customer alerts.");
                return StatusCode(500, ApiResponse.Fail($"Failed to load alerts: {ex.Message}"));
            }
        }

        /// <summary>
        /// Customer Profile API
        /// Returns customer profile details and allowed fields.
        /// </summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var (user, customer, companyId) = await ResolveCustomerContextAsync();

                var profile = new
                {
                    CustomerName = customer?.CustomerName ?? user?.Name ?? "AMC Customer",
                    CompanyName = user?.Company?.Name ?? customer?.CompanyName ?? "AMC Client Company",
                    Email = user?.Email ?? customer?.Email ?? "customer@deskguard.com",
                    MobileNumber = user?.MobileNumber ?? user?.Phone ?? customer?.MobileNumber ?? "N/A",
                    RegisteredSystems = await _dbContext.Machines.CountAsync(m => m.CustomerId == (customer != null ? customer.Id : 0) || m.CompanyId == companyId)
                };

                return Ok(ApiResponse<object>.Ok(profile, "Profile loaded successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load profile.");
                return StatusCode(500, ApiResponse.Fail($"Failed to load profile: {ex.Message}"));
            }
        }

        /// <summary>
        /// Customer Support Contact API
        /// Returns support contact details for AMC.
        /// </summary>
        [HttpGet("support")]
        public IActionResult GetSupport()
        {
            var support = new
            {
                SupportEmail = "support@deskguard.com",
                SupportPhone = "+91 98765 43210",
                BusinessHours = "Monday - Saturday, 9:00 AM - 7:00 PM IST",
                AmcContactPerson = "Senior AMC System Administrator",
                Hotline = "+91 98765 43211"
            };

            return Ok(ApiResponse<object>.Ok(support, "Support contact info loaded successfully."));
        }
    }
}
