using System.Collections.Generic;
using System.Threading.Tasks;
using DeskGuardBackend.Entities;

namespace DeskGuardBackend.Services.Interfaces
{
    public interface ICompanyService
    {
        Task<Company> GetCompanyAsync(long id);
        Task<IEnumerable<Company>> GetCompaniesAsync();
        Task<Company> CreateCompanyAsync(string name, string? email, string? phone);
    }
}
