using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.DTOs.Auth;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Exceptions;
using DeskGuardBackend.Services.Interfaces;
using DeskGuardBackend.Enums;

namespace DeskGuardBackend.Services
{
    public class AuthService : IAuthService
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            DeskGuardDbContext dbContext,
            IJwtTokenService jwtTokenService,
            IAuditLogService auditLogService,
            ILogger<AuthService> logger)
        {
            _dbContext = dbContext;
            _jwtTokenService = jwtTokenService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    throw new UnauthorizedActionException("Email and password are required.", 422);
                }

                var user = await _dbContext.Users
                    .Include(u => u.Company)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null || string.IsNullOrEmpty(user.Password) || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                {
                    await _auditLogService.LogAsync(
                        EventType.Login.ToString(),
                        $"Failed login attempt for email: {request.Email}",
                        user: user
                    );

                    throw new UnauthorizedActionException("The provided credentials are incorrect.", 401);
                }

                if (!user.IsActive)
                {
                    await _auditLogService.LogAsync(
                        EventType.Login.ToString(),
                        $"Login attempt for inactive account: {user.Email}",
                        user: user
                    );

                    throw new UnauthorizedActionException("Your account has been deactivated. Please contact your administrator.", 403);
                }

                var token = _jwtTokenService.GenerateToken(user);
                user.LastLoginAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    EventType.Login.ToString(),
                    $"User logged in successfully: {user.Email}",
                    user: user
                );

                _logger.LogInformation("User logged in successfully: {Email}", user.Email);

                return new LoginResponse
                {
                    Token = token,
                    User = new UserDto
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
                    }
                };
            }
            catch (UnauthorizedActionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuthService::LoginAsync failed for email: {Email}", request.Email);
                throw new UnauthorizedActionException($"Login error: {ex.Message} | Inner: {ex.InnerException?.Message ?? "none"} | Type: {ex.GetType().Name}", 500);
            }
        }

        public async Task LogoutAsync(User user)
        {
            try
            {
                await _auditLogService.LogAsync(
                    EventType.Logout.ToString(),
                    $"User logged out: {user.Email}",
                    user: user
                );

                _logger.LogInformation("User logged out successfully: {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuthService::LogoutAsync failed for user: {UserId}", user.Id);
                throw;
            }
        }

        public async Task<User> GetAuthenticatedUserAsync(long userId)
        {
            var user = await _dbContext.Users
                .Include(u => u.Company)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new UnauthorizedActionException("No authenticated user found.", 401);
            }

            return user;
        }

        public async Task<string> RefreshTokenAsync(User user)
        {
            try
            {
                var newToken = _jwtTokenService.GenerateToken(user);

                await _auditLogService.LogAsync(
                    EventType.Update.ToString(),
                    $"Token refreshed for user: {user.Email}",
                    user: user
                );

                _logger.LogInformation("Token refreshed for user: {Email}", user.Email);
                return newToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuthService::RefreshTokenAsync failed for user: {UserId}", user.Id);
                throw;
            }
        }
    }
}
