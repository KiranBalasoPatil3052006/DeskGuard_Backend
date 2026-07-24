using System.Collections.Generic;
using System.Threading.Tasks;
using DeskGuardBackend.DTOs.Common;
using DeskGuardBackend.DTOs.Security;
using DeskGuardBackend.Entities;

namespace DeskGuardBackend.Services.Interfaces
{
    public interface ISecurityService
    {
        Task<SecuritySettingDto> GetSecuritySettingsAsync(long? companyId = null);
        Task<SecuritySettingDto> UpdateSecuritySettingsAsync(UpdateSecuritySettingRequest request, long performedByUserId, string? ipAddress = null, string? userAgent = null, long? companyId = null);
        Task ValidatePasswordAgainstPolicyAsync(string password, long? companyId = null);
        Task RecordLoginHistoryAsync(long? userId, string email, bool isSuccess, string? failureReason, string? ipAddress, string? userAgent, long? companyId = null);
        Task<PaginatedResult<UserLoginHistoryDto>> GetLoginHistoryAsync(int page = 1, int perPage = 20, string? search = null, string? status = null, long? companyId = null);
        Task<PaginatedResult<SecurityAuditLogDto>> GetSecurityAuditLogsAsync(int page = 1, int perPage = 20, string? search = null, long? companyId = null);
    }
}
