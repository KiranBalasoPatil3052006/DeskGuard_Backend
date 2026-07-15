using System.Collections.Generic;
using System.Threading.Tasks;
using DeskGuardBackend.Entities;

namespace DeskGuardBackend.Services.Interfaces
{
    public interface IUserService
    {
        Task<User> GetUserAsync(long id);
        Task<IEnumerable<User>> GetCompanyUsersAsync(long companyId);
        Task AssignRoleAsync(long userId, long roleId);
    }
}
