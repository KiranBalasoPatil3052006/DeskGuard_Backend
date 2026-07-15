using System.Collections.Generic;
using System.Threading.Tasks;
using DeskGuardBackend.Entities;
using DeskGuardBackend.DTOs.Machine;

namespace DeskGuardBackend.Services.Interfaces
{
    public interface IMachineService
    {
        Task<Machine> GetMachineAsync(long id);
        Task<Machine> GetMachineByUidAsync(string uid);
        Task<PaginatedResponseDto<MachineResponseDto>> GetCompanyMachinesAsync(long companyId, int page, int perPage, string? status, string? search);
        Task<object> GetCompanyMachineSummaryAsync(long companyId);
        Task<Machine> AssignMachineAsync(long machineId, long userId);
        Task<Machine> UnassignMachineAsync(long machineId);
        Task UpdateHeartbeatAsync(string machineUid);
        Task<int> MarkOfflineMachinesAsync();
        Task<int> GetOnlineCountAsync(long companyId);
        Task<int> GetOfflineCountAsync(long companyId);
    }

    public class PaginatedResponseDto<T>
    {
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public int CurrentPage { get; set; }
        public int LastPage { get; set; }
        public int Total { get; set; }
        public int PerPage { get; set; }
    }
}
