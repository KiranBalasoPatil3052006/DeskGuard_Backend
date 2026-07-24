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
        private readonly ISecurityService _securityService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            DeskGuardDbContext dbContext,
            IJwtTokenService jwtTokenService,
            IAuditLogService auditLogService,
            ISecurityService securityService,
            ILogger<AuthService> logger)
        {
            _dbContext = dbContext;
            _jwtTokenService = jwtTokenService;
            _auditLogService = auditLogService;
            _securityService = securityService;
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

                var securitySettings = await _securityService.GetSecuritySettingsAsync(user?.CompanyId);

                // Check active lockout
                if (user != null && user.LockoutEndAt.HasValue && user.LockoutEndAt.Value > DateTime.UtcNow)
                {
                    var remainingMins = Math.Max(1, (int)Math.Ceiling((user.LockoutEndAt.Value - DateTime.UtcNow).TotalMinutes));
                    await _securityService.RecordLoginHistoryAsync(user.Id, request.Email, false, "Account temporarily locked", null, null, user.CompanyId);
                    throw new UnauthorizedActionException($"Your account is temporarily locked due to multiple failed login attempts. Please try again in {remainingMins} minutes.", 403);
                }

                if (user == null || string.IsNullOrEmpty(user.Password) || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                {
                    if (user != null && securitySettings.MaxFailedLoginAttempts > 0)
                    {
                        user.FailedLoginAttempts++;
                        if (user.FailedLoginAttempts >= securitySettings.MaxFailedLoginAttempts)
                        {
                            user.LockoutEndAt = DateTime.UtcNow.AddMinutes(securitySettings.AccountLockoutDurationMinutes);
                            await _auditLogService.LogAsync(
                                EventType.Update.ToString(),
                                $"Account temporarily locked out for email {user.Email} after {user.FailedLoginAttempts} failed attempts.",
                                user: user
                            );
                        }
                        await _dbContext.SaveChangesAsync();
                    }

                    await _securityService.RecordLoginHistoryAsync(user?.Id, request.Email, false, "Invalid email or password", null, null, user?.CompanyId);

                    await _auditLogService.LogAsync(
                        EventType.Login.ToString(),
                        $"Failed login attempt for email: {request.Email}",
                        user: user
                    );

                    throw new UnauthorizedActionException("The provided credentials are incorrect.", 401);
                }

                if (!user.IsActive)
                {
                    await _securityService.RecordLoginHistoryAsync(user.Id, user.Email!, false, "Account deactivated", null, null, user.CompanyId);
                    await _auditLogService.LogAsync(
                        EventType.Login.ToString(),
                        $"Login attempt for inactive account: {user.Email}",
                        user: user
                    );

                    throw new UnauthorizedActionException("Your account has been deactivated. Please contact your administrator.", 403);
                }

                // Reset failed attempts on clean login
                user.FailedLoginAttempts = 0;
                user.LockoutEndAt = null;
                user.LastLoginAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                await _securityService.RecordLoginHistoryAsync(user.Id, user.Email!, true, null, null, null, user.CompanyId);

                var token = _jwtTokenService.GenerateToken(user);

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
                throw new UnauthorizedActionException("An unexpected error occurred during login. Please try again.", 500);
            }
        }

        public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                {
                    throw new UnauthorizedActionException("Name, email, and password are required.", 422);
                }

                await _securityService.ValidatePasswordAgainstPolicyAsync(request.Password);

                var existingUser = await _dbContext.Users.AnyAsync(u => u.Email == request.Email);
                if (existingUser)
                {
                    throw new UnauthorizedActionException("An account with this email already exists.", 409);
                }

                var company = await _dbContext.Companies.FirstOrDefaultAsync();
                if (company == null)
                {
                    company = new Company
                    {
                        Name = "Default Company",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _dbContext.Companies.AddAsync(company);
                    await _dbContext.SaveChangesAsync();
                }

                var user = new User
                {
                    CompanyId = company.Id,
                    Name = request.Name,
                    Email = request.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    IsActive = true,
                    IsVerified = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _dbContext.Users.AddAsync(user);
                await _dbContext.SaveChangesAsync();

                var userRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "User");
                if (userRole != null)
                {
                    _dbContext.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = userRole.Id,
                        ModelType = "App\\Models\\User"
                    });
                    await _dbContext.SaveChangesAsync();
                }

                var token = _jwtTokenService.GenerateToken(user);

                await _auditLogService.LogAsync(
                    EventType.Login.ToString(),
                    $"User registered successfully: {user.Email}",
                    user: user
                );

                _logger.LogInformation("User registered successfully: {Email}", user.Email);

                return new LoginResponse
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        CompanyId = user.CompanyId,
                        Name = user.Name,
                        Email = user.Email,
                        IsActive = user.IsActive,
                        Role = "User"
                    }
                };
            }
            catch (UnauthorizedActionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuthService::RegisterAsync failed for email: {Email}", request.Email);
                throw new UnauthorizedActionException("An unexpected error occurred during registration. Please try again.", 500);
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
