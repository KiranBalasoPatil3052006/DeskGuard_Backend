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

namespace DeskGuardBackend.Services
{
    public class AccountService : IAccountService
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<AccountService> _logger;

        public AccountService(DeskGuardDbContext dbContext, ILogger<AccountService> logger)
        {
            _dbContext = dbContext;
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

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Name = request.FullName,
                Email = request.Email,
                Password = passwordHash,
                EmployeeId = request.EmployeeId,
                IsActive = true,
                IsVerified = true,
                CreatedByUserId = creatorUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            var adminRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            if (adminRole != null)
            {
                _dbContext.UserRoles.Add(new UserRole
                {
                    RoleId = adminRole.Id,
                    UserId = user.Id,
                    ModelType = "App\\Models\\User"
                });
                await _dbContext.SaveChangesAsync();
            }

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
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Admin" || ur.Role.Name == "Super Admin"))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.ToLower();
                query = query.Where(u =>
                    (u.Name != null && u.Name.ToLower().Contains(search)) ||
                    (u.Email != null && u.Email.ToLower().Contains(search)) ||
                    (u.EmployeeId != null && u.EmployeeId.ToLower().Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                if (filter.Status == "active")
                    query = query.Where(u => u.IsActive);
                else if (filter.Status == "disabled")
                    query = query.Where(u => !u.IsActive);
            }

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)filter.PerPage);

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((filter.Page - 1) * filter.PerPage)
                .Take(filter.PerPage)
                .ToListAsync();

            var data = users.Select(u => new AccountDto
            {
                Id = u.Id,
                EmployeeId = u.EmployeeId,
                FullName = u.Name,
                Email = u.Email,
                Role = u.UserRoles?.FirstOrDefault()?.Role?.Name ?? "User",
                Status = u.IsActive ? "active" : "disabled",
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                LastLogin = u.LastLoginAt,
                CreatedBy = u.CreatedBy?.Name
            }).ToList();

            return new AccountListResponse
            {
                Data = data,
                Total = total,
                Page = filter.Page,
                PerPage = filter.PerPage,
                TotalPages = totalPages
            };
        }

        public async Task<AccountDto> GetByIdAsync(long id)
        {
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.CreatedBy)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                throw new AccountException("Account not found.", 404);

            return new AccountDto
            {
                Id = user.Id,
                EmployeeId = user.EmployeeId,
                FullName = user.Name,
                Email = user.Email,
                Role = user.UserRoles?.FirstOrDefault()?.Role?.Name ?? "User",
                Status = user.IsActive ? "active" : "disabled",
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                LastLogin = user.LastLoginAt,
                CreatedBy = user.CreatedBy?.Name
            };
        }

        public async Task<AccountDto> UpdateAsync(long id, UpdateAccountRequest request)
        {
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                throw new AccountException("Account not found.", 404);

            var errors = new Dictionary<string, string[]>();

            if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
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
            if (!string.IsNullOrWhiteSpace(request.EmployeeId))
                user.EmployeeId = request.EmployeeId;

            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Account updated: {Id}", id);

            return await GetByIdAsync(id);
        }

        public async Task DeleteAsync(long id)
        {
            var user = await _dbContext.Users.FindAsync(id);
            if (user == null)
                throw new AccountException("Account not found.", 404);

            user.DeletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Account soft-deleted: {Id}", id);
        }

        public async Task DisableAsync(long id)
        {
            var user = await _dbContext.Users.FindAsync(id);
            if (user == null)
                throw new AccountException("Account not found.", 404);

            if (!user.IsActive)
                throw new AccountException("Account is already disabled.", 400);

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Account disabled: {Id}", id);
        }

        public async Task EnableAsync(long id)
        {
            var user = await _dbContext.Users.FindAsync(id);
            if (user == null)
                throw new AccountException("Account not found.", 404);

            if (user.IsActive)
                throw new AccountException("Account is already active.", 400);

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Account enabled: {Id}", id);
        }

        public async Task<string> GenerateEmployeeIdAsync()
        {
            var last = await _dbContext.Users
                .Where(u => u.EmployeeId != null && u.EmployeeId.StartsWith("EMP-"))
                .OrderByDescending(u => u.EmployeeId)
                .Select(u => u.EmployeeId)
                .FirstOrDefaultAsync();

            int next = 1;
            if (last != null && int.TryParse(last[4..], out var num))
                next = num + 1;

            var empId = $"EMP-{next:D4}";

            while (await _dbContext.Users.AnyAsync(u => u.EmployeeId == empId))
            {
                next++;
                empId = $"EMP-{next:D4}";
            }

            return empId;
        }
    }
}
