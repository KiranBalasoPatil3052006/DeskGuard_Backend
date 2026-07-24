using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.DTOs.Profile;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Exceptions;
using DeskGuardBackend.Services.Interfaces;
using DeskGuardBackend.Enums;

namespace DeskGuardBackend.Services
{
    public class UserService : IUserService
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            DeskGuardDbContext dbContext,
            IAuditLogService auditLogService,
            ILogger<UserService> logger)
        {
            _dbContext = dbContext;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<User> GetUserAsync(long id)
        {
            var user = await _dbContext.Users
                .Include(u => u.Company)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                throw new KeyNotFoundException($"User not found with ID: {id}");
            }

            return user;
        }

        public async Task<IEnumerable<User>> GetCompanyUsersAsync(long companyId)
        {
            return await _dbContext.Users
                .Where(u => u.CompanyId == companyId)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .ToListAsync();
        }

        public async Task AssignRoleAsync(long userId, long roleId)
        {
            try
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null) throw new KeyNotFoundException($"User not found: {userId}");

                var role = await _dbContext.Roles.FindAsync(roleId);
                if (role == null) throw new KeyNotFoundException($"Role not found: {roleId}");

                // Remove existing roles
                var userRoles = await _dbContext.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
                _dbContext.UserRoles.RemoveRange(userRoles);

                // Add new role
                var newUserRole = new UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                    ModelType = "App\\Models\\User"
                };

                await _dbContext.UserRoles.AddAsync(newUserRole);
                await _dbContext.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    EventType.Update.ToString(),
                    $"Assigned role {role.Name} to user {user.Email}",
                    user: user
                );

                _logger.LogInformation("Role {RoleName} assigned to user ID {UserId}", role.Name, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserService::AssignRoleAsync failed for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<ProfileDto> GetProfileAsync(long userId)
        {
            var user = await _dbContext.Users
                .Include(u => u.Company)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new KeyNotFoundException($"User profile not found for user ID: {userId}");
            }

            return MapToProfileDto(user);
        }

        public async Task<ProfileDto> UpdateProfileAsync(long userId, UpdateProfileRequest request)
        {
            var user = await _dbContext.Users
                .Include(u => u.Company)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new KeyNotFoundException($"User profile not found for user ID: {userId}");
            }

            if (!string.IsNullOrWhiteSpace(request.Email) && !request.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailExists = await _dbContext.Users.AnyAsync(u => u.Id != userId && u.Email == request.Email);
                if (emailExists)
                {
                    throw new AccountException("This email address is already registered to another user account.", 409);
                }
                user.Email = request.Email;
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                user.Name = request.Name;
            }

            user.MobileNumber = request.MobileNumber;
            user.Phone = request.Phone;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _auditLogService.LogAsync(
                EventType.Update.ToString(),
                $"Updated profile details for user {user.Email}",
                user: user
            );

            _logger.LogInformation("Profile updated successfully for user ID {UserId}", userId);

            return MapToProfileDto(user);
        }

        public async Task ChangePasswordAsync(long userId, ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
                string.IsNullOrWhiteSpace(request.NewPassword) ||
                string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                throw new UnauthorizedActionException("Current password, new password, and confirmation password are required.", 422);
            }

            if (!request.NewPassword.Equals(request.ConfirmPassword))
            {
                throw new UnauthorizedActionException("New password and confirm password do not match.", 422);
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User not found for ID: {userId}");
            }

            if (string.IsNullOrEmpty(user.Password) || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
            {
                throw new UnauthorizedActionException("The current password provided is incorrect.", 400);
            }

            ValidatePasswordComplexity(request.NewPassword);

            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.MustChangePassword = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _auditLogService.LogAsync(
                EventType.Update.ToString(),
                $"Changed password for user {user.Email}",
                user: user
            );

            _logger.LogInformation("Password changed successfully for user ID {UserId}", userId);
        }

        private static void ValidatePasswordComplexity(string password)
        {
            if (password.Length < 6)
            {
                throw new UnauthorizedActionException("Password must be at least 6 characters long.", 422);
            }

            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

            if (!hasUpper || !hasLower || !hasDigit || !hasSpecial)
            {
                throw new UnauthorizedActionException(
                    "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.", 422);
            }
        }

        private static ProfileDto MapToProfileDto(User user)
        {
            return new ProfileDto
            {
                Id = user.Id,
                CompanyId = user.CompanyId,
                Name = user.Name,
                Email = user.Email,
                MobileNumber = user.MobileNumber,
                Phone = user.Phone,
                Avatar = user.Avatar,
                EmployeeId = user.EmployeeId,
                Role = user.UserRoles?.FirstOrDefault()?.Role?.Name ?? "User",
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
    }
}
