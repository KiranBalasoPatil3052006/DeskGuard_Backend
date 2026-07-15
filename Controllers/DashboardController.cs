using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace DeskGuardBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
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

        [HttpGet("company")]
        public async Task<IActionResult> Company()
        {
            try
            {
                var companyId = GetCompanyId();
                var data = await _dashboardService.GetCompanyDashboardAsync(companyId);
                return Ok(ApiResponse<object>.Ok(data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build company dashboard");
                return StatusCode(500, ApiResponse.Fail("Failed to build dashboard."));
            }
        }

        [HttpGet("employee")]
        public async Task<IActionResult> Employee()
        {
            try
            {
                var userId = GetUserId();
                var data = await _dashboardService.GetEmployeeDashboardAsync(userId);
                return Ok(ApiResponse<object>.Ok(data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build employee dashboard");
                return StatusCode(500, ApiResponse.Fail("Failed to build employee dashboard."));
            }
        }

        [HttpGet("charts/cpu")]
        public async Task<IActionResult> CpuTrend([FromQuery] int hours = 24)
        {
            try
            {
                var companyId = GetCompanyId();
                var data = await _dashboardService.GetCpuChartDataAsync(companyId, hours);
                return Ok(ApiResponse<object>.Ok(data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get CPU trend chart data");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve CPU trend."));
            }
        }

        [HttpGet("charts/ram")]
        public async Task<IActionResult> RamTrend([FromQuery] int hours = 24)
        {
            try
            {
                var companyId = GetCompanyId();
                var data = await _dashboardService.GetRamChartDataAsync(companyId, hours);
                return Ok(ApiResponse<object>.Ok(data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get RAM trend chart data");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve RAM trend."));
            }
        }

        [HttpGet("charts/alerts")]
        public async Task<IActionResult> AlertTrend([FromQuery] int days = 7)
        {
            try
            {
                var companyId = GetCompanyId();
                var data = await _dashboardService.GetAlertChartDataAsync(companyId, days);
                return Ok(ApiResponse<object>.Ok(data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get Alert trend chart data");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve Alert trend."));
            }
        }
    }
}
