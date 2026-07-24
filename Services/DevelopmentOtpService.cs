using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Exceptions;
using DeskGuardBackend.Services.Interfaces;

namespace DeskGuardBackend.Services
{
    /// <summary>
    /// Development OTP Service implementation.
    /// Uses a fixed OTP (111111) for development without invoking external SMS gateways,
    /// generating random OTPs, or storing OTP history.
    /// Architecture is ready to be swapped with ProductionSmsOtpService when deploying to production.
    /// </summary>
    public class DevelopmentOtpService : IOtpService
    {
        public const string FixedOtp = "111111";
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<DevelopmentOtpService> _logger;

        public DevelopmentOtpService(DeskGuardDbContext dbContext, ILogger<DevelopmentOtpService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        private static string NormalizeMobileNumber(string? mobileNumber)
        {
            if (string.IsNullOrWhiteSpace(mobileNumber)) return string.Empty;
            return mobileNumber.Trim().Replace(" ", "").Replace("-", "").Replace("+91", "");
        }

        public static bool IsValidMobileFormat(string mobileNumber)
        {
            var normalized = NormalizeMobileNumber(mobileNumber);
            return !string.IsNullOrEmpty(normalized) && Regex.IsMatch(normalized, @"^\d{10}$");
        }

        public async Task<bool> IsCustomerRegisteredAsync(string mobileNumber)
        {
            var cleanMobile = NormalizeMobileNumber(mobileNumber);
            if (!IsValidMobileFormat(cleanMobile)) return false;

            // Check Users table
            var userExists = await _dbContext.Users
                .AsNoTracking()
                .AnyAsync(u => u.MobileNumber == cleanMobile || u.Phone == cleanMobile);

            if (userExists) return true;

            // Check Customers table
            try
            {
                var customerExists = await _dbContext.Customers
                    .AsNoTracking()
                    .AnyAsync(c => c.MobileNumber == cleanMobile);

                if (customerExists) return true;
            }
            catch
            {
                // Ignore if customers table is not created yet
            }

            // Check Machines table for assigned employee mobile
            var machineExists = await _dbContext.Machines
                .AsNoTracking()
                .AnyAsync(m => m.EmployeeMobileNumber == cleanMobile);

            if (machineExists) return true;

            // [DEV MODE]: Allow any valid 10-digit mobile number during development testing
            return true;
        }

        public async Task<object> GenerateOtpAsync(string mobileNumber)
        {
            var cleanMobile = NormalizeMobileNumber(mobileNumber);

            if (!IsValidMobileFormat(cleanMobile))
            {
                throw new UnauthorizedActionException("Please enter a valid 10-digit mobile number.", 400);
            }

            var isRegistered = await IsCustomerRegisteredAsync(cleanMobile);
            if (!isRegistered)
            {
                throw new UnauthorizedActionException("No customer account is registered with this mobile number. Please contact your AMC administrator.", 404);
            }

            _logger.LogInformation("[DEV MODE] Fixed OTP {FixedOtp} requested for customer mobile: {Mobile}", FixedOtp, cleanMobile);

            // Development mode: No SMS sent, no DB entry stored, fixed OTP 111111 returned
            return new
            {
                success = true,
                dev_mode = true,
                dev_otp = FixedOtp,
                mobile_number = cleanMobile,
                message = "OTP sent successfully."
            };
        }

        public Task<bool> VerifyOtpAsync(string mobileNumber, string otp)
        {
            var cleanMobile = NormalizeMobileNumber(mobileNumber);
            if (!IsValidMobileFormat(cleanMobile))
            {
                return Task.FromResult(false);
            }

            if (string.IsNullOrWhiteSpace(otp))
            {
                return Task.FromResult(false);
            }

            var cleanOtp = otp.Trim();
            // Development Mode: Accept 111111, 123456, or any 6-digit numeric OTP
            var isValid = cleanOtp == FixedOtp || cleanOtp == "123456" || (cleanOtp.Length == 6 && cleanOtp.All(char.IsDigit));

            if (isValid)
            {
                _logger.LogInformation("[DEV MODE] Fixed OTP verified successfully for mobile: {Mobile}", cleanMobile);
            }
            else
            {
                _logger.LogWarning("[DEV MODE] Invalid OTP entered for mobile: {Mobile}. Expected: {FixedOtp}, Got: {Otp}", cleanMobile, FixedOtp, cleanOtp);
            }

            return Task.FromResult(isValid);
        }

        public async Task<User> FindOrCreateUserAsync(string mobileNumber)
        {
            var cleanMobile = NormalizeMobileNumber(mobileNumber);

            // 1. Search existing users with normalized mobile or phone or email
            var users = await _dbContext.Users
                .Include(u => u.Company)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .ToListAsync();

            var user = users.FirstOrDefault(u =>
                NormalizeMobileNumber(u.MobileNumber) == cleanMobile ||
                NormalizeMobileNumber(u.Phone) == cleanMobile ||
                (u.Email != null && u.Email.StartsWith(cleanMobile)));

            if (user != null)
            {
                if (!user.IsVerified || !user.IsActive)
                {
                    user.IsVerified = true;
                    user.IsActive = true;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }
                return user;
            }

            // 2. Ensure Default Company exists
            var defaultCompany = await _dbContext.Companies.FirstOrDefaultAsync();
            if (defaultCompany == null)
            {
                defaultCompany = new Company
                {
                    Name = "DeskGuard Default Company",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _dbContext.Companies.AddAsync(defaultCompany);
                await _dbContext.SaveChangesAsync();
            }

            // 3. Look up Customer record if available
            Customer? customer = null;
            try
            {
                var customers = await _dbContext.Customers.AsNoTracking().ToListAsync();
                customer = customers.FirstOrDefault(c => NormalizeMobileNumber(c.MobileNumber) == cleanMobile);
            }
            catch (Exception custEx)
            {
                _logger.LogWarning(custEx, "Notice querying customers table");
            }

            var email = customer?.Email ?? $"{cleanMobile}@customer.deskguard.com";

            // Check if user with this email already exists
            var existingByEmail = users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            if (existingByEmail != null)
            {
                existingByEmail.MobileNumber = cleanMobile;
                existingByEmail.Phone = cleanMobile;
                existingByEmail.IsVerified = true;
                existingByEmail.IsActive = true;
                existingByEmail.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                return existingByEmail;
            }

            // 4. Create new User safely
            var newUser = new User
            {
                CompanyId = defaultCompany.Id,
                MobileNumber = cleanMobile,
                Phone = cleanMobile,
                Name = customer?.CustomerName ?? (customer?.CompanyName != null ? $"{customer.CustomerName} ({customer.CompanyName})" : $"Customer ({cleanMobile})"),
                Email = email,
                IsVerified = true,
                IsActive = true,
                MustChangePassword = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _dbContext.Users.AddAsync(newUser);
            await _dbContext.SaveChangesAsync();

            // 5. Assign Role safely
            var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Customer")
                    ?? await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "User")
                    ?? await _dbContext.Roles.FirstOrDefaultAsync();

            if (role != null)
            {
                var userRoleExists = await _dbContext.UserRoles.AnyAsync(ur => ur.UserId == newUser.Id && ur.RoleId == role.Id);
                if (!userRoleExists)
                {
                    _dbContext.UserRoles.Add(new UserRole
                    {
                        RoleId = role.Id,
                        UserId = newUser.Id,
                        ModelType = "App\\Models\\User"
                    });
                    await _dbContext.SaveChangesAsync();
                }
            }

            _logger.LogInformation("Customer user created post-OTP verification. ID: {UserId}, Mobile: {Mobile}", newUser.Id, cleanMobile);

            return await _dbContext.Users
                .Include(u => u.Company)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstAsync(u => u.Id == newUser.Id);
        }
    }
}
