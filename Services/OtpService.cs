using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Services.Interfaces;

namespace DeskGuardBackend.Services
{
    public class OtpService : IOtpService
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<OtpService> _logger;
        private const int OtpExpiryMinutes = 10;

        public OtpService(DeskGuardDbContext dbContext, ILogger<OtpService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<object> GenerateOtpAsync(string mobileNumber)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Invalidate all active unused OTPs for this number
                var existingOtps = await _dbContext.OtpCodes
                    .Where(o => o.MobileNumber == mobileNumber && !o.IsUsed)
                    .ToListAsync();

                foreach (var oldOtp in existingOtps)
                {
                    oldOtp.IsUsed = true;
                    oldOtp.UsedAt = DateTime.UtcNow;
                }

                // Generate new 6 digit OTP using cryptographic RNG
                var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
                var expiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes);

                var otpCode = new OtpCode
                {
                    MobileNumber = mobileNumber,
                    Otp = otp,
                    ExpiresAt = expiresAt,
                    IsUsed = false
                };

                await _dbContext.OtpCodes.AddAsync(otpCode);
                await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("OTP generated for mobile: {MobileNumber}", mobileNumber);

                return new
                {
                    otp = otp,
                    expires_at = expiresAt.ToString("o")
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "OtpService::GenerateOtpAsync failed for mobile: {MobileNumber}", mobileNumber);
                throw;
            }
        }

        public async Task<OtpCode?> VerifyOtpAsync(string mobileNumber, string otp)
        {
            var otpRecord = await _dbContext.OtpCodes
                .Where(o => o.MobileNumber == mobileNumber && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow && o.Otp == otp)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null)
            {
                _logger.LogWarning("OTP verification failed for mobile: {MobileNumber}", mobileNumber);
                return null;
            }

            otpRecord.IsUsed = true;
            otpRecord.UsedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("OTP verified successfully for mobile: {MobileNumber}", mobileNumber);
            return otpRecord;
        }

        public async Task<User> FindOrCreateUserAsync(string mobileNumber)
        {
            var user = await _dbContext.Users
                .Include(u => u.Company)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.MobileNumber == mobileNumber);

            if (user == null)
            {
                // Create user automatically
                user = new User
                {
                    MobileNumber = mobileNumber,
                    IsVerified = true,
                    IsActive = true,
                    MustChangePassword = false
                };

                await _dbContext.Users.AddAsync(user);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("User auto-created after OTP verification. ID: {UserId}, Mobile: {Mobile}", user.Id, mobileNumber);
            }
            else if (!user.IsVerified)
            {
                user.IsVerified = true;
                await _dbContext.SaveChangesAsync();
            }

            return user;
        }
    }
}
