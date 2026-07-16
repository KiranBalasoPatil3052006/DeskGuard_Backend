using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeskGuardBackend.DTOs.Auth;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace DeskGuardBackend.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var response = await _authService.RegisterAsync(request);
                return Ok(ApiResponse<LoginResponse>.Ok(response, "Registration successful."));
            }
            catch (Exceptions.UnauthorizedActionException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for email {Email}", request.Email);
                return StatusCode(500, ApiResponse.Fail("An unexpected error occurred during registration."));
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);
                return Ok(ApiResponse<LoginResponse>.Ok(response, "Login successful."));
            }
            catch (Exceptions.UnauthorizedActionException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for email {Email}", request.Email);
                return StatusCode(500, ApiResponse.Fail("An unexpected error occurred during login."));
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
                {
                    return Unauthorized(ApiResponse.Fail("Unauthorized."));
                }

                var user = await _authService.GetAuthenticatedUserAsync(userId);
                await _authService.LogoutAsync(user);

                return Ok(ApiResponse.Ok("Logged out successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed");
                return StatusCode(500, ApiResponse.Fail("Logout failed."));
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
                {
                    return Unauthorized(ApiResponse.Fail("Unauthorized."));
                }

                var user = await _authService.GetAuthenticatedUserAsync(userId);
                
                var userDto = new UserDto
                {
                    Id = user.Id,
                    CompanyId = user.CompanyId,
                    Name = user.Name,
                    Email = user.Email,
                    MobileNumber = user.MobileNumber,
                    Phone = user.Phone,
                    Avatar = user.Avatar,
                    IsActive = user.IsActive,
                    Role = user.UserRoles?.FirstOrDefault()?.Role?.Name ?? "User"
                };

                return Ok(ApiResponse<UserDto>.Ok(userDto, "Authenticated user retrieved."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve current user info");
                return StatusCode(500, ApiResponse.Fail("Unable to retrieve authenticated user."));
            }
        }

        [Authorize]
        [HttpGet("user")] // Route matching React frontend `getUser()` Service path `/auth/user`
        public async Task<IActionResult> GetUser()
        {
            return await Me();
        }

        [Authorize]
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
                {
                    return Unauthorized(ApiResponse.Fail("Unauthorized."));
                }

                var user = await _authService.GetAuthenticatedUserAsync(userId);
                var token = await _authService.RefreshTokenAsync(user);

                return Ok(ApiResponse<object>.Ok(new { token = token }, "Token refreshed successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                return StatusCode(500, ApiResponse.Fail("Failed to refresh token."));
            }
        }
    }
}
