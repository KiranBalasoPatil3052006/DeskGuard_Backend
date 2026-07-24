using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.DTOs.Account;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Exceptions;
using DeskGuardBackend.Services.Interfaces;
using DeskGuardBackend.Enums;

namespace DeskGuardBackend.Services
{
    public class AccountService : IAccountService
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly IAuditLogService _auditLogService;
        private readonly ISecurityService _securityService;
        private readonly ILogger<AccountService> _logger;

        public AccountService(
            DeskGuardDbContext dbContext,
            IAuditLogService auditLogService,
            ISecurityService securityService,
            ILogger<AccountService> logger)
        {
            _dbContext = dbContext;
            _auditLogService = auditLogService;
            _securityService = securityService;
            _logger = logger;
        }

        public async Task<AccountDto> CreateAsync(CreateAccountRequest request, long creatorUserId)
        {
            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(request.FullName))
                errors["full_name"] = new[] { "Full name is required." };

            if (string.IsNullOrWhiteSpace(request.Email))
                errors["email"] = new[] { "Email is required." };
            else if (!Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                errors["email"] = new[] { "Invalid email format." };

            if (string.IsNullOrWhiteSpace(request.Password))
                errors["password"] = new[] { "Password is required." };
            else if (request.Password.Length < 6)
                errors["password"] = new[] { "Password must be at least 6 characters." };

            if (request.Password != request.ConfirmPassword)
                errors["confirm_password"] = new[] { "Passwords do not match." };

            if (string.IsNullOrWhiteSpace(request.EmployeeId))
                errors["employee_id"] = new[] { "Employee ID is required." };

            if (errors.Count > 0)
                throw new AccountException("Validation failed.", 422, errors);

            var emailExists = await _dbContext.Users.AnyAsync(u => u.Email == request.Email);
            if (emailExists)
                throw new AccountException("Email already exists.", 422, new Dictionary<string, string[]>
                {
                    ["email"] = new[] { "An account with this email already exists." }
                });

            var empIdExists = await _dbContext.Users.AnyAsync(u => u.EmployeeId == request.EmployeeId);
            if (empIdExists)
                throw new AccountException("Employee ID already exists.", 422, new Dictionary<string, string[]>
                {
                    ["employee_id"] = new[] { "This Employee ID is already in use." }
                });

            var creatorUser = await _dbContext.Users.FindAsync(creatorUserId);
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var isActive = string.Equals(request.Status, "active", StringComparison.OrdinalIgnoreCase) || request.Status == null;

            var user = new User
            {
                Name = request.FullName,
                Email = request.Email,
                MobileNumber = request.MobileNumber,
                Password = passwordHash,
                EmployeeId = request.EmployeeId,
                IsActive = isActive,
                IsVerified = true,
                CreatedByUserId = creatorUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Assign role
            var roleName = NormalizeRoleName(request.Role);
            var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null)
            {
                role = new Role
                {
                    Name = roleName,
                    GuardName = "web",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _dbContext.Roles.AddAsync(role);
                await _dbContext.SaveChangesAsync();
            }

            _dbContext.UserRoles.Add(new UserRole
            {
                RoleId = role.Id,
                UserId = user.Id,
                ModelType = "App\\Models\\User"
            });
            await _dbContext.SaveChangesAsync();

            await _auditLogService.LogAsync(
                EventType.Update.ToString(),
                $"Created account {user.Email} (Employee ID: {user.EmployeeId}, Role: {roleName})",
                user: creatorUser
            );

            _logger.LogInformation("Account created: {Email} with EmployeeId {EmpId} by creator {CreatorId}",
                request.Email, request.EmployeeId, creatorUserId);

            return await GetByIdAsync(user.Id);
        }

        public async Task<AccountListResponse> GetAllAsync(AccountFilterRequest filter)
        {
            var query = _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.CreatedBy)
                .Where(u => u.DeletedAt == null)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim().ToLower();
                query = query.Where(u =>
                    (u.Name != null && u.Name.ToLower().Contains(search)) ||
                    (u.Email != null && u.Email.ToLower().Contains(search)) ||
                    (u.MobileNumber != null && u.MobileNumber.ToLower().Contains(search)) ||
                    (u.EmployeeId != null && u.EmployeeId.ToLower().Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(filter.Role) && !filter.Role.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                var roleFilter = NormalizeRoleName(filter.Role);
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == roleFilter));
            }

            if (!string.IsNullOrWhiteSpace(filter.Status) && !filter.Status.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                if (filter.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(u => u.IsActive);
                else if (filter.Status.Equals("disabled", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(u => !u.IsActive);
            }

            // Sorting
            var sortOrder = (filter.SortOrder ?? "desc").ToLower();
            var sortBy = (filter.SortBy ?? "created_at").ToLower();

            query = (sortBy, sortOrder) switch
            {
                ("employee_id", "asc") => query.OrderBy(u => u.EmployeeId),
                ("employee_id", "desc") => query.OrderByDescending(u => u.EmployeeId),
                ("full_name", "asc") => query.OrderBy(u => u.Name),
                ("full_name", "desc") => query.OrderByDescending(u => u.Name),
                ("email", "asc") => query.OrderBy(u => u.Email),
                ("email", "desc") => query.OrderByDescending(u => u.Email),
                ("last_login", "asc") => query.OrderBy(u => u.LastLoginAt),
                ("last_login", "desc") => query.OrderByDescending(u => u.LastLoginAt),
                ("status", "asc") => query.OrderBy(u => u.IsActive),
                ("status", "desc") => query.OrderByDescending(u => u.IsActive),
                ("created_at", "asc") => query.OrderBy(u => u.CreatedAt),
                _ => query.OrderByDescending(u => u.CreatedAt),
            };

            var page = Math.Max(1, filter.Page);
            var perPage = filter.PerPage switch
            {
                10 => 10,
                50 => 50,
                100 => 100,
                _ => 20
            };

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)perPage);

            var users = await query
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .ToListAsync();

            var data = users.Select(u => MapToAccountDto(u)).ToList();

            return new AccountListResponse
            {
                Data = data,
                Total = total,
                Page = page,
                PerPage = perPage,
                TotalPages = totalPages
            };
        }

        public async Task<AccountDto> GetByIdAsync(long id)
        {
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.CreatedBy)
                .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (user == null)
                throw new AccountException("Account not found.", 404);

            return MapToAccountDto(user);
        }

        public async Task<AccountDto> UpdateAsync(long id, UpdateAccountRequest request, long currentUserId)
        {
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (user == null)
                throw new AccountException("Account not found.", 404);

            var errors = new Dictionary<string, string[]>();

            if (!string.IsNullOrWhiteSpace(request.Email) && !request.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
            {
                if (!Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    errors["email"] = new[] { "Invalid email format." };
                else
                {
                    var emailExists = await _dbContext.Users.AnyAsync(u => u.Email == request.Email && u.Id != id);
                    if (emailExists)
                        errors["email"] = new[] { "Email already in use by another account." };
                }
            }

            if (!string.IsNullOrWhiteSpace(request.EmployeeId) && request.EmployeeId != user.EmployeeId)
            {
                var empIdExists = await _dbContext.Users.AnyAsync(u => u.EmployeeId == request.EmployeeId && u.Id != id);
                if (empIdExists)
                    errors["employee_id"] = new[] { "Employee ID already in use." };
            }

            if (errors.Count > 0)
                throw new AccountException("Validation failed.", 422, errors);

            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.Name = request.FullName;

            if (!string.IsNullOrWhiteSpace(request.Email))
                user.Email = request.Email;

            if (request.MobileNumber != null)
                user.MobileNumber = request.MobileNumber;

            if (!string.IsNullOrWhiteSpace(request.EmployeeId))
                user.EmployeeId = request.EmployeeId;

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                user.IsActive = string.Equals(request.Status, "active", StringComparison.OrdinalIgnoreCase);
            }

            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var roleName = NormalizeRoleName(request.Role);
                var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                if (role == null)
                {
                    role = new Role
                    {
                        Name = roleName,
                        GuardName = "web",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _dbContext.Roles.AddAsync(role);
                    await _dbContext.SaveChangesAsync();
                }

                var existingUserRoles = await _dbContext.UserRoles.Where(ur => ur.UserId == id).ToListAsync();
                _dbContext.UserRoles.RemoveRange(existingUserRoles);
                await _dbContext.SaveChangesAsync();

                _dbContext.UserRoles.Add(new UserRole
                {
                    RoleId = role.Id,
                    UserId = id,
                    ModelType = "App\\Models\\User"
                });
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            var modifier = await _dbContext.Users.FindAsync(currentUserId);
            await _auditLogService.LogAsync(
                EventType.Update.ToString(),
                $"Updated account details for {user.Email} (Employee ID: {user.EmployeeId})",
                user: modifier
            );

            _logger.LogInformation("Account updated: {Id}", id);

            return await GetByIdAsync(id);
        }

        public async Task ResetPasswordAsync(long id, ResetPasswordRequest request, long currentUserId)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
            if (user == null)
                throw new AccountException("Account not found.", 404);

            if (request.NewPassword != request.ConfirmPassword)
                throw new AccountException("Passwords do not match.", 422);

            try
            {
                await _securityService.ValidatePasswordAgainstPolicyAsync(request.NewPassword, user.CompanyId);
            }
            catch (Exception ex)
            {
                throw new AccountException(ex.Message, 422);
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.MustChangePassword = request.MustChangePassword;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            var modifier = await _dbContext.Users.FindAsync(currentUserId);
            await _auditLogService.LogAsync(
                EventType.Update.ToString(),
                $"Reset password for account {user.Email}",
                user: modifier
            );

            _logger.LogInformation("Password reset for account ID {Id}", id);
        }

        public async Task DeleteAsync(long id, long currentUserId)
        {
            if (id == currentUserId)
            {
                throw new AccountException("You cannot delete your own account.", 400);
            }

            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (user == null)
                throw new AccountException("Account not found.", 404);

            var isSuperAdmin = user.UserRoles.Any(ur => ur.Role.Name == "Super Admin");
            if (isSuperAdmin)
            {
                var superAdminCount = await _dbContext.UserRoles
                    .CountAsync(ur => ur.Role.Name == "Super Admin" && ur.User.DeletedAt == null);
                if (superAdminCount <= 1)
                {
                    throw new AccountException("Cannot delete the final Super Admin account.", 400);
                }
            }

            user.DeletedAt = DateTime.UtcNow;
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            var modifier = await _dbContext.Users.FindAsync(currentUserId);
            await _auditLogService.LogAsync(
                EventType.Update.ToString(),
                $"Soft-deleted account {user.Email} (Employee ID: {user.EmployeeId})",
                user: modifier
            );

            _logger.LogInformation("Account soft-deleted: {Id}", id);
        }

        public async Task DisableAsync(long id, long currentUserId)
        {
            if (id == currentUserId)
            {
                throw new AccountException("You cannot disable your own active account session.", 400);
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
            if (user == null)
                throw new AccountException("Account not found.", 404);

            if (!user.IsActive)
                throw new AccountException("Account is already disabled.", 400);

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            var modifier = await _dbContext.Users.FindAsync(currentUserId);
            await _auditLogService.LogAsync(
                EventType.Update.ToString(),
                $"Disabled account {user.Email}",
                user: modifier
            );

            _logger.LogInformation("Account disabled: {Id}", id);
        }

        public async Task EnableAsync(long id, long currentUserId)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
            if (user == null)
                throw new AccountException("Account not found.", 404);

            if (user.IsActive)
                throw new AccountException("Account is already active.", 400);

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            var modifier = await _dbContext.Users.FindAsync(currentUserId);
            await _auditLogService.LogAsync(
                EventType.Update.ToString(),
                $"Enabled account {user.Email}",
                user: modifier
            );

            _logger.LogInformation("Account enabled: {Id}", id);
        }

        public async Task<string> GenerateEmployeeIdAsync()
        {
            var empIds = await _dbContext.Users
                .Where(u => u.EmployeeId != null && u.EmployeeId.StartsWith("EMP-"))
                .Select(u => u.EmployeeId)
                .ToListAsync();

            long maxNum = 0;
            foreach (var empId in empIds)
            {
                if (empId != null && empId.Length > 4 && long.TryParse(empId[4..], out var num))
                {
                    if (num > maxNum) maxNum = num;
                }
            }

            long next = maxNum + 1;
            var candidate = $"EMP-{next:D6}";

            while (await _dbContext.Users.AnyAsync(u => u.EmployeeId == candidate))
            {
                next++;
                candidate = $"EMP-{next:D6}";
            }

            return candidate;
        }

        private static string NormalizeRoleName(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return "Admin";
            if (role.Equals("Super Admin", StringComparison.OrdinalIgnoreCase)) return "Super Admin";
            if (role.Equals("Manager", StringComparison.OrdinalIgnoreCase)) return "Manager";
            if (role.Equals("Technician", StringComparison.OrdinalIgnoreCase)) return "Technician";
            return "Admin";
        }

        private static AccountDto MapToAccountDto(User u)
        {
            return new AccountDto
            {
                Id = u.Id,
                EmployeeId = u.EmployeeId,
                FullName = u.Name,
                Email = u.Email,
                MobileNumber = u.MobileNumber,
                Role = u.UserRoles?.FirstOrDefault()?.Role?.Name ?? "User",
                Status = u.IsActive ? "active" : "disabled",
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                LastLogin = u.LastLoginAt,
                CreatedBy = u.CreatedBy?.Name
            };
        }
    }
}
