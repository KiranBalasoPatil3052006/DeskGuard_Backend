using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.DTOs.Profile;
using DeskGuardBackend.Exceptions;
using DeskGuardBackend.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace DeskGuardBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/profile")]
    [Route("api/profile")]
    public class ProfileController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(IUserService userService, ILogger<ProfileController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        private long GetAuthenticatedUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("id")?.Value
                ?? User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
            {
                throw new UnauthorizedActionException("Unauthorized user session.", 401);
            }
            return userId;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = GetAuthenticatedUserId();
                var profile = await _userService.GetProfileAsync(userId);
                return Ok(ApiResponse<ProfileDto>.Ok(profile, "User profile retrieved successfully."));
            }
            catch (BaseException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message));
            }
            catch (System.Collections.Generic.KeyNotFoundException ex)
            {
                return StatusCode(404, ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve authenticated user profile");
                return StatusCode(500, ApiResponse.Fail("Unable to retrieve user profile."));
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var userId = GetAuthenticatedUserId();
                var updatedProfile = await _userService.UpdateProfileAsync(userId, request);
                return Ok(ApiResponse<ProfileDto>.Ok(updatedProfile, "User profile updated successfully."));
            }
            catch (BaseException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user profile");
                return StatusCode(500, ApiResponse.Fail("Failed to update user profile."));
            }
        }

        [HttpPatch("password")]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userId = GetAuthenticatedUserId();
                await _userService.ChangePasswordAsync(userId, request);
                return Ok(ApiResponse.Ok("Password changed successfully."));
            }
            catch (BaseException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change password");
                return StatusCode(500, ApiResponse.Fail("Failed to change password."));
            }
        }
    }
}
