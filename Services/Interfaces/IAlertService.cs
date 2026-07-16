using System.Collections.Generic;
using System.Threading.Tasks;
using DeskGuardBackend.Entities;

namespace DeskGuardBackend.Services.Interfaces
{
    public interface IAlertService
    {
        Task EvaluateMachineAlertsAsync(Machine machine, MachineCurrentStatus status);
        Task<Alert> AcknowledgeAlertAsync(long alertId, long userId);
        Task<Alert> ResolveAlertAsync(long alertId, long userId, string? resolution);
        Task<PaginatedResponseDto<Alert>> GetCompanyAlertsAsync(long companyId, string? severity, string? status, int page, int perPage);
        Task<IEnumerable<Alert>> GetMachineAlertsAsync(long machineId);
        Task<IEnumerable<Alert>> GetCriticalAlertsAsync(long companyId);
        Task<IEnumerable<AlertRule>> GetAlertRulesAsync(long companyId);
        Task<AlertRule> UpdateAlertRuleAsync(long ruleId, IDictionary<string, object> data);
        Task CreateMachineOfflineAlertAsync(Machine machine);
        Task CreateMachineUninstalledAlertAsync(Machine machine, string? reason);
    }
}
