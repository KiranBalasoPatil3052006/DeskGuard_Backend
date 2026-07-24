using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.DTOs.Security;
using DeskGuardBackend.Exceptions;
using DeskGuardBackend.Services.Interfaces;

namespace DeskGuardBackend.Controllers
{
    [ApiController]
    [Route("api/v1/security")]
    [Route("api/security")]
    [Authorize]
    public class SecurityController : ControllerBase
    {
        private readonly ISecurityService _securityService;

        public SecurityController(ISecurityService securityService)
        {
            _securityService = securityService;
        }

        /// <summary>
        /// GET /api/v1/security/settings
        /// Get active security settings for password policy, session timeout, and lockout rules.
        /// </summary>
        [HttpGet("settings")]
        public async Task<ActionResult<ApiResponse<SecuritySettingDto>>> GetSecuritySettings()
        {
            var companyId = GetCompanyIdFromClaims();
            var settings = await _securityService.GetSecuritySettingsAsync(companyId);
            return Ok(ApiResponse<SecuritySettingDto>.Ok(settings, "Security settings retrieved successfully."));
        }

        /// <summary>
        /// PUT /api/v1/security/settings
        /// Update system security settings. Restricted to Super Admin role.
        /// </summary>
        [HttpPut("settings")]
        [Authorize(Roles = "Super Admin")]
        public async Task<ActionResult<ApiResponse<SecuritySettingDto>>> UpdateSecuritySettings([FromBody] UpdateSecuritySettingRequest request)
        {
            var userId = GetUserIdFromClaims();
            var companyId = GetCompanyIdFromClaims();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var updated = await _securityService.UpdateSecuritySettingsAsync(request, userId, ipAddress, userAgent, companyId);
            return Ok(ApiResponse<SecuritySettingDto>.Ok(updated, "Security settings updated successfully."));
        }

        /// <summary>
        /// GET /api/v1/security/login-history
        /// Get paginated web user login history.
        /// </summary>
        [HttpGet("login-history")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<UserLoginHistoryDto>>>> GetLoginHistory(
            [FromQuery] int page = 1,
            [FromQuery] int per_page = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null)
        {
            var companyId = GetCompanyIdFromClaims();
            var result = await _securityService.GetLoginHistoryAsync(page, per_page, search, status, companyId);
            return Ok(ApiResponse<PaginatedResult<UserLoginHistoryDto>>.Ok(result, "Login history retrieved successfully."));
        }

        /// <summary>
        /// GET /api/v1/security/audit
        /// Get paginated security audit logs.
        /// </summary>
        [HttpGet("audit")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<SecurityAuditLogDto>>>> GetSecurityAuditLogs(
            [FromQuery] int page = 1,
            [FromQuery] int per_page = 20,
            [FromQuery] string? search = null)
        {
            var companyId = GetCompanyIdFromClaims();
            var result = await _securityService.GetSecurityAuditLogsAsync(page, per_page, search, companyId);
            return Ok(ApiResponse<PaginatedResult<SecurityAuditLogDto>>.Ok(result, "Security audit logs retrieved successfully."));
        }

        private long GetUserIdFromClaims()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (claim != null && long.TryParse(claim.Value, out var userId))
            {
                return userId;
            }
            throw new UnauthorizedActionException("Invalid user token claim.");
        }

        private long? GetCompanyIdFromClaims()
        {
            var claim = User.FindFirst("company_id") ?? User.FindFirst("CompanyId");
            if (claim != null && long.TryParse(claim.Value, out var companyId))
            {
                return companyId;
            }
            return null;
        }
    }
}
