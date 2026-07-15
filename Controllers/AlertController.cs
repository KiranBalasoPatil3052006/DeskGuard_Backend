using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Services.Interfaces;
using DeskGuardBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DeskGuardBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/alerts")]
    public class AlertController : ControllerBase
    {
        private readonly IAlertService _alertService;
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<AlertController> _logger;

        public AlertController(
            IAlertService alertService,
            DeskGuardDbContext dbContext,
            ILogger<AlertController> logger)
        {
            _alertService = alertService;
            _dbContext = dbContext;
            _logger = logger;
        }

        private long GetCompanyId()
        {
            var compIdStr = User.FindFirst("CompanyId")?.Value;
            if (string.IsNullOrEmpty(compIdStr) || !long.TryParse(compIdStr, out var companyId))
            {
                return 1; // Fallback to company ID 1 for dev
            }
            return companyId;
        }

        private long GetUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdStr, out var userId) ? userId : 0;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string? severity = null, [FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int per_page = 15)
        {
            try
            {
                var companyId = GetCompanyId();
                var result = await _alertService.GetCompanyAlertsAsync(companyId, severity, status, page, per_page);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get company alerts");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve alerts."));
            }
        }

        [HttpGet("critical")]
        public async Task<IActionResult> Critical()
        {
            try
            {
                var companyId = GetCompanyId();
                var alerts = await _alertService.GetCriticalAlertsAsync(companyId);
                return Ok(ApiResponse<IEnumerable<Alert>>.Ok(alerts));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get critical alerts");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve critical alerts."));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Show(long id)
        {
            try
            {
                var alert = await _dbContext.Alerts
                    .Include(a => a.Machine)
                    .Include(a => a.Acknowledger)
                    .Include(a => a.Resolver)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (alert == null) return NotFound(ApiResponse.Fail("Alert not found."));
                return Ok(ApiResponse<Alert>.Ok(alert));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get alert {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve alert details."));
            }
        }

        [HttpPost("{id}/acknowledge")]
        public async Task<IActionResult> Acknowledge(long id)
        {
            try
            {
                var userId = GetUserId();
                var alert = await _alertService.AcknowledgeAlertAsync(id, userId);
                return Ok(ApiResponse<Alert>.Ok(alert, "Alert acknowledged successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to acknowledge alert {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Failed to acknowledge alert."));
            }
        }

        [HttpPost("{id}/resolve")]
        public async Task<IActionResult> Resolve(long id, [FromBody] JsonElement body)
        {
            try
            {
                var userId = GetUserId();
                var note = body.TryGetProperty("note", out var noteProp) ? noteProp.GetString() : null;
                var alert = await _alertService.ResolveAlertAsync(id, userId, note);
                return Ok(ApiResponse<Alert>.Ok(alert, "Alert resolved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve alert {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Failed to resolve alert."));
            }
        }
    }

    [Authorize]
    [ApiController]
    [Route("api/v1/alert-rules")]
    public class AlertRulesController : ControllerBase
    {
        private readonly IAlertService _alertService;
        private readonly ILogger<AlertRulesController> _logger;

        public AlertRulesController(IAlertService alertService, ILogger<AlertRulesController> logger)
        {
            _alertService = alertService;
            _logger = logger;
        }

        private long GetCompanyId()
        {
            var compIdStr = User.FindFirst("CompanyId")?.Value;
            if (string.IsNullOrEmpty(compIdStr) || !long.TryParse(compIdStr, out var companyId))
            {
                return 1; // Fallback to company ID 1 for dev
            }
            return companyId;
        }

        [HttpGet]
        public async Task<IActionResult> Rules()
        {
            try
            {
                var companyId = GetCompanyId();
                var rules = await _alertService.GetAlertRulesAsync(companyId);
                return Ok(ApiResponse<IEnumerable<AlertRule>>.Ok(rules));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get alert rules");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve alert rules."));
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRule(long id, [FromBody] Dictionary<string, object> body)
        {
            try
            {
                var rule = await _alertService.UpdateAlertRuleAsync(id, body);
                return Ok(ApiResponse<AlertRule>.Ok(rule, "Alert rule updated successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update alert rule {Id}", id);
                return StatusCode(500, ApiResponse.Fail("Failed to update alert rule."));
            }
        }
    }
}
