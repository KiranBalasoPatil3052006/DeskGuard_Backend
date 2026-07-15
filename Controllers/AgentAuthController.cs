using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DeskGuardBackend.DTOs.Auth;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.Services;
using DeskGuardBackend.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace DeskGuardBackend.Controllers
{
    [ApiController]
    [Route("api/v1/agent")]
    public class AgentAuthController : ControllerBase
    {
        private readonly IOtpService _otpService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<AgentAuthController> _logger;

        public AgentAuthController(
            IOtpService otpService,
            IJwtTokenService jwtTokenService,
            ILogger<AgentAuthController> logger)
        {
            _otpService = otpService;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        [HttpPost("request-otp")]
        public async Task<IActionResult> RequestOtp([FromBody] OtpRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.MobileNumber))
                {
                    return BadRequest(ApiResponse.Fail("Mobile number is required."));
                }

                var result = await _otpService.GenerateOtpAsync(request.MobileNumber);
                return Ok(ApiResponse<object>.Ok(result, "OTP sent successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP for mobile {Mobile}", request.MobileNumber);
                return StatusCode(500, ApiResponse.Fail("Failed to send OTP."));
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpVerifyRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.MobileNumber) || string.IsNullOrEmpty(request.Otp))
                {
                    return BadRequest(ApiResponse.Fail("Mobile number and OTP are required."));
                }

                var otpRecord = await _otpService.VerifyOtpAsync(request.MobileNumber, request.Otp);
                if (otpRecord == null)
                {
                    return UnprocessableEntity(ApiResponse.Fail("Invalid or expired OTP."));
                }

                var user = await _otpService.FindOrCreateUserAsync(request.MobileNumber);
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
                    Role = user.UserRoles?.FirstOrDefault()?.Role?.Name ?? "User"
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
                _logger.LogError(ex, "OTP verification failed for mobile {Mobile}", request.MobileNumber);
                return StatusCode(500, ApiResponse.Fail("OTP verification failed."));
            }
        }
    }
}
