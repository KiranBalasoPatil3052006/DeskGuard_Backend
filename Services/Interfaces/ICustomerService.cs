using System.Collections.Generic;
using System.Threading.Tasks;
using DeskGuardBackend.DTOs.Customer;

namespace DeskGuardBackend.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<(List<CustomerDto> Items, int TotalCount)> GetCustomersAsync(
            string? search,
            string? sortBy,
            int page,
            int pageSize);

        Task<CustomerDetailDto?> GetCustomerByIdAsync(long id);

        Task<List<CustomerMachineDto>> GetCustomerMachinesAsync(long customerId);
    }
}
