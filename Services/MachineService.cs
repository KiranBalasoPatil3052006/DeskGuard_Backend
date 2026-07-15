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
using DeskGuardBackend.DTOs.Machine;
using DeskGuardBackend.Enums;

namespace DeskGuardBackend.Services
{
    public class MachineService : IMachineService
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<MachineService> _logger;
        private const int OfflineThresholdMinutes = 5;

        public MachineService(
            DeskGuardDbContext dbContext,
            IAuditLogService auditLogService,
            ILogger<MachineService> logger)
        {
            _dbContext = dbContext;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<Machine> GetMachineAsync(long id)
        {
            try
            {
                var machine = await _dbContext.Machines
                    .Include(m => m.Company)
                    .Include(m => m.AssignedUser)
                    .Include(m => m.CurrentStatus)
                    .Include(m => m.NetworkAdapters)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (machine == null)
                {
                    throw new MachineNotFoundException($"Machine not found with ID: {id}", 404);
                }

                return machine;
            }
            catch (MachineNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MachineService::GetMachineAsync failed for ID: {MachineId}", id);
                throw new MachineNotFoundException("Failed to retrieve machine.", 500);
            }
        }

        public async Task<Machine> GetMachineByUidAsync(string uid)
        {
            try
            {
                var machine = await _dbContext.Machines
                    .Include(m => m.Company)
                    .Include(m => m.AssignedUser)
                    .Include(m => m.CurrentStatus)
                    .FirstOrDefaultAsync(m => m.MachineUid == uid);

                if (machine == null)
                {
                    throw new MachineNotFoundException($"Machine not found with UID: {uid}", 404);
                }

                return machine;
            }
            catch (MachineNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MachineService::GetMachineByUidAsync failed for UID: {MachineUid}", uid);
                throw new MachineNotFoundException("Failed to retrieve machine by UID.", 500);
            }
        }

        public async Task<PaginatedResponseDto<MachineResponseDto>> GetCompanyMachinesAsync(
            long companyId, 
            int page, 
            int perPage, 
            string? status, 
            string? search)
        {
            try
            {
                var query = _dbContext.Machines
                    .Include(m => m.AssignedUser)
                    .Include(m => m.CurrentStatus)
                    .Where(m => m.CompanyId == companyId);

                if (!string.IsNullOrEmpty(status))
                {
                    var statusLower = status.ToLowerInvariant();
                    if (statusLower == "online")
                    {
                        query = query.Where(m => m.IsOnline);
                    }
                    else if (statusLower == "offline")
                    {
                        query = query.Where(m => !m.IsOnline);
                    }
                }

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(m => 
                        (m.Hostname != null && m.Hostname.Contains(search)) ||
                        (m.DeviceName != null && m.DeviceName.Contains(search)) ||
                        m.MachineUid.Contains(search) ||
                        (m.EmployeeMobileNumber != null && m.EmployeeMobileNumber.Contains(search))
                    );
                }

                var total = await query.CountAsync();
                perPage = Math.Min(Math.Max(1, perPage), 100);
                var lastPage = (int)Math.Ceiling((double)total / perPage);
                page = Math.Min(Math.Max(1, page), Math.Max(1, lastPage));

                var machines = await query
                    .OrderByDescending(m => m.LastHeartbeatAt)
                    .Skip((page - 1) * perPage)
                    .Take(perPage)
                    .ToListAsync();

                var items = machines.Select(m => new MachineResponseDto
                {
                    Id = m.Id,
                    CompanyId = m.CompanyId,
                    UserId = m.UserId,
                    MachineUid = m.MachineUid,
                    Hostname = m.Hostname,
                    DeviceName = m.DeviceName,
                    OperatingSystem = m.OperatingSystem,
                    Model = m.Model,
                    IsOnline = m.IsOnline,
                    LastHeartbeatAt = m.LastHeartbeatAt,
                    IsActive = m.IsActive,
                    EmployeeMobileNumber = m.EmployeeMobileNumber,
                    CurrentStatus = m.CurrentStatus != null ? new MachineCurrentStatusDto
                    {
                        CpuPercentage = m.CurrentStatus.CpuPercentage,
                        CpuTemperature = m.CurrentStatus.CpuTemperature,
                        RamPercentage = m.CurrentStatus.RamPercentage,
                        DiskPercentage = m.CurrentStatus.DiskPercentage,
                        OnlineStatus = m.CurrentStatus.OnlineStatus,
                        NetworkInterfaces = m.CurrentStatus.NetworkInterfaces != null
                            ? JsonSerializer.Deserialize<object>(m.CurrentStatus.NetworkInterfaces)
                            : null
                    } : null
                }).ToList();

                return new PaginatedResponseDto<MachineResponseDto>
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
                _logger.LogError(ex, "MachineService::GetCompanyMachinesAsync failed for company: {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<object> GetCompanyMachineSummaryAsync(long companyId)
        {
            try
            {
                var total = await _dbContext.Machines.CountAsync(m => m.CompanyId == companyId);
                var online = await _dbContext.Machines.CountAsync(m => m.CompanyId == companyId && m.IsOnline);
                var critical = await _dbContext.Alerts
                    .CountAsync(a => a.CompanyId == companyId && a.Severity == "critical" && (a.Status == "open" || a.Status == "acknowledged"));

                return new
                {
                    total = total,
                    online_count = online,
                    offline_count = total - online,
                    critical_count = critical
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MachineService::GetCompanyMachineSummaryAsync failed for company: {CompanyId}", companyId);
                return new { total = 0, online_count = 0, offline_count = 0, critical_count = 0 };
            }
        }

        public async Task<Machine> AssignMachineAsync(long machineId, long userId)
        {
            try
            {
                var machine = await _dbContext.Machines.FindAsync(machineId);
                if (machine == null)
                {
                    throw new MachineNotFoundException($"Machine not found with ID: {machineId}", 404);
                }

                machine.UserId = userId;
                await _dbContext.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    EventType.Update.ToString(),
                    $"Machine assigned to user ID: {userId}",
                    machine: machine
                );

                _logger.LogInformation("Machine {MachineId} assigned to user {UserId}", machineId, userId);
                return machine;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MachineService::AssignMachineAsync failed for machine {MachineId}", machineId);
                throw;
            }
        }

        public async Task<Machine> UnassignMachineAsync(long machineId)
        {
            try
            {
                var machine = await _dbContext.Machines.FindAsync(machineId);
                if (machine == null)
                {
                    throw new MachineNotFoundException($"Machine not found with ID: {machineId}", 404);
                }

                machine.UserId = null;
                await _dbContext.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    EventType.Update.ToString(),
                    "Machine unassigned",
                    machine: machine
                );

                _logger.LogInformation("Machine {MachineId} unassigned", machineId);
                return machine;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MachineService::UnassignMachineAsync failed for machine {MachineId}", machineId);
                throw;
            }
        }

        public async Task UpdateHeartbeatAsync(string machineUid)
        {
            try
            {
                var machine = await _dbContext.Machines.FirstOrDefaultAsync(m => m.MachineUid == machineUid);
                if (machine == null)
                {
                    _logger.LogWarning("MachineService::UpdateHeartbeatAsync - Machine not found: {MachineUid}", machineUid);
                    return;
                }

                machine.LastHeartbeatAt = DateTime.UtcNow;
                machine.IsOnline = true;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Heartbeat updated for machine: {MachineUid}", machineUid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MachineService::UpdateHeartbeatAsync failed for machine: {MachineUid}", machineUid);
            }
        }

        public async Task<int> MarkOfflineMachinesAsync()
        {
            try
            {
                var threshold = DateTime.UtcNow.AddMinutes(-OfflineThresholdMinutes);

                var count = await _dbContext.Machines
                    .Where(m => m.IsOnline && (m.LastHeartbeatAt == null || m.LastHeartbeatAt < threshold))
                    .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.IsOnline, false));

                if (count > 0)
                {
                    _logger.LogInformation("{Count} machines marked as offline", count);
                }

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MachineService::MarkOfflineMachinesAsync failed");
                return 0;
            }
        }

        public async Task<int> GetOnlineCountAsync(long companyId)
        {
            return await _dbContext.Machines.CountAsync(m => m.CompanyId == companyId && m.IsOnline);
        }

        public async Task<int> GetOfflineCountAsync(long companyId)
        {
            return await _dbContext.Machines.CountAsync(m => m.CompanyId == companyId && !m.IsOnline);
        }
    }
}
