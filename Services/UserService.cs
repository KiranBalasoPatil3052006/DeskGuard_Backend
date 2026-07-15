using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
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
    }
}
