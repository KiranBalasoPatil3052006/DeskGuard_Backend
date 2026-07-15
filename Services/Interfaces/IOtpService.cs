using System.Threading.Tasks;
using DeskGuardBackend.Entities;
using DeskGuardBackend.DTOs.Auth;

namespace DeskGuardBackend.Services.Interfaces
{
    public interface IOtpService
    {
        Task<object> GenerateOtpAsync(string mobileNumber);
        Task<OtpCode?> VerifyOtpAsync(string mobileNumber, string otp);
        Task<User> FindOrCreateUserAsync(string mobileNumber);
    }
}
