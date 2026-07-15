using System.Threading.Tasks;

namespace DeskGuardBackend.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<object> GetCompanyDashboardAsync(long companyId);
        Task<object> GetEmployeeDashboardAsync(long userId);
        Task<object> GetCpuChartDataAsync(long companyId, int hours = 24);
        Task<object> GetRamChartDataAsync(long companyId, int hours = 24);
        Task<object> GetAlertChartDataAsync(long companyId, int days = 7);
    }
}
