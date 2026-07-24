using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeskGuardBackend.Data;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.DTOs.Notification;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DeskGuardBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/settings")]
    public class SettingsController : ControllerBase
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ISmtpEmailService _smtpEmailService;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(
            DeskGuardDbContext dbContext,
            ISmtpEmailService smtpEmailService,
            ILogger<SettingsController> logger)
        {
            _dbContext = dbContext;
            _smtpEmailService = smtpEmailService;
            _logger = logger;
        }

        private long GetCompanyId()
        {
            var compIdStr = User.FindFirst("CompanyId")?.Value;
            return long.TryParse(compIdStr, out var companyId) ? companyId : 1;
        }

        private string GetUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        }
        // ── 0. COMBINED NOTIFICATIONS OVERVIEW ──
        // Frontend can optionally call this single endpoint instead of multiple

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotificationsOverview()
        {
            try
            {
                var companyId = GetCompanyId();

                // Aggregate SMTP, recipients, and rules into one response
                var smtpConfig = await _smtpEmailService.GetSmtpConfigAsync(companyId);
                var recipients = await _dbContext.EmailRecipients
                    .Where(r => r.CompanyId == companyId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new EmailRecipientDto
                    {
                        Id = r.Id,
                        Email = r.Email,
                        Name = r.Name,
                        Department = r.Department,
                        IsActive = r.IsActive,
                        CreatedAt = r.CreatedAt
                    })
                    .ToListAsync();

                var rules = await _dbContext.NotificationRules
                    .Where(r => r.CompanyId == companyId || r.CompanyId == null)
                    .Select(r => new NotificationRuleDto
                    {
                        Id = r.Id,
                        Category = r.Category,
                        EventType = r.EventType,
                        DisplayName = r.DisplayName,
                        SendEmail = r.SendEmail
                    })
                    .ToListAsync();

                if (rules.Count == 0) rules = GetDefaultNotificationRules();

                var anyEmailEnabled = rules.Any(r => r.SendEmail);

                return Ok(ApiResponse<object>.Ok(new
                {
                    smtp = smtpConfig,
                    email_recipients = recipients,
                    notification_rules = rules,
                    email_alerts_enabled = anyEmailEnabled
                }, "Notification settings retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get notification overview");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve notification settings."));
            }
        }

        // ── 1. SMTP CONFIGURATION ──

        [HttpGet("smtp")]
        [HttpGet("notifications/smtp")]
        public async Task<IActionResult> GetSmtpConfig()
        {
            try
            {
                var companyId = GetCompanyId();
                var config = await _smtpEmailService.GetSmtpConfigAsync(companyId);
                return Ok(ApiResponse<SmtpConfigDto>.Ok(config, "SMTP configuration retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get SMTP configuration");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve SMTP configuration."));
            }
        }

        [HttpPut("smtp")]
        [HttpPut("notifications/smtp")]
        public async Task<IActionResult> UpdateSmtpConfig([FromBody] UpdateSmtpConfigRequest request)
        {
            try
            {
                if (GetUserRole() != "Super Admin")
                {
                    return StatusCode(403, ApiResponse.Fail("Only Super Admin can update SMTP configuration."));
                }

                var companyId = GetCompanyId();
                var updated = await _smtpEmailService.UpdateSmtpConfigAsync(companyId, request);
                return Ok(ApiResponse<SmtpConfigDto>.Ok(updated, "SMTP configuration saved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update SMTP configuration");
                return StatusCode(500, ApiResponse.Fail("Failed to save SMTP configuration."));
            }
        }

        [HttpPost("smtp/test")]
        [HttpPost("notifications/test")]
        public async Task<IActionResult> TestSmtpConnection([FromBody] TestSmtpConnectionRequest request)
        {
            try
            {
                if (GetUserRole() != "Super Admin")
                {
                    return StatusCode(403, ApiResponse.Fail("Only Super Admin can perform SMTP connection tests."));
                }

                var companyId = GetCompanyId();
                var result = await _smtpEmailService.TestSmtpConnectionAsync(companyId, request);
                if (!result.Success)
                {
                    return BadRequest(ApiResponse.Fail(result.Message));
                }

                return Ok(ApiResponse.Ok(result.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test SMTP connection");
                return StatusCode(500, ApiResponse.Fail($"SMTP connection test failed: {ex.Message}"));
            }
        }

        // ── 2. EMAIL RECIPIENTS ──

        [HttpGet("email-recipients")]
        public async Task<IActionResult> ListEmailRecipients()
        {
            try
            {
                var companyId = GetCompanyId();
                var recipients = await _dbContext.EmailRecipients
                    .Where(r => r.CompanyId == companyId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new EmailRecipientDto
                    {
                        Id = r.Id,
                        Email = r.Email,
                        Name = r.Name,
                        Department = r.Department,
                        IsActive = r.IsActive,
                        CreatedAt = r.CreatedAt
                    })
                    .ToListAsync();

                return Ok(ApiResponse<IEnumerable<EmailRecipientDto>>.Ok(recipients, "Email recipients retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list email recipients");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve email recipients."));
            }
        }

        [HttpPost("email-recipients")]
        public async Task<IActionResult> AddEmailRecipient([FromBody] CreateEmailRecipientRequest request)
        {
            try
            {
                if (GetUserRole() != "Super Admin")
                {
                    return StatusCode(403, ApiResponse.Fail("Only Super Admin can add email recipients."));
                }

                var companyId = GetCompanyId();
                var email = request.Email.Trim().ToLowerInvariant();

                var exists = await _dbContext.EmailRecipients
                    .AnyAsync(r => r.CompanyId == companyId && r.Email.ToLower() == email);

                if (exists)
                {
                    return UnprocessableEntity(ApiResponse.Fail("This email address is already registered as a recipient."));
                }

                var recipient = new EmailRecipient
                {
                    CompanyId = companyId,
                    Email = email,
                    Name = request.Name?.Trim(),
                    Department = request.Department?.Trim(),
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _dbContext.EmailRecipients.AddAsync(recipient);
                await _dbContext.SaveChangesAsync();

                var dto = new EmailRecipientDto
                {
                    Id = recipient.Id,
                    Email = recipient.Email,
                    Name = recipient.Name,
                    Department = recipient.Department,
                    IsActive = recipient.IsActive,
                    CreatedAt = recipient.CreatedAt
                };

                return StatusCode(201, ApiResponse<EmailRecipientDto>.Ok(dto, "Email recipient added successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add email recipient");
                return StatusCode(500, ApiResponse.Fail("Failed to add email recipient."));
            }
        }

        [HttpPut("email-recipients/{id}")]
        public async Task<IActionResult> UpdateEmailRecipient(long id, [FromBody] UpdateEmailRecipientRequest request)
        {
            try
            {
                if (GetUserRole() != "Super Admin")
                {
                    return StatusCode(403, ApiResponse.Fail("Only Super Admin can update email recipients."));
                }

                var companyId = GetCompanyId();
                var recipient = await _dbContext.EmailRecipients
                    .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId);

                if (recipient == null) return NotFound(ApiResponse.Fail("Email recipient not found."));

                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    recipient.Email = request.Email.Trim().ToLowerInvariant();
                }
                if (request.Name != null)
                {
                    recipient.Name = request.Name.Trim();
                }
                if (request.Department != null)
                {
                    recipient.Department = request.Department.Trim();
                }
                if (request.IsActive.HasValue)
                {
                    recipient.IsActive = request.IsActive.Value;
                }

                recipient.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                var dto = new EmailRecipientDto
                {
                    Id = recipient.Id,
                    Email = recipient.Email,
                    Name = recipient.Name,
                    Department = recipient.Department,
                    IsActive = recipient.IsActive,
                    CreatedAt = recipient.CreatedAt
                };

                return Ok(ApiResponse<EmailRecipientDto>.Ok(dto, "Email recipient updated successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update email recipient {RecipientId}", id);
                return StatusCode(500, ApiResponse.Fail("Failed to update email recipient."));
            }
        }

        [HttpDelete("email-recipients/{id}")]
        public async Task<IActionResult> RemoveEmailRecipient(long id)
        {
            try
            {
                if (GetUserRole() != "Super Admin")
                {
                    return StatusCode(403, ApiResponse.Fail("Only Super Admin can remove email recipients."));
                }

                var companyId = GetCompanyId();
                var recipient = await _dbContext.EmailRecipients
                    .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId);

                if (recipient == null) return NotFound(ApiResponse.Fail("Email recipient not found."));

                _dbContext.EmailRecipients.Remove(recipient);
                await _dbContext.SaveChangesAsync();

                return Ok(ApiResponse.Ok("Email recipient removed successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove email recipient {RecipientId}", id);
                return StatusCode(500, ApiResponse.Fail("Failed to remove email recipient."));
            }
        }

        // ── 3. NOTIFICATION RULES ──

        [HttpGet("notifications/rules")]
        public async Task<IActionResult> GetNotificationRules()
        {
            try
            {
                var companyId = GetCompanyId();
                var rules = await _dbContext.NotificationRules
                    .Where(r => r.CompanyId == companyId || r.CompanyId == null)
                    .Select(r => new NotificationRuleDto
                    {
                        Id = r.Id,
                        Category = r.Category,
                        EventType = r.EventType,
                        DisplayName = r.DisplayName,
                        SendEmail = r.SendEmail
                    })
                    .ToListAsync();

                if (rules.Count == 0)
                {
                    rules = GetDefaultNotificationRules();
                }

                return Ok(ApiResponse<IEnumerable<NotificationRuleDto>>.Ok(rules, "Notification rules retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get notification rules");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve notification rules."));
            }
        }

        [HttpPut("notifications/rules")]
        public async Task<IActionResult> UpdateNotificationRules([FromBody] UpdateNotificationRulesRequest request)
        {
            try
            {
                if (GetUserRole() != "Super Admin")
                {
                    return StatusCode(403, ApiResponse.Fail("Only Super Admin can update notification rules."));
                }

                var companyId = GetCompanyId();
                var existing = await _dbContext.NotificationRules
                    .Where(r => r.CompanyId == companyId)
                    .ToListAsync();

                foreach (var dto in request.Rules)
                {
                    var rule = existing.FirstOrDefault(r => r.EventType == dto.EventType);
                    if (rule != null)
                    {
                        rule.SendEmail = dto.SendEmail;
                        rule.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        _dbContext.NotificationRules.Add(new NotificationRule
                        {
                            CompanyId = companyId,
                            Category = dto.Category,
                            EventType = dto.EventType,
                            DisplayName = dto.DisplayName,
                            SendEmail = dto.SendEmail,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _dbContext.SaveChangesAsync();
                return Ok(ApiResponse.Ok("Notification rules saved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save notification rules");
                return StatusCode(500, ApiResponse.Fail("Failed to save notification rules."));
            }
        }

        // ── 4. EMAIL DELIVERY LOGS ──

        [HttpGet("notifications/logs")]
        public async Task<IActionResult> GetEmailLogs([FromQuery] int page = 1, [FromQuery] int per_page = 20)
        {
            try
            {
                var companyId = GetCompanyId();
                var query = _dbContext.EmailLogs
                    .Where(l => l.CompanyId == companyId || l.CompanyId == null)
                    .OrderByDescending(l => l.CreatedAt);

                var total = await query.CountAsync();
                per_page = Math.Min(Math.Max(1, per_page), 100);
                page = Math.Max(1, page);

                var items = await query
                    .Skip((page - 1) * per_page)
                    .Take(per_page)
                    .Select(l => new EmailLogDto
                    {
                        Id = l.Id,
                        RecipientEmail = l.RecipientEmail,
                        Subject = l.Subject,
                        Status = l.Status,
                        SentAt = l.SentAt,
                        FailureReason = l.FailureReason,
                        RetryCount = l.RetryCount,
                        CreatedAt = l.CreatedAt
                    })
                    .ToListAsync();

                return Ok(ApiResponse<PaginatedResult<EmailLogDto>>.Ok(new PaginatedResult<EmailLogDto>
                {
                    Data = items,
                    Total = total,
                    Page = page,
                    PerPage = per_page,
                    TotalPages = (int)Math.Ceiling((double)total / per_page)
                }, "Email logs retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get email logs");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve email logs."));
            }
        }

        private static List<NotificationRuleDto> GetDefaultNotificationRules()
        {
            return new List<NotificationRuleDto>
            {
                // Critical Alerts
                new() { Category = "Critical Alerts", EventType = "cpu_usage", DisplayName = "CPU Usage Critical Threshold", SendEmail = true },
                new() { Category = "Critical Alerts", EventType = "ram_usage", DisplayName = "RAM Usage Critical Threshold", SendEmail = true },
                new() { Category = "Critical Alerts", EventType = "disk_usage", DisplayName = "Disk Low Space Warning", SendEmail = true },
                new() { Category = "Critical Alerts", EventType = "agent_offline", DisplayName = "Agent Went Offline", SendEmail = true },

                // Security Events
                new() { Category = "Security Events", EventType = "firewall_disabled", DisplayName = "Windows Firewall Disabled", SendEmail = true },
                new() { Category = "Security Events", EventType = "antivirus_disabled", DisplayName = "Antivirus Protection Disabled", SendEmail = true },
                new() { Category = "Security Events", EventType = "login_lockout", DisplayName = "Failed Login Account Lockout", SendEmail = true },

                // Change Detection Events
                new() { Category = "Change Detection Events", EventType = "hardware_changed", DisplayName = "Hardware Baseline Change Detected", SendEmail = true },
                new() { Category = "Change Detection Events", EventType = "software_installed", DisplayName = "Unauthorized Software Installed", SendEmail = true },
                new() { Category = "Change Detection Events", EventType = "usb_connected", DisplayName = "USB Storage Device Connected", SendEmail = true }
            };
        }
    }
}
