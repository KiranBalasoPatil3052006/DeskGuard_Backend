using System.Threading.Tasks;
using DeskGuardBackend.DTOs.Auth;
using DeskGuardBackend.Entities;

namespace DeskGuardBackend.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LoginResponse> RegisterAsync(RegisterRequest request);
        Task LogoutAsync(User user);
        Task<User> GetAuthenticatedUserAsync(long userId);
        Task<string> RefreshTokenAsync(User user);
    }
}
