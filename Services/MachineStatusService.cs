using System;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Services.Interfaces;

namespace DeskGuardBackend.Services
{
    public class MachineStatusService : IMachineStatusService
    {
        public string CalculateStatus(Machine machine)
        {
            if (machine == null) return "Unknown";

            // 1. Inactive / Registration Pending / Disabled check
            if (!machine.IsActive)
            {
                if (!machine.ActivatedAt.HasValue)
                {
                    return "Registration Pending";
                }
                return "Disabled";
            }

            // 2. Online Check (Heartbeat within 10 minutes)
            if (machine.IsOnline && machine.LastHeartbeatAt.HasValue && machine.LastHeartbeatAt.Value >= DateTime.UtcNow.AddMinutes(-10))
            {
                return "Online";
            }

            // 3. Sleeping Check (Current status indicates OnlineStatus is false, but collected recently and battery present/idle)
            if (machine.CurrentStatus?.BatteryIsPresent == true && machine.CurrentStatus?.BatteryChargingStatus == false &&
                machine.LastHeartbeatAt.HasValue && machine.LastHeartbeatAt.Value >= DateTime.UtcNow.AddHours(-1) &&
                machine.LastHeartbeatAt.Value < DateTime.UtcNow.AddMinutes(-10))
            {
                return "Sleeping";
            }

            // 4. Offline Check
            if (machine.LastHeartbeatAt.HasValue && machine.LastHeartbeatAt.Value < DateTime.UtcNow.AddMinutes(-10))
            {
                return "Offline";
            }

            // Fallback
            return machine.IsOnline ? "Online" : "Offline";
        }
    }
}
