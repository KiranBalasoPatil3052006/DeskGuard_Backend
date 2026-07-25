using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.Services.Interfaces;

namespace DeskGuardBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/customer-portal")]
    public class CustomerPortalController : ControllerBase
    {
        private readonly ICustomerPortalService _customerPortalService;
        private readonly ILogger<CustomerPortalController> _logger;

        public CustomerPortalController(ICustomerPortalService customerPortalService, ILogger<CustomerPortalController> logger)
        {
            _customerPortalService = customerPortalService;
            _logger = logger;
        }

        private (string mobileNumber, long? userId, long? companyId) GetCustomerContext()
        {
            var mobileNumber = User.FindFirst("MobileNumber")?.Value
                ?? User.FindFirst(ClaimTypes.MobilePhone)?.Value
                ?? User.FindFirst("mobile_number")?.Value
                ?? User.FindFirst(ClaimTypes.Name)?.Value
                ?? string.Empty;

            long? userId = null;
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdStr) && long.TryParse(userIdStr, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            long? companyId = null;
            var compIdStr = User.FindFirst("CompanyId")?.Value;
            if (!string.IsNullOrEmpty(compIdStr) && long.TryParse(compIdStr, out var parsedCompId))
            {
                companyId = parsedCompId;
            }

            return (mobileNumber, userId, companyId);
        }

        /// <summary>
        /// GET /api/v1/customer-portal/dashboard
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var (mobileNumber, userId, companyId) = GetCustomerContext();
                var data = await _customerPortalService.GetCustomerDashboardAsync(mobileNumber, userId, companyId);
                return Ok(ApiResponse<object>.Ok(data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build customer dashboard");
                return StatusCode(500, ApiResponse.Fail("An error occurred while loading customer dashboard."));
            }
        }

        /// <summary>
        /// GET /api/v1/customer-portal/systems
        /// </summary>
        [HttpGet("systems")]
        public async Task<IActionResult> GetSystems(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? sortBy,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 10)
        {
            try
            {
                var (mobileNumber, userId, companyId) = GetCustomerContext();
                var data = await _customerPortalService.GetCustomerSystemsAsync(mobileNumber, userId, companyId, search, status, sortBy, page, perPage);
                return Ok(ApiResponse<object>.Ok(data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get customer systems list");
                return StatusCode(500, ApiResponse.Fail("An error occurred while fetching customer systems."));
            }
        }

        /// <summary>
        /// GET /api/v1/customer-portal/systems/{id}
        /// </summary>
        [HttpGet("systems/{id}")]
        public async Task<IActionResult> GetMachineOverview(long id)
        {
            try
            {
                var (mobileNumber, userId, companyId) = GetCustomerContext();
                var data = await _customerPortalService.GetMachineOverviewAsync(id, mobileNumber, userId, companyId);
                if (data == null)
                {
                    return NotFound(ApiResponse.Fail("System not found or access denied."));
                }
                return Ok(ApiResponse<object>.Ok(data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get machine overview for ID: {MachineId}", id);
                return StatusCode(500, ApiResponse.Fail("An error occurred while loading system details."));
            }
        }

        /// <summary>
        /// GET /api/v1/customer-portal/alerts
        /// </summary>
        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts(
            [FromQuery] string? search,
            [FromQuery] string? severity,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 10)
        {
            try
            {
                var (mobileNumber, userId, companyId) = GetCustomerContext();
                var data = await _customerPortalService.GetCustomerAlertsAsync(mobileNumber, userId, companyId, search, severity, page, perPage);
                return Ok(ApiResponse<object>.Ok(data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get customer alerts");
                return StatusCode(500, ApiResponse.Fail("An error occurred while loading customer alerts."));
            }
        }

        /// <summary>
        /// GET /api/v1/customer-portal/profile
        /// </summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var (mobileNumber, userId, companyId) = GetCustomerContext();
                var data = await _customerPortalService.GetCustomerProfileAsync(mobileNumber, userId, companyId);
                return Ok(ApiResponse<object>.Ok(data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get customer profile");
                return StatusCode(500, ApiResponse.Fail("An error occurred while loading customer profile."));
            }
        }

        /// <summary>
        /// GET /api/v1/customer-portal/support
        /// </summary>
        [HttpGet("support")]
        public async Task<IActionResult> GetSupportInfo()
        {
            try
            {
                var data = await _customerPortalService.GetCustomerSupportInfoAsync();
                return Ok(ApiResponse<object>.Ok(data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get customer support info");
                return StatusCode(500, ApiResponse.Fail("An error occurred while loading support details."));
            }
        }
    }
}
