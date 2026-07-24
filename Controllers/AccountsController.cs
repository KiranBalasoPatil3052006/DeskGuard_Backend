using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.DTOs.Account;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.Exceptions;
using DeskGuardBackend.Services.Interfaces;

namespace DeskGuardBackend.Controllers
{
    [ApiController]
    [Route("api/v1/accounts")]
    [Authorize]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountsController> _logger;

        public AccountsController(IAccountService accountService, ILogger<AccountsController> logger)
        {
            _accountService = accountService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAccountRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                if (currentUserRole != "Super Admin")
                    return Forbid();

                var result = await _accountService.CreateAsync(request, currentUserId);
                return Ok(ApiResponse<AccountDto>.Ok(result, "Account created successfully."));
            }
            catch (AccountException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message, ex.Errors));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create account");
                return StatusCode(500, ApiResponse.Fail("An unexpected error occurred."));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] AccountFilterRequest filter)
        {
            try
            {
                var currentUserRole = GetCurrentUserRole();
                if (currentUserRole != "Super Admin")
                    return Forbid();

                var result = await _accountService.GetAllAsync(filter);
                return Ok(ApiResponse<AccountListResponse>.Ok(result, "Accounts retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve accounts");
                return StatusCode(500, ApiResponse.Fail("An unexpected error occurred."));
            }
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var result = await _accountService.GetByIdAsync(id);
                return Ok(ApiResponse<AccountDto>.Ok(result, "Account retrieved successfully."));
            }
            catch (AccountException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve account {Id}", id);
                return StatusCode(500, ApiResponse.Fail("An unexpected error occurred."));
            }
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateAccountRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                if (currentUserRole != "Super Admin")
                    return Forbid();

                var result = await _accountService.UpdateAsync(id, request, currentUserId);
                return Ok(ApiResponse<AccountDto>.Ok(result, "Account updated successfully."));
            }
            catch (AccountException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message, ex.Errors));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update account {Id}", id);
                return StatusCode(500, ApiResponse.Fail("An unexpected error occurred."));
            }
        }

        [HttpPost("{id:long}/reset-password")]
        public async Task<IActionResult> ResetPassword(long id, [FromBody] ResetPasswordRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                if (currentUserRole != "Super Admin")
                    return Forbid();

                await _accountService.ResetPasswordAsync(id, request, currentUserId);
                return Ok(ApiResponse.Ok("Account password reset successfully."));
            }
            catch (AccountException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message, ex.Errors));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset password for account {Id}", id);
                return StatusCode(500, ApiResponse.Fail("An unexpected error occurred."));
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                if (currentUserRole != "Super Admin")
                    return Forbid();

                await _accountService.DeleteAsync(id, currentUserId);
                return Ok(ApiResponse.Ok("Account deleted successfully."));
            }
            catch (AccountException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete account {Id}", id);
                return StatusCode(500, ApiResponse.Fail("An unexpected error occurred."));
            }
        }

        [HttpPatch("{id:long}/disable")]
        public async Task<IActionResult> Disable(long id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                if (currentUserRole != "Super Admin")
                    return Forbid();

                await _accountService.DisableAsync(id, currentUserId);
                return Ok(ApiResponse.Ok("Account disabled successfully."));
            }
            catch (AccountException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to disable account {Id}", id);
                return StatusCode(500, ApiResponse.Fail("An unexpected error occurred."));
            }
        }

        [HttpPatch("{id:long}/enable")]
        public async Task<IActionResult> Enable(long id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                if (currentUserRole != "Super Admin")
                    return Forbid();

                await _accountService.EnableAsync(id, currentUserId);
                return Ok(ApiResponse.Ok("Account enabled successfully."));
            }
            catch (AccountException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enable account {Id}", id);
                return StatusCode(500, ApiResponse.Fail("An unexpected error occurred."));
            }
        }

        [HttpGet("employee-id/next")]
        public async Task<IActionResult> GetNextEmployeeId()
        {
            try
            {
                var empId = await _accountService.GenerateEmployeeIdAsync();
                return Ok(ApiResponse<object>.Ok(new { employee_id = empId }, "Employee ID generated."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate employee ID");
                return StatusCode(500, ApiResponse.Fail("An unexpected error occurred."));
            }
        }

        private long GetCurrentUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("id")?.Value
                ?? User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
                throw new UnauthorizedActionException("Unauthorized.", 401);
            return userId;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        }
    }
}
