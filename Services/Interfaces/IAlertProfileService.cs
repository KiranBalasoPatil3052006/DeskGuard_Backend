using System.Collections.Generic;
using System.Threading.Tasks;
using DeskGuardBackend.DTOs.AlertThreshold;

namespace DeskGuardBackend.Services.Interfaces
{
    public interface IAlertProfileService
    {
        Task<AlertProfileListResponse> GetAllAsync(AlertProfileFilterRequest filter);
        Task<AlertProfileDto> GetByIdAsync(long id);
        Task<AlertProfileDto> CreateAsync(CreateAlertProfileRequest request);
        Task<AlertProfileDto> UpdateAsync(long id, UpdateAlertProfileRequest request);
        Task DeleteAsync(long id);
        Task<AlertProfileDto> DuplicateAsync(long id);
        Task AssignToCompanyAsync(long profileId, long companyId);
        Task UnassignFromCompanyAsync(long profileId, long companyId);
        Task AssignToMachineAsync(long profileId, long machineId);
        Task UnassignFromMachineAsync(long profileId, long machineId);

        // Profile resolution for alert engine
        Task<AlertThresholdDto?> ResolveThresholdsForMachineAsync(long machineId);
    }
}
