using System.Threading.Tasks;
using DeskGuardBackend.DTOs.Machine;
using DeskGuardBackend.Entities;

namespace DeskGuardBackend.Services.Interfaces
{
    public interface IAgentRegistrationService
    {
        Task<Machine> RegisterAsync(MachineRegistrationDto dto);
        Task<User> ValidateActivationTokenAsync(string token);
        string GenerateActivationToken();
        Task<Machine> ActivateAsync(long machineId);
    }
}
