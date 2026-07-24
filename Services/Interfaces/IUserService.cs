using System.Collections.Generic;
using System.Threading.Tasks;
using DeskGuardBackend.DTOs.Profile;
using DeskGuardBackend.Entities;

namespace DeskGuardBackend.Services.Interfaces
{
    public interface IUserService
    {
        Task<User> GetUserAsync(long id);
        Task<IEnumerable<User>> GetCompanyUsersAsync(long companyId);
        Task AssignRoleAsync(long userId, long roleId);
        Task<ProfileDto> GetProfileAsync(long userId);
        Task<ProfileDto> UpdateProfileAsync(long userId, UpdateProfileRequest request);
        Task ChangePasswordAsync(long userId, ChangePasswordRequest request);
    }
}
