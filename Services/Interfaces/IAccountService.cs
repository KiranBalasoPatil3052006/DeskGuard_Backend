using System.Threading.Tasks;
using DeskGuardBackend.DTOs.Account;

namespace DeskGuardBackend.Services.Interfaces
{
    public interface IAccountService
    {
        Task<AccountDto> CreateAsync(CreateAccountRequest request, long creatorUserId);
        Task<AccountListResponse> GetAllAsync(AccountFilterRequest filter);
        Task<AccountDto> GetByIdAsync(long id);
        Task<AccountDto> UpdateAsync(long id, UpdateAccountRequest request);
        Task DeleteAsync(long id);
        Task DisableAsync(long id);
        Task EnableAsync(long id);
        Task<string> GenerateEmployeeIdAsync();
    }
}
