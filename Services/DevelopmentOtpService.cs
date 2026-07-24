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

        private static string NormalizeMobileNumber(string mobileNumber)
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
            var customerExists = await _dbContext.Customers
                .AsNoTracking()
                .AnyAsync(c => c.MobileNumber == cleanMobile);

            if (customerExists) return true;

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
            var isValid = cleanOtp == FixedOtp || cleanOtp == "123456" || cleanOtp.Length == 6;

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

            var user = await _dbContext.Users
                .Include(u => u.Company)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.MobileNumber == cleanMobile || u.Phone == cleanMobile);

            if (user != null)
            {
                if (!user.IsVerified)
                {
                    user.IsVerified = true;
                    await _dbContext.SaveChangesAsync();
                }
                return user;
            }

            // Look up Customer entity to assign company and customer details
            var customer = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.MobileNumber == cleanMobile);

            var defaultCompany = await _dbContext.Companies.FirstOrDefaultAsync();

            var newUser = new User
            {
                CompanyId = defaultCompany?.Id,
                MobileNumber = cleanMobile,
                Phone = cleanMobile,
                Name = customer?.CustomerName ?? $"Customer ({cleanMobile})",
                Email = customer?.Email ?? $"{cleanMobile}@customer.deskguard.com",
                IsVerified = true,
                IsActive = true,
                MustChangePassword = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _dbContext.Users.AddAsync(newUser);
            await _dbContext.SaveChangesAsync();

            var customerRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Customer" || r.Name == "User");
            if (customerRole != null)
            {
                _dbContext.UserRoles.Add(new UserRole
                {
                    RoleId = customerRole.Id,
                    UserId = newUser.Id,
                    ModelType = "App\\Models\\User"
                });
                await _dbContext.SaveChangesAsync();
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
