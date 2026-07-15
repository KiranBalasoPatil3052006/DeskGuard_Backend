using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DeskGuardBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/settings")]
    public class SettingsController : ControllerBase
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(DeskGuardDbContext dbContext, ILogger<SettingsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        private long GetCompanyId()
        {
            var compIdStr = User.FindFirst("CompanyId")?.Value;
            if (string.IsNullOrEmpty(compIdStr) || !long.TryParse(compIdStr, out var companyId))
            {
                return 1;
            }
            return companyId;
        }

        [HttpGet("email-recipients")]
        public async Task<IActionResult> ListEmailRecipients()
        {
            try
            {
                var companyId = GetCompanyId();
                var recipients = await _dbContext.EmailRecipients
                    .Where(r => r.CompanyId == companyId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
                return Ok(ApiResponse<IEnumerable<EmailRecipient>>.Ok(recipients, "Email recipients retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list email recipients");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve email recipients."));
            }
        }

        [HttpPost("email-recipients")]
        public async Task<IActionResult> AddEmailRecipient([FromBody] JsonElement body)
        {
            try
            {
                if (!body.TryGetProperty("email", out var emailProp) || string.IsNullOrEmpty(emailProp.GetString()))
                {
                    return BadRequest(ApiResponse.Fail("email is required."));
                }

                var email = emailProp.GetString()!;
                var name = body.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;

                var companyId = GetCompanyId();

                var exists = await _dbContext.EmailRecipients
                    .AnyAsync(r => r.CompanyId == companyId && r.Email == email);

                if (exists)
                {
                    return UnprocessableEntity(ApiResponse.Fail("This email address is already registered."));
                }

                var recipient = new EmailRecipient
                {
                    CompanyId = companyId,
                    Email = email,
                    Name = name,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _dbContext.EmailRecipients.AddAsync(recipient);
                await _dbContext.SaveChangesAsync();

                return StatusCode(201, ApiResponse<EmailRecipient>.Ok(recipient, "Email recipient added successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add email recipient");
                return StatusCode(500, ApiResponse.Fail("Failed to add email recipient."));
            }
        }

        [HttpPut("email-recipients/{id}")]
        public async Task<IActionResult> UpdateEmailRecipient(long id, [FromBody] JsonElement body)
        {
            try
            {
                var companyId = GetCompanyId();
                var recipient = await _dbContext.EmailRecipients
                    .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId);

                if (recipient == null) return NotFound(ApiResponse.Fail("Email recipient not found."));

                if (body.TryGetProperty("email", out var emailProp) && !string.IsNullOrEmpty(emailProp.GetString()))
                {
                    recipient.Email = emailProp.GetString()!;
                }
                if (body.TryGetProperty("name", out var nameProp))
                {
                    recipient.Name = nameProp.GetString();
                }
                if (body.TryGetProperty("is_active", out var activeProp))
                {
                    recipient.IsActive = activeProp.GetBoolean();
                }

                recipient.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                return Ok(ApiResponse<EmailRecipient>.Ok(recipient, "Email recipient updated successfully."));
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

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotificationSettings()
        {
            try
            {
                var companyId = GetCompanyId();
                var recipients = await _dbContext.EmailRecipients
                    .Where(r => r.CompanyId == companyId && r.IsActive == true)
                    .ToListAsync();

                return Ok(ApiResponse<object>.Ok(new
                {
                    email_recipients = recipients,
                    email_alerts_enabled = true,
                    alert_severity_filters = new Dictionary<string, bool>
                    {
                        { "critical", true },
                        { "warning", true },
                        { "info", false }
                    }
                }, "Notification settings retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get notification settings");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve notification settings."));
            }
        }
    }
}
