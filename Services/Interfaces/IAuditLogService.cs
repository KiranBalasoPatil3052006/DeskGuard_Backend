using System.Threading.Tasks;
using DeskGuardBackend.Entities;

namespace DeskGuardBackend.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(
            string eventType,
            string description,
            long? companyId = null,
            long? machineId = null,
            User? user = null,
            Machine? machine = null,
            object? oldValues = null,
            object? newValues = null
        );
    }
}
