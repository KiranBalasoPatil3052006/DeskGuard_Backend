using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeskGuardBackend.DTOs.Auth;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.Services;
using DeskGuardBackend.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace DeskGuardBackend.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly IOtpService _otpService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            IUserService userService,
            IOtpService otpService,
            IJwtTokenService jwtTokenService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _userService = userService;
            _otpService = otpService;
            _jwtTokenService = jwtTokenService;
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
                    EmployeeId = user.EmployeeId,
                    IsActive = user.IsActive,
                    Role = user.UserRoles?.FirstOrDefault()?.Role?.Name ?? "User",
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
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
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] DeskGuardBackend.DTOs.Profile.ChangePasswordRequest request)
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
                {
                    return Unauthorized(ApiResponse.Fail("Unauthorized."));
                }

                await _userService.ChangePasswordAsync(userId, request);
                return Ok(ApiResponse.Ok("Password changed successfully."));
            }
            catch (Exceptions.BaseException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change password");
                return StatusCode(500, ApiResponse.Fail("Failed to change password."));
            }
        }

        [AllowAnonymous]
        [HttpPost("customer-request-otp")]
        public async Task<IActionResult> CustomerRequestOtp([FromBody] OtpRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.MobileNumber))
                {
                    return BadRequest(ApiResponse.Fail("Please enter a valid 10-digit mobile number."));
                }

                var cleanMobile = request.MobileNumber.Trim().Replace(" ", "").Replace("-", "").Replace("+91", "");
                if (!DevelopmentOtpService.IsValidMobileFormat(cleanMobile))
                {
                    return BadRequest(ApiResponse.Fail("Please enter a valid 10-digit mobile number."));
                }

                var isRegistered = await _otpService.IsCustomerRegisteredAsync(cleanMobile);
                if (!isRegistered)
                {
                    return NotFound(ApiResponse.Fail("No customer account is registered with this mobile number. Please contact your AMC administrator."));
                }

                var result = await _otpService.GenerateOtpAsync(cleanMobile);
                return Ok(ApiResponse<object>.Ok(result, "OTP sent successfully."));
            }
            catch (Exceptions.UnauthorizedActionException ex)
            {
                return StatusCode(ex.StatusCode, ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request OTP for mobile {Mobile}", request?.MobileNumber);
                return StatusCode(500, ApiResponse.Fail("Failed to send OTP."));
            }
        }

        [AllowAnonymous]
        [HttpPost("customer-verify-otp")]
        public async Task<IActionResult> CustomerVerifyOtp([FromBody] OtpVerifyRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.MobileNumber) || string.IsNullOrWhiteSpace(request.Otp))
                {
                    return BadRequest(ApiResponse.Fail("Mobile number and OTP are required."));
                }

                var cleanMobile = request.MobileNumber.Trim().Replace(" ", "").Replace("-", "").Replace("+91", "");
                if (!DevelopmentOtpService.IsValidMobileFormat(cleanMobile))
                {
                    return BadRequest(ApiResponse.Fail("Please enter a valid 10-digit mobile number."));
                }

                var isValidOtp = await _otpService.VerifyOtpAsync(cleanMobile, request.Otp);
                if (!isValidOtp)
                {
                    return BadRequest(ApiResponse.Fail("Invalid OTP. Development Mode: Use OTP 111111."));
                }

                var user = await _otpService.FindOrCreateUserAsync(cleanMobile);
                var token = _jwtTokenService.GenerateToken(user);

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
                    Role = user.UserRoles?.FirstOrDefault()?.Role?.Name ?? "Customer"
                };

                var response = new OtpVerifyResponse
                {
                    Token = token,
                    User = userDto
                };

                return Ok(ApiResponse<OtpVerifyResponse>.Ok(response, "OTP verified successfully."));
            }
            catch (Exception ex)
            {
                var detailedError = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError(ex, "Customer OTP verification failed for mobile {Mobile}. Cause: {Cause}", request?.MobileNumber, detailedError);
                return StatusCode(500, ApiResponse.Fail($"OTP verification failed: {detailedError}"));
            }
        }
    }
}
