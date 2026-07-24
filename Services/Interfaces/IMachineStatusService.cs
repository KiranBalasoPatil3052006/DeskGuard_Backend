using DeskGuardBackend.Entities;

namespace DeskGuardBackend.Services.Interfaces
{
    public interface IMachineStatusService
    {
        /// <summary>
        /// Calculates the dynamic machine status string based on real-time telemetry,
        /// heartbeat timestamps, and system flags.
        /// Returns one of: Online, Offline, Sleeping, Maintenance, Disabled, Deleted, Uninstalled, Registration Pending, Unknown.
        /// </summary>
        string CalculateStatus(Machine machine);
    }
}
