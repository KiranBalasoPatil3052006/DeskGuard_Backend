using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Extensions;

namespace DeskGuardBackend.Services.PayloadProcessors
{
    public class DeviceProcessor : IPayloadProcessor
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<DeviceProcessor> _logger;

        public DeviceProcessor(DeskGuardDbContext dbContext, ILogger<DeviceProcessor> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ProcessAsync(Machine machine, JsonElement payload, HealthLog healthLog)
        {
            try
            {
                var peripheralsProp = payload.GetPropertyOrNull("peripherals");
                if (peripheralsProp == null || peripheralsProp.Value.ValueKind != JsonValueKind.Array) return;

                var existingDevices = await _dbContext.MachineConnectedDevices
                    .Where(d => d.MachineId == machine.Id)
                    .ToListAsync();

                foreach (var device in peripheralsProp.Value.EnumerateArray())
                {
                    var deviceName = device.GetStringProperty("deviceName") ?? device.GetStringProperty("device_name");
                    if (string.IsNullOrEmpty(deviceName)) continue;

                    var dbDevice = existingDevices.FirstOrDefault(d => d.DeviceName == deviceName);
                    if (dbDevice == null)
                    {
                        dbDevice = new MachineConnectedDevice { MachineId = machine.Id, DeviceName = deviceName };
                        await _dbContext.MachineConnectedDevices.AddAsync(dbDevice);
                    }

                    dbDevice.DeviceType = device.GetStringProperty("deviceType") ?? device.GetStringProperty("device_type");
                    dbDevice.DeviceId = device.GetStringProperty("deviceId") ?? device.GetStringProperty("device_id");
                    dbDevice.Manufacturer = device.GetStringProperty("manufacturer");
                    dbDevice.ConnectionType = device.GetStringProperty("connectionType") ?? device.GetStringProperty("connection_type");
                    dbDevice.Status = device.GetStringProperty("status") ?? "connected";
                    dbDevice.DriverVersion = device.GetStringProperty("driverVersion") ?? device.GetStringProperty("driver_version");
                    dbDevice.LastSeen = ParseDateTime(device.GetStringProperty("lastSeen") ?? device.GetStringProperty("last_seen")) ?? DateTime.UtcNow;
                    dbDevice.HasProblem = device.GetBooleanProperty("hasProblem") ?? device.GetBooleanProperty("has_problem");
                    dbDevice.ProblemDescription = device.GetStringProperty("problemDescription") ?? device.GetStringProperty("problem_description");
                    dbDevice.UpdatedAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogDebug("DeviceProcessor: Processed peripherals for machine {MachineId}", machine.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeviceProcessor: Failed to process peripherals for machine {MachineId}", machine.Id);
            }
        }

        private static DateTime? ParseDateTime(string? value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            if (DateTime.TryParse(value, out var dt)) return dt;
            return null;
        }
    }
}
