using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.DTOs.Customer;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Services.Interfaces;

namespace DeskGuardBackend.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly IMachineStatusService _statusService;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(
            DeskGuardDbContext dbContext,
            IMachineStatusService statusService,
            ILogger<CustomerService> logger)
        {
            _dbContext = dbContext;
            _statusService = statusService;
            _logger = logger;
        }

        private async Task AutoGroupUnassignedMachinesAsync()
        {
            try
            {
                var unassignedMachines = await _dbContext.Machines
                    .Include(m => m.Company)
                    .Where(m => m.CustomerId == null)
                    .ToListAsync();

                if (unassignedMachines.Count > 0)
                {
                    var existingCustomers = await _dbContext.Customers.ToListAsync();

                    foreach (var m in unassignedMachines)
                    {
                        var compName = (!string.IsNullOrWhiteSpace(m.Company?.Name) ? m.Company.Name : "Default Enterprise").Trim();
                        var mobNum = (!string.IsNullOrWhiteSpace(m.EmployeeMobileNumber) ? m.EmployeeMobileNumber : "").Trim();
                        var custName = compName;

                        var match = existingCustomers.FirstOrDefault(c =>
                            c.CompanyName.Equals(compName, StringComparison.OrdinalIgnoreCase) &&
                            c.MobileNumber == mobNum);

                        if (match == null)
                        {
                            match = new Customer
                            {
                                CustomerCode = $"CUST-{1001 + existingCustomers.Count}",
                                CompanyName = compName,
                                CustomerName = custName,
                                MobileNumber = mobNum,
                                Email = m.Company?.Email,
                                Status = "Active",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            await _dbContext.Customers.AddAsync(match);
                            await _dbContext.SaveChangesAsync();
                            existingCustomers.Add(match);
                        }

                        m.CustomerId = match.Id;
                    }
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AutoGroupUnassignedMachinesAsync warning during auto-migration.");
            }
        }

        public async Task<(List<CustomerDto> Items, int TotalCount)> GetCustomersAsync(
            string? search,
            string? sortBy,
            int page,
            int pageSize)
        {
            await AutoGroupUnassignedMachinesAsync();

            var query = _dbContext.Customers
                .Include(c => c.Machines)
                    .ThenInclude(m => m.CurrentStatus)
                .Include(c => c.Machines)
                    .ThenInclude(m => m.Alerts)
                .AsNoTracking();

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(c =>
                    c.CompanyName.ToLower().Contains(s) ||
                    c.CustomerName.ToLower().Contains(s) ||
                    c.MobileNumber.ToLower().Contains(s) ||
                    (c.Email != null && c.Email.ToLower().Contains(s)) ||
                    c.Machines.Any(m => (m.Hostname != null && m.Hostname.ToLower().Contains(s)) ||
                                        (m.DeviceName != null && m.DeviceName.ToLower().Contains(s)) ||
                                        m.MachineUid.ToLower().Contains(s)));
            }

            var totalCount = await query.CountAsync();

            // Fetch raw customers list
            var rawList = await query.ToListAsync();

            // Map and calculate aggregate metrics in memory
            var mappedList = rawList.Select(c =>
            {
                var totalSys = c.Machines.Count;
                var onlineSys = c.Machines.Count(m => _statusService.CalculateStatus(m) == "Online");
                var offlineSys = c.Machines.Count(m => _statusService.CalculateStatus(m) == "Offline");
                var sleepSys = c.Machines.Count(m => _statusService.CalculateStatus(m) == "Sleeping");
                var critAlerts = c.Machines.Sum(m => m.Alerts.Count(a => a.Severity != null && a.Severity.Equals("critical", StringComparison.OrdinalIgnoreCase) && a.Status != "resolved"));

                var validDates = c.Machines
                    .Where(m => m.LastHeartbeatAt.HasValue)
                    .Select(m => m.LastHeartbeatAt!.Value)
                    .ToList();

                DateTime lastAct = validDates.Count > 0 ? validDates.Max() : c.CreatedAt;

                return new CustomerDto
                {
                    Id = c.Id,
                    CustomerCode = c.CustomerCode,
                    CompanyName = c.CompanyName,
                    CustomerName = c.CustomerName,
                    MobileNumber = c.MobileNumber,
                    Email = c.Email,
                    Status = c.Status,
                    Remarks = c.Remarks,
                    TotalSystems = totalSys,
                    OnlineSystems = onlineSys,
                    OfflineSystems = offlineSys,
                    SleepingSystems = sleepSys,
                    CriticalAlertsCount = critAlerts,
                    LastActivityAt = lastAct,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                };
            });

            // Sorting
            IEnumerable<CustomerDto> sortedEnum = sortBy?.ToLower() switch
            {
                "company_desc" => mappedList.OrderByDescending(c => c.CompanyName),
                "oldest" => mappedList.OrderBy(c => c.CreatedAt),
                "systems_desc" => mappedList.OrderByDescending(c => c.TotalSystems),
                "alerts_desc" => mappedList.OrderByDescending(c => c.CriticalAlertsCount),
                "last_active_desc" => mappedList.OrderByDescending(c => c.LastActivityAt),
                _ => mappedList.OrderBy(c => c.CompanyName) // default company_asc
            };

            var items = sortedEnum
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (items, totalCount);
        }

        public async Task<CustomerDetailDto?> GetCustomerByIdAsync(long id)
        {
            await AutoGroupUnassignedMachinesAsync();

            var c = await _dbContext.Customers
                .Include(cust => cust.Machines)
                    .ThenInclude(m => m.CurrentStatus)
                .Include(cust => cust.Machines)
                    .ThenInclude(m => m.Alerts)
                .Include(cust => cust.Machines)
                    .ThenInclude(m => m.NetworkAdapters)
                .AsNoTracking()
                .FirstOrDefaultAsync(cust => cust.Id == id);

            if (c == null) return null;

            var machinesDto = c.Machines.Select(m =>
            {
                var st = _statusService.CalculateStatus(m);
                var ip = m.NetworkAdapters.FirstOrDefault(n => !string.IsNullOrEmpty(n.IpAddress))?.IpAddress;
                var critAlerts = m.Alerts.Count(a => a.Severity != null && a.Severity.Equals("critical", StringComparison.OrdinalIgnoreCase) && a.Status != "resolved");

                return new CustomerMachineDto
                {
                    Id = m.Id,
                    MachineUid = m.MachineUid,
                    Hostname = m.Hostname,
                    DeviceName = m.DeviceName,
                    OperatingSystem = m.OperatingSystem,
                    Status = st,
                    IsOnline = m.IsOnline,
                    LastHeartbeatAt = m.LastHeartbeatAt,
                    IpAddress = ip,
                    RamGb = m.RamGb,
                    Processor = m.Processor,
                    CriticalAlertsCount = critAlerts,
                    CreatedAt = m.CreatedAt
                };
            }).ToList();

            var totalSys = machinesDto.Count;
            var onlineSys = machinesDto.Count(m => m.Status == "Online");
            var offlineSys = machinesDto.Count(m => m.Status == "Offline");
            var sleepSys = machinesDto.Count(m => m.Status == "Sleeping");
            var maintSys = machinesDto.Count(m => m.Status == "Maintenance");
            var disabledSys = machinesDto.Count(m => m.Status == "Disabled");
            var deletedSys = machinesDto.Count(m => m.Status == "Deleted");
            var uninstalledSys = machinesDto.Count(m => m.Status == "Uninstalled");
            var pendingSys = machinesDto.Count(m => m.Status == "Registration Pending");
            var unknownSys = machinesDto.Count(m => m.Status == "Unknown");
            var totalCritAlerts = machinesDto.Sum(m => m.CriticalAlertsCount);

            var validHeartbeats = c.Machines
                .Where(m => m.LastHeartbeatAt.HasValue)
                .Select(m => m.LastHeartbeatAt!.Value)
                .ToList();

            DateTime lastAct = validHeartbeats.Count > 0 ? validHeartbeats.Max() : c.CreatedAt;

            return new CustomerDetailDto
            {
                Id = c.Id,
                CustomerCode = c.CustomerCode,
                CompanyName = c.CompanyName,
                CustomerName = c.CustomerName,
                MobileNumber = c.MobileNumber,
                Email = c.Email,
                Status = c.Status,
                Remarks = c.Remarks,
                TotalSystems = totalSys,
                OnlineSystems = onlineSys,
                OfflineSystems = offlineSys,
                SleepingSystems = sleepSys,
                MaintenanceSystems = maintSys,
                DisabledSystems = disabledSys,
                DeletedSystems = deletedSys,
                UninstalledSystems = uninstalledSys,
                PendingSystems = pendingSys,
                UnknownSystems = unknownSys,
                CriticalAlertsCount = totalCritAlerts,
                LastActivityAt = lastAct,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                Machines = machinesDto
            };
        }

        public async Task<List<CustomerMachineDto>> GetCustomerMachinesAsync(long customerId)
        {
            var detail = await GetCustomerByIdAsync(customerId);
            return detail?.Machines ?? new List<CustomerMachineDto>();
        }
    }
}
