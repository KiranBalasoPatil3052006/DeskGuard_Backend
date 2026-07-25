using System.Threading.Tasks;

namespace DeskGuardBackend.Services.Interfaces
{
    public interface ICustomerPortalService
    {
        Task<object> GetCustomerDashboardAsync(string mobileNumber, long? userId, long? companyId);
        Task<object> GetCustomerSystemsAsync(string mobileNumber, long? userId, long? companyId, string? search, string? status, string? sortBy, int page, int perPage);
        Task<object?> GetMachineOverviewAsync(long machineId, string mobileNumber, long? userId, long? companyId);
        Task<object> GetCustomerAlertsAsync(string mobileNumber, long? userId, long? companyId, string? search, string? severity, int page, int perPage);
        Task<object> GetCustomerProfileAsync(string mobileNumber, long? userId, long? companyId);
        Task<object> GetCustomerSupportInfoAsync();
    }
}
