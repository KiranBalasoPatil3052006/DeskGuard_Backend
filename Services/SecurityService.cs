using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.DTOs.Security;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Exceptions;
using DeskGuardBackend.Services.Interfaces;

namespace DeskGuardBackend.Services
{
    public class SecurityService : ISecurityService
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<SecurityService> _logger;

        public SecurityService(
            DeskGuardDbContext dbContext,
            IAuditLogService auditLogService,
            ILogger<SecurityService> logger)
        {
            _dbContext = dbContext;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<SecuritySettingDto> GetSecuritySettingsAsync(long? companyId = null)
        {
            var settings = await GetOrCreateSettingsEntityAsync(companyId);
            return MapToDto(settings);
        }

        public async Task<SecuritySettingDto> UpdateSecuritySettingsAsync(
            UpdateSecuritySettingRequest request,
            long performedByUserId,
            string? ipAddress = null,
            string? userAgent = null,
            long? companyId = null)
        {
            if (request.MinPasswordLength < 6 || request.MinPasswordLength > 64)
            {
                throw new UnauthorizedActionException("Minimum password length must be between 6 and 64 characters.", 400);
            }

            var settings = await GetOrCreateSettingsEntityAsync(companyId);

            var oldValues = System.Text.Json.JsonSerializer.Serialize(MapToDto(settings));

            settings.MinPasswordLength = request.MinPasswordLength;
            settings.RequireUppercase = request.RequireUppercase;
            settings.RequireLowercase = request.RequireLowercase;
            settings.RequireNumbers = request.RequireNumbers;
            settings.RequireSpecialChars = request.RequireSpecialChars;
            settings.IdleSessionTimeoutMinutes = request.IdleSessionTimeoutMinutes;
            settings.MaxFailedLoginAttempts = request.MaxFailedLoginAttempts;
            settings.AccountLockoutDurationMinutes = request.AccountLockoutDurationMinutes;
            settings.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            var newValues = System.Text.Json.JsonSerializer.Serialize(MapToDto(settings));
            var performedUser = await _dbContext.Users.FindAsync(performedByUserId);

            await _auditLogService.LogAsync(
                eventType: "Security Settings Updated",
                description: $"Updated security settings: MinLength={request.MinPasswordLength}, Timeout={request.IdleSessionTimeoutMinutes}m, LockoutMaxAttempts={request.MaxFailedLoginAttempts}",
                companyId: companyId,
                user: performedUser,
                oldValues: oldValues,
                newValues: newValues
            );

            return MapToDto(settings);
        }

        public async Task ValidatePasswordAgainstPolicyAsync(string password, long? companyId = null)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new UnauthorizedActionException("Password cannot be empty.", 400);
            }

            var settings = await GetOrCreateSettingsEntityAsync(companyId);

            if (password.Length < settings.MinPasswordLength)
            {
                throw new UnauthorizedActionException($"Password must be at least {settings.MinPasswordLength} characters long.", 400);
            }

            if (settings.RequireUppercase && !password.Any(char.IsUpper))
            {
                throw new UnauthorizedActionException("Password must contain at least one uppercase letter (A-Z).", 400);
            }

            if (settings.RequireLowercase && !password.Any(char.IsLower))
            {
                throw new UnauthorizedActionException("Password must contain at least one lowercase letter (a-z).", 400);
            }

            if (settings.RequireNumbers && !password.Any(char.IsDigit))
            {
                throw new UnauthorizedActionException("Password must contain at least one numeric digit (0-9).", 400);
            }

            if (settings.RequireSpecialChars && !password.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                throw new UnauthorizedActionException("Password must contain at least one special character (e.g. !@#$%^&*).", 400);
            }
        }

        public async Task RecordLoginHistoryAsync(
            long? userId,
            string email,
            bool isSuccess,
            string? failureReason,
            string? ipAddress,
            string? userAgent,
            long? companyId = null)
        {
            try
            {
                var (browser, os) = ParseUserAgent(userAgent);

                var history = new UserLoginHistory
                {
                    UserId = userId,
                    CompanyId = companyId,
                    Email = email,
                    LoginTime = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Browser = browser,
                    OperatingSystem = os,
                    Status = isSuccess ? "Success" : "Failed",
                    FailureReason = failureReason,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _dbContext.UserLoginHistories.AddAsync(history);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record login history for user email {Email}", email);
            }
        }

        public async Task<PaginatedResult<UserLoginHistoryDto>> GetLoginHistoryAsync(
            int page = 1,
            int perPage = 20,
            string? search = null,
            string? status = null,
            long? companyId = null)
        {
            var query = _dbContext.UserLoginHistories
                .Include(h => h.User)
                .AsNoTracking()
                .AsQueryable();

            if (companyId.HasValue)
            {
                query = query.Where(h => h.CompanyId == companyId || h.CompanyId == null);
            }

            if (!string.IsNullOrWhiteSpace(status) && status.ToLower() != "all")
            {
                query = query.Where(h => h.Status.ToLower() == status.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(h =>
                    h.Email.ToLower().Contains(term) ||
                    (h.User != null && h.User.Name != null && h.User.Name.ToLower().Contains(term)) ||
                    (h.IpAddress != null && h.IpAddress.ToLower().Contains(term)) ||
                    (h.Browser != null && h.Browser.ToLower().Contains(term)) ||
                    (h.OperatingSystem != null && h.OperatingSystem.ToLower().Contains(term)));
            }

            var totalItems = await query.CountAsync();
            page = Math.Max(1, page);
            perPage = Math.Clamp(perPage, 10, 100);

            var items = await query
                .OrderByDescending(h => h.LoginTime)
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .Select(h => new UserLoginHistoryDto
                {
                    Id = h.Id,
                    UserId = h.UserId,
                    Email = h.Email,
                    UserName = h.User != null ? h.User.Name : null,
                    LoginTime = h.LoginTime,
                    LogoutTime = h.LogoutTime,
                    IpAddress = h.IpAddress,
                    UserAgent = h.UserAgent,
                    Browser = h.Browser,
                    OperatingSystem = h.OperatingSystem,
                    Status = h.Status,
                    FailureReason = h.FailureReason
                })
                .ToListAsync();

            return new PaginatedResult<UserLoginHistoryDto>(items, totalItems, page, perPage);
        }

        public async Task<PaginatedResult<SecurityAuditLogDto>> GetSecurityAuditLogsAsync(
            int page = 1,
            int perPage = 20,
            string? search = null,
            long? companyId = null)
        {
            var query = _dbContext.AuditLogs
                .Include(a => a.User)
                .AsNoTracking()
                .AsQueryable();

            if (companyId.HasValue)
            {
                query = query.Where(a => a.CompanyId == companyId || a.CompanyId == null);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(a =>
                    a.EventType.ToLower().Contains(term) ||
                    (a.Description != null && a.Description.ToLower().Contains(term)) ||
                    (a.User != null && a.User.Email != null && a.User.Email.ToLower().Contains(term)) ||
                    (a.User != null && a.User.Name != null && a.User.Name.ToLower().Contains(term)));
            }

            var totalItems = await query.CountAsync();
            page = Math.Max(1, page);
            perPage = Math.Clamp(perPage, 10, 100);

            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .Select(a => new SecurityAuditLogDto
                {
                    Id = a.Id,
                    EventType = a.EventType,
                    Description = a.Description,
                    PerformedBy = a.User != null ? (a.User.Name ?? a.User.Email ?? "System") : "System",
                    TargetUser = ExtractTargetUser(a.Description, a.OldValues, a.NewValues),
                    IpAddress = a.IpAddress,
                    UserAgent = a.UserAgent,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return new PaginatedResult<SecurityAuditLogDto>(items, totalItems, page, perPage);
        }

        private async Task<SecuritySetting> GetOrCreateSettingsEntityAsync(long? companyId)
        {
            var settings = await _dbContext.SecuritySettings
                .FirstOrDefaultAsync(s => s.CompanyId == companyId);

            if (settings == null)
            {
                settings = new SecuritySetting
                {
                    CompanyId = companyId,
                    MinPasswordLength = 6,
                    RequireUppercase = true,
                    RequireLowercase = true,
                    RequireNumbers = true,
                    RequireSpecialChars = true,
                    IdleSessionTimeoutMinutes = 30,
                    MaxFailedLoginAttempts = 5,
                    AccountLockoutDurationMinutes = 30,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _dbContext.SecuritySettings.AddAsync(settings);
                await _dbContext.SaveChangesAsync();
            }

            return settings;
        }

        private static SecuritySettingDto MapToDto(SecuritySetting s) => new SecuritySettingDto
        {
            Id = s.Id,
            MinPasswordLength = s.MinPasswordLength,
            RequireUppercase = s.RequireUppercase,
            RequireLowercase = s.RequireLowercase,
            RequireNumbers = s.RequireNumbers,
            RequireSpecialChars = s.RequireSpecialChars,
            IdleSessionTimeoutMinutes = s.IdleSessionTimeoutMinutes,
            MaxFailedLoginAttempts = s.MaxFailedLoginAttempts,
            AccountLockoutDurationMinutes = s.AccountLockoutDurationMinutes,
            UpdatedAt = s.UpdatedAt
        };

        private static (string Browser, string OS) ParseUserAgent(string? ua)
        {
            if (string.IsNullOrEmpty(ua)) return ("Unknown", "Unknown");

            var browser = "Unknown";
            if (ua.Contains("Edg/", StringComparison.OrdinalIgnoreCase)) browser = "Microsoft Edge";
            else if (ua.Contains("Chrome/", StringComparison.OrdinalIgnoreCase)) browser = "Google Chrome";
            else if (ua.Contains("Firefox/", StringComparison.OrdinalIgnoreCase)) browser = "Mozilla Firefox";
            else if (ua.Contains("Safari/", StringComparison.OrdinalIgnoreCase)) browser = "Apple Safari";
            else if (ua.Contains("OPR/", StringComparison.OrdinalIgnoreCase) || ua.Contains("Opera", StringComparison.OrdinalIgnoreCase)) browser = "Opera";

            var os = "Unknown";
            if (ua.Contains("Windows", StringComparison.OrdinalIgnoreCase)) os = "Windows";
            else if (ua.Contains("Macintosh", StringComparison.OrdinalIgnoreCase) || ua.Contains("Mac OS", StringComparison.OrdinalIgnoreCase)) os = "macOS";
            else if (ua.Contains("Linux", StringComparison.OrdinalIgnoreCase)) os = "Linux";
            else if (ua.Contains("Android", StringComparison.OrdinalIgnoreCase)) os = "Android";
            else if (ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || ua.Contains("iPad", StringComparison.OrdinalIgnoreCase)) os = "iOS";

            return (browser, os);
        }

        private static string? ExtractTargetUser(string? description, string? oldValues, string? newValues)
        {
            if (!string.IsNullOrEmpty(description) && description.Contains("account", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(description, @"account\s+([^\s]+)", RegexOptions.IgnoreCase);
                if (match.Success) return match.Groups[1].Value;
            }
            return null;
        }
    }
}
