using System.Threading.Tasks;
using DeskGuardBackend.DTOs.Account;

namespace DeskGuardBackend.Services.Interfaces
{
    public interface IAccountService
    {
        Task<AccountDto> CreateAsync(CreateAccountRequest request, long creatorUserId);
        Task<AccountListResponse> GetAllAsync(AccountFilterRequest filter);
        Task<AccountDto> GetByIdAsync(long id);
        Task<AccountDto> UpdateAsync(long id, UpdateAccountRequest request, long currentUserId);
        Task ResetPasswordAsync(long id, ResetPasswordRequest request, long currentUserId);
        Task DeleteAsync(long id, long currentUserId);
        Task DisableAsync(long id, long currentUserId);
        Task EnableAsync(long id, long currentUserId);
        Task<string> GenerateEmployeeIdAsync();
    }
}
