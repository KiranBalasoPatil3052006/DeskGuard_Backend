using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using DeskGuardBackend.DTOs.AlertThreshold;
using DeskGuardBackend.Services.Interfaces;
using DeskGuardBackend.Enums;
using DeskGuardBackend.Exceptions;

namespace DeskGuardBackend.Services
{
    public class AlertProfileService : IAlertProfileService
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AlertProfileService> _logger;

        public AlertProfileService(
            DeskGuardDbContext dbContext,
            IAuditLogService auditLogService,
            ILogger<AlertProfileService> logger)
        {
            _dbContext = dbContext;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<AlertProfileListResponse> GetAllAsync(AlertProfileFilterRequest filter)
        {
            var query = _dbContext.AlertProfiles
                .Include(p => p.AssignedCompanies)
                .Include(p => p.CustomAssignedMachines)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(search) ||
                    (p.Description != null && p.Description.ToLower().Contains(search)));
            }

            var total = await query.CountAsync();
            filter.PerPage = Math.Min(Math.Max(1, filter.PerPage), 100);
            var lastPage = (int)Math.Ceiling((double)total / filter.PerPage);
            filter.Page = Math.Min(Math.Max(1, filter.Page), Math.Max(1, lastPage));

            var items = await query
                .OrderByDescending(p => p.IsDefault)
                .ThenBy(p => p.Name)
                .Skip((filter.Page - 1) * filter.PerPage)
                .Take(filter.PerPage)
                .ToListAsync();

            var data = items.Select(MapToDto).ToList();

            return new AlertProfileListResponse
            {
                Data = data,
                Total = total,
                Page = filter.Page,
                PerPage = filter.PerPage,
                TotalPages = lastPage
            };
        }

        public async Task<AlertProfileDto> GetByIdAsync(long id)
        {
            var profile = await _dbContext.AlertProfiles
                .Include(p => p.Threshold)
                .Include(p => p.AssignedCompanies)
                .Include(p => p.CustomAssignedMachines)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile == null)
                throw new AlertProfileException("Alert profile not found.", 404);

            return MapToDto(profile);
        }

        public async Task<AlertProfileDto> CreateAsync(CreateAlertProfileRequest request)
        {
            var errors = ValidateProfileName(request.Name);
            if (request.Thresholds != null)
                ValidateThresholds(request.Thresholds, errors);

            if (errors.Count > 0)
                throw new AlertProfileException("Validation failed.", 422, errors);

            var now = DateTime.UtcNow;
            var profile = new AlertProfile
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.AlertProfiles.Add(profile);
            await _dbContext.SaveChangesAsync();

            if (request.Thresholds != null)
            {
                var threshold = MapThresholdFromDto(request.Thresholds, profile.Id);
                _dbContext.AlertThresholds.Add(threshold);
                await _dbContext.SaveChangesAsync();
            }

            await _auditLogService.LogAsync(
                EventType.Create.ToString(),
                $"Alert profile created: {profile.Name}"
            );

            _logger.LogInformation("AlertProfile {Id} ({Name}) created", profile.Id, profile.Name);
            return await GetByIdAsync(profile.Id);
        }

        public async Task<AlertProfileDto> UpdateAsync(long id, UpdateAlertProfileRequest request)
        {
            var profile = await _dbContext.AlertProfiles
                .Include(p => p.Threshold)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile == null)
                throw new AlertProfileException("Alert profile not found.", 404);

            var errors = new Dictionary<string, string[]>();

            if (request.Name != null)
            {
                var nameErrors = ValidateProfileName(request.Name);
                foreach (var kv in nameErrors) errors[kv.Key] = kv.Value;
                profile.Name = request.Name.Trim();
            }

            if (request.Description != null)
                profile.Description = request.Description?.Trim();

            if (request.IsDefault.HasValue && request.IsDefault.Value && !profile.IsDefault)
            {
                // Unset previous default
                var currentDefault = await _dbContext.AlertProfiles
                    .FirstOrDefaultAsync(p => p.IsDefault && p.Id != id);
                if (currentDefault != null)
                {
                    currentDefault.IsDefault = false;
                }
                profile.IsDefault = true;
            }
            else if (request.IsDefault.HasValue && !request.IsDefault.Value && profile.IsDefault)
            {
                // Cannot unset the only default
                errors["is_default"] = new[] { "Cannot unset the default profile. Set another profile as default first." };
            }

            if (request.Thresholds != null)
            {
                ValidateThresholds(request.Thresholds, errors);
                if (errors.Count == 0)
                {
                    if (profile.Threshold != null)
                    {
                        MapThresholdFromDto(request.Thresholds, profile.Id, profile.Threshold);
                    }
                    else
                    {
                        var threshold = MapThresholdFromDto(request.Thresholds, profile.Id);
                        _dbContext.AlertThresholds.Add(threshold);
                    }
                }
            }

            if (errors.Count > 0)
                throw new AlertProfileException("Validation failed.", 422, errors);

            profile.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            await _auditLogService.LogAsync(
                EventType.Update.ToString(),
                $"Alert profile updated: {profile.Name}"
            );

            _logger.LogInformation("AlertProfile {Id} ({Name}) updated", profile.Id, profile.Name);
            return await GetByIdAsync(profile.Id);
        }

        public async Task DeleteAsync(long id)
        {
            var profile = await _dbContext.AlertProfiles
                .Include(p => p.AssignedCompanies)
                .Include(p => p.CustomAssignedMachines)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile == null)
                throw new AlertProfileException("Alert profile not found.", 404);

            if (profile.AssignedCompanies.Count > 0 || profile.CustomAssignedMachines.Count > 0)
            {
                var companyNames = profile.AssignedCompanies.Select(c => c.Name);
                var machineIds = profile.CustomAssignedMachines.Select(m => m.Id.ToString());
                throw new AlertProfileException(
                    "Cannot delete profile that is currently assigned. Unassign all companies and machines first.", 422,
                    new Dictionary<string, string[]>
                    {
                        { "assigned_companies", companyNames.ToArray() },
                        { "assigned_machines", machineIds.ToArray() }
                    });
            }

            _dbContext.AlertProfiles.Remove(profile);
            await _dbContext.SaveChangesAsync();

            await _auditLogService.LogAsync(
                EventType.Delete.ToString(),
                $"Alert profile deleted: {profile.Name}"
            );

            _logger.LogInformation("AlertProfile {Id} ({Name}) deleted", id, profile.Name);
        }

        public async Task<AlertProfileDto> DuplicateAsync(long id)
        {
            var source = await _dbContext.AlertProfiles
                .Include(p => p.Threshold)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (source == null)
                throw new AlertProfileException("Alert profile not found.", 404);

            var duplicateName = GenerateDuplicateName(source.Name);
            var now = DateTime.UtcNow;

            var profile = new AlertProfile
            {
                Name = duplicateName,
                Description = source.Description,
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.AlertProfiles.Add(profile);
            await _dbContext.SaveChangesAsync();

            if (source.Threshold != null)
            {
                var threshold = new AlertThreshold
                {
                    ProfileId = profile.Id,
                    CpuWarningPercent = source.Threshold.CpuWarningPercent,
                    CpuCriticalPercent = source.Threshold.CpuCriticalPercent,
                    CpuWarningDurationMinutes = source.Threshold.CpuWarningDurationMinutes,
                    RamWarningPercent = source.Threshold.RamWarningPercent,
                    RamCriticalPercent = source.Threshold.RamCriticalPercent,
                    RamWarningDurationMinutes = source.Threshold.RamWarningDurationMinutes,
                    CpuTempWarning = source.Threshold.CpuTempWarning,
                    CpuTempCritical = source.Threshold.CpuTempCritical,
                    DiskWarningPercent = source.Threshold.DiskWarningPercent,
                    DiskCriticalPercent = source.Threshold.DiskCriticalPercent,
                    DiskSmartWarningEnabled = source.Threshold.DiskSmartWarningEnabled,
                    DiskSmartCriticalEnabled = source.Threshold.DiskSmartCriticalEnabled,
                    OfflineWarningMinutes = source.Threshold.OfflineWarningMinutes,
                    OfflineCriticalMinutes = source.Threshold.OfflineCriticalMinutes,
                    FailedLoginWarningCount = source.Threshold.FailedLoginWarningCount,
                    FailedLoginCriticalCount = source.Threshold.FailedLoginCriticalCount,
                    NetworkDisconnectWarningCount = source.Threshold.NetworkDisconnectWarningCount,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _dbContext.AlertThresholds.Add(threshold);
                await _dbContext.SaveChangesAsync();
            }

            await _auditLogService.LogAsync(
                EventType.Create.ToString(),
                $"Alert profile duplicated from {source.Name}: {profile.Name}"
            );

            _logger.LogInformation("AlertProfile duplicated from {SourceId} to {NewId}", id, profile.Id);
            return await GetByIdAsync(profile.Id);
        }

        public async Task AssignToCompanyAsync(long profileId, long companyId)
        {
            var profile = await _dbContext.AlertProfiles.FindAsync(profileId);
            if (profile == null)
                throw new AlertProfileException("Alert profile not found.", 404);

            var company = await _dbContext.Companies.FindAsync(companyId);
            if (company == null)
                throw new AlertProfileException("Company not found.", 404);

            company.AlertProfileId = profileId;
            await _dbContext.SaveChangesAsync();

            await _auditLogService.LogAsync(
                EventType.Update.ToString(),
                $"Alert profile '{profile.Name}' assigned to company '{company.Name}'"
            );
        }

        public async Task UnassignFromCompanyAsync(long profileId, long companyId)
        {
            var company = await _dbContext.Companies
                .FirstOrDefaultAsync(c => c.Id == companyId && c.AlertProfileId == profileId);

            if (company == null)
                throw new AlertProfileException("Company is not assigned to this profile.", 404);

            company.AlertProfileId = null;
            await _dbContext.SaveChangesAsync();

            await _auditLogService.LogAsync(
                EventType.Update.ToString(),
                $"Alert profile unassigned from company '{company.Name}'"
            );
        }

        public async Task AssignToMachineAsync(long profileId, long machineId)
        {
            var profile = await _dbContext.AlertProfiles.FindAsync(profileId);
            if (profile == null)
                throw new AlertProfileException("Alert profile not found.", 404);

            var machine = await _dbContext.Machines.FindAsync(machineId);
            if (machine == null)
                throw new AlertProfileException("Machine not found.", 404);

            machine.CustomAlertProfileId = profileId;
            await _dbContext.SaveChangesAsync();

            await _auditLogService.LogAsync(
                EventType.Update.ToString(),
                $"Alert profile '{profile.Name}' assigned to machine {machine.MachineUid}"
            );
        }

        public async Task UnassignFromMachineAsync(long profileId, long machineId)
        {
            var machine = await _dbContext.Machines
                .FirstOrDefaultAsync(m => m.Id == machineId && m.CustomAlertProfileId == profileId);

            if (machine == null)
                throw new AlertProfileException("Machine does not have this profile override.", 404);

            machine.CustomAlertProfileId = null;
            await _dbContext.SaveChangesAsync();

            await _auditLogService.LogAsync(
                EventType.Update.ToString(),
                $"Alert profile override removed from machine {machine.MachineUid}"
            );
        }

        public async Task<AlertThresholdDto?> ResolveThresholdsForMachineAsync(long machineId)
        {
            var machine = await _dbContext.Machines
                .Include(m => m.Company)
                .FirstOrDefaultAsync(m => m.Id == machineId);

            if (machine == null) return null;

            AlertThreshold? threshold = null;

            // 1. Machine override
            if (machine.CustomAlertProfileId.HasValue)
            {
                threshold = await _dbContext.AlertThresholds
                    .FirstOrDefaultAsync(t => t.ProfileId == machine.CustomAlertProfileId.Value);
            }

            // 2. Company profile
            if (threshold == null && machine.Company?.AlertProfileId != null)
            {
                threshold = await _dbContext.AlertThresholds
                    .FirstOrDefaultAsync(t => t.ProfileId == machine.Company.AlertProfileId.Value);
            }

            // 3. Default profile
            if (threshold == null)
            {
                var defaultProfile = await _dbContext.AlertProfiles
                    .FirstOrDefaultAsync(p => p.IsDefault);

                if (defaultProfile != null)
                {
                    threshold = await _dbContext.AlertThresholds
                        .FirstOrDefaultAsync(t => t.ProfileId == defaultProfile.Id);
                }
            }

            if (threshold == null) return null;

            return new AlertThresholdDto
            {
                CpuWarningPercent = threshold.CpuWarningPercent,
                CpuCriticalPercent = threshold.CpuCriticalPercent,
                CpuWarningDurationMinutes = threshold.CpuWarningDurationMinutes,
                RamWarningPercent = threshold.RamWarningPercent,
                RamCriticalPercent = threshold.RamCriticalPercent,
                RamWarningDurationMinutes = threshold.RamWarningDurationMinutes,
                CpuTempWarning = threshold.CpuTempWarning,
                CpuTempCritical = threshold.CpuTempCritical,
                DiskWarningPercent = threshold.DiskWarningPercent,
                DiskCriticalPercent = threshold.DiskCriticalPercent,
                DiskSmartWarningEnabled = threshold.DiskSmartWarningEnabled,
                DiskSmartCriticalEnabled = threshold.DiskSmartCriticalEnabled,
                OfflineWarningMinutes = threshold.OfflineWarningMinutes,
                OfflineCriticalMinutes = threshold.OfflineCriticalMinutes,
                FailedLoginWarningCount = threshold.FailedLoginWarningCount,
                FailedLoginCriticalCount = threshold.FailedLoginCriticalCount,
                NetworkDisconnectWarningCount = threshold.NetworkDisconnectWarningCount
            };
        }

        // ──────────────────────────────────────────────
        // Private helpers
        // ──────────────────────────────────────────────

        private static AlertProfileDto MapToDto(AlertProfile profile)
        {
            return new AlertProfileDto
            {
                Id = profile.Id,
                Name = profile.Name,
                Description = profile.Description,
                IsDefault = profile.IsDefault,
                AssignedCompaniesCount = profile.AssignedCompanies?.Count ?? 0,
                AssignedMachinesCount = profile.CustomAssignedMachines?.Count ?? 0,
                CreatedAt = profile.CreatedAt,
                UpdatedAt = profile.UpdatedAt,
                Thresholds = profile.Threshold != null ? new AlertThresholdDto
                {
                    CpuWarningPercent = profile.Threshold.CpuWarningPercent,
                    CpuCriticalPercent = profile.Threshold.CpuCriticalPercent,
                    CpuWarningDurationMinutes = profile.Threshold.CpuWarningDurationMinutes,
                    RamWarningPercent = profile.Threshold.RamWarningPercent,
                    RamCriticalPercent = profile.Threshold.RamCriticalPercent,
                    RamWarningDurationMinutes = profile.Threshold.RamWarningDurationMinutes,
                    CpuTempWarning = profile.Threshold.CpuTempWarning,
                    CpuTempCritical = profile.Threshold.CpuTempCritical,
                    DiskWarningPercent = profile.Threshold.DiskWarningPercent,
                    DiskCriticalPercent = profile.Threshold.DiskCriticalPercent,
                    DiskSmartWarningEnabled = profile.Threshold.DiskSmartWarningEnabled,
                    DiskSmartCriticalEnabled = profile.Threshold.DiskSmartCriticalEnabled,
                    OfflineWarningMinutes = profile.Threshold.OfflineWarningMinutes,
                    OfflineCriticalMinutes = profile.Threshold.OfflineCriticalMinutes,
                    FailedLoginWarningCount = profile.Threshold.FailedLoginWarningCount,
                    FailedLoginCriticalCount = profile.Threshold.FailedLoginCriticalCount,
                    NetworkDisconnectWarningCount = profile.Threshold.NetworkDisconnectWarningCount
                } : null
            };
        }

        private static AlertThreshold MapThresholdFromDto(AlertThresholdDto dto, long profileId, AlertThreshold? existing = null)
        {
            var threshold = existing ?? new AlertThreshold();
            threshold.ProfileId = profileId;
            threshold.CpuWarningPercent = dto.CpuWarningPercent;
            threshold.CpuCriticalPercent = dto.CpuCriticalPercent;
            threshold.CpuWarningDurationMinutes = dto.CpuWarningDurationMinutes;
            threshold.RamWarningPercent = dto.RamWarningPercent;
            threshold.RamCriticalPercent = dto.RamCriticalPercent;
            threshold.RamWarningDurationMinutes = dto.RamWarningDurationMinutes;
            threshold.CpuTempWarning = dto.CpuTempWarning;
            threshold.CpuTempCritical = dto.CpuTempCritical;
            threshold.DiskWarningPercent = dto.DiskWarningPercent;
            threshold.DiskCriticalPercent = dto.DiskCriticalPercent;
            threshold.DiskSmartWarningEnabled = dto.DiskSmartWarningEnabled;
            threshold.DiskSmartCriticalEnabled = dto.DiskSmartCriticalEnabled;
            threshold.OfflineWarningMinutes = dto.OfflineWarningMinutes;
            threshold.OfflineCriticalMinutes = dto.OfflineCriticalMinutes;
            threshold.FailedLoginWarningCount = dto.FailedLoginWarningCount;
            threshold.FailedLoginCriticalCount = dto.FailedLoginCriticalCount;
            threshold.NetworkDisconnectWarningCount = dto.NetworkDisconnectWarningCount;
            return threshold;
        }

        private static Dictionary<string, string[]> ValidateProfileName(string name)
        {
            var errors = new Dictionary<string, string[]>();
            if (string.IsNullOrWhiteSpace(name))
                errors["name"] = new[] { "Profile name is required." };
            else if (name.Trim().Length > 255)
                errors["name"] = new[] { "Profile name must not exceed 255 characters." };
            return errors;
        }

        private static void ValidateThresholds(AlertThresholdDto dto, Dictionary<string, string[]> errors)
        {
            if (dto.CpuWarningPercent.HasValue && dto.CpuCriticalPercent.HasValue &&
                dto.CpuCriticalPercent <= dto.CpuWarningPercent)
                errors["cpu_critical_percent"] = new[] { "CPU critical threshold must be greater than warning threshold." };

            if (dto.RamWarningPercent.HasValue && dto.RamCriticalPercent.HasValue &&
                dto.RamCriticalPercent <= dto.RamWarningPercent)
                errors["ram_critical_percent"] = new[] { "RAM critical threshold must be greater than warning threshold." };

            if (dto.OfflineWarningMinutes.HasValue && dto.OfflineCriticalMinutes.HasValue &&
                dto.OfflineCriticalMinutes <= dto.OfflineWarningMinutes)
                errors["offline_critical_minutes"] = new[] { "Offline critical duration must be greater than warning duration." };

            if (dto.FailedLoginWarningCount.HasValue && dto.FailedLoginCriticalCount.HasValue &&
                dto.FailedLoginCriticalCount <= dto.FailedLoginWarningCount)
                errors["failed_login_critical_count"] = new[] { "Failed login critical count must be greater than warning count." };
        }

        private static string GenerateDuplicateName(string sourceName)
        {
            var match = Regex.Match(sourceName, @"^(.*)\s+\(copy\s*(\d*)\)$");
            if (match.Success)
            {
                var baseName = match.Groups[1].Value;
                var numStr = match.Groups[2].Value;
                var num = string.IsNullOrEmpty(numStr) ? 2 : int.Parse(numStr) + 1;
                return $"{baseName} (copy {num})";
            }
            return $"{sourceName} (copy)";
        }
    }
}
