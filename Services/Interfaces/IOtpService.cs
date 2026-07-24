using System.Threading.Tasks;
using DeskGuardBackend.Entities;

namespace DeskGuardBackend.Services.Interfaces
{
    public interface IOtpService
    {
        /// <summary>
        /// Checks whether the given mobile number belongs to a registered customer.
        /// </summary>
        Task<bool> IsCustomerRegisteredAsync(string mobileNumber);

        /// <summary>
        /// Generates OTP response for development or triggers SMS delivery in production.
        /// </summary>
        Task<object> GenerateOtpAsync(string mobileNumber);

        /// <summary>
        /// Verifies the entered OTP code against the mobile number.
        /// </summary>
        Task<bool> VerifyOtpAsync(string mobileNumber, string otp);

        /// <summary>
        /// Finds the existing user or creates a customer user for the verified mobile number.
        /// </summary>
        Task<User> FindOrCreateUserAsync(string mobileNumber);
    }
}
