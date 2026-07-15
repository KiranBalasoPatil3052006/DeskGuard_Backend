using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Exceptions;
using DeskGuardBackend.Extensions;
using DeskGuardBackend.Services.Interfaces;

namespace DeskGuardBackend.Services
{
    public class TelemetryService : ITelemetryService
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly IPayloadProcessorService _payloadProcessorService;
        private readonly ILogger<TelemetryService> _logger;

        public TelemetryService(
            DeskGuardDbContext dbContext,
            IPayloadProcessorService payloadProcessorService,
            ILogger<TelemetryService> logger)
        {
            _dbContext = dbContext;
            _payloadProcessorService = payloadProcessorService;
            _logger = logger;
        }

        public async Task ProcessTelemetryAsync(JsonElement payload, string sourceIp)
        {
            try
            {
                var machineUid = payload.GetStringProperty("machineId") ?? payload.GetStringProperty("machine_uid") ?? string.Empty;
                if (string.IsNullOrEmpty(machineUid))
                {
                    _logger.LogWarning("TelemetryService: Received payload without machineId");
                    return;
                }

                var machine = await _dbContext.Machines
                    .FirstOrDefaultAsync(m => m.MachineUid == machineUid);

                if (machine == null)
                {
                    _logger.LogWarning("TelemetryService: Machine not found with UID {MachineUid}", machineUid);
                    return;
                }

                var rawPayloadLog = new RawPayloadLog
                {
                    MachineId = machine.Id,
                    MachineUid = machineUid,
                    Payload = JsonSerializer.Serialize(payload),
                    SourceIp = sourceIp,
                    ReceivedAt = DateTime.UtcNow
                };

                await _dbContext.RawPayloadLogs.AddAsync(rawPayloadLog);
                await _dbContext.SaveChangesAsync();

                // Normalise the payload format to the structure expected by section processors
                var normalizedPayload = NormalisePayload(payload);

                using var doc = JsonDocument.Parse(JsonSerializer.Serialize(normalizedPayload));
                await _payloadProcessorService.ProcessAsync(machine, doc.RootElement);

                _logger.LogInformation("TelemetryService: Telemetry processed successfully for machine {MachineUid}", machineUid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TelemetryService::ProcessTelemetryAsync failed");
                throw;
            }
        }

        private static Dictionary<string, object?> NormalisePayload(JsonElement payload)
        {
            var systemInfoProp = payload.GetPropertyOrNull("systemInfo");
            var cpuProp = payload.GetPropertyOrNull("cpuInfo");
            var memoryProp = payload.GetPropertyOrNull("memoryInfo");
            var diskProp = payload.GetPropertyOrNull("diskInfo");
            var batteryProp = payload.GetPropertyOrNull("batteryInfo");
            var networkProp = payload.GetPropertyOrNull("networkInfo");
            var antivirusProp = payload.GetPropertyOrNull("antivirusInfo");
            var firewallProp = payload.GetPropertyOrNull("firewallInfo");
            var processProp = payload.GetPropertyOrNull("processInfo");
            var serviceProp = payload.GetPropertyOrNull("serviceInfo");
            var startupProp = payload.GetPropertyOrNull("startupProgramInfo");
            var updateProp = payload.GetPropertyOrNull("updateInfo");
            var eventLogProp = payload.GetPropertyOrNull("eventLogInfo");
            var loginProp = payload.GetPropertyOrNull("loginActivityInfo");
            var usbProp = payload.GetPropertyOrNull("usbActivityInfo");
            var peripheralProp = payload.GetPropertyOrNull("peripheralInfo");

            return new Dictionary<string, object?>
            {
                ["machineId"] = payload.GetStringProperty("machineId"),
                ["collectedAt"] = payload.GetStringProperty("timestamp"),
                ["systemInfo"] = systemInfoProp?.ValueKind == JsonValueKind.Object ? (object)systemInfoProp.Value : null,
                ["cpu"] = cpuProp?.ValueKind == JsonValueKind.Object ? (object)cpuProp.Value : null,
                ["memory"] = memoryProp?.ValueKind == JsonValueKind.Object ? (object)memoryProp.Value : null,
                ["disks"] = diskProp?.ValueKind == JsonValueKind.Array ? (object)diskProp.Value : null,
                ["battery"] = batteryProp?.ValueKind == JsonValueKind.Object ? (object)batteryProp.Value : null,
                ["networkAdapters"] = networkProp?.ValueKind == JsonValueKind.Array ? (object)networkProp.Value : null,
                ["antivirus"] = antivirusProp?.ValueKind == JsonValueKind.Object ? (object)antivirusProp.Value : null,
                ["firewall"] = firewallProp?.ValueKind == JsonValueKind.Object ? (object)firewallProp.Value : null,
                ["processes"] = processProp?.ValueKind == JsonValueKind.Array ? (object)processProp.Value : null,
                ["services"] = serviceProp?.ValueKind == JsonValueKind.Array ? (object)serviceProp.Value : null,
                ["startupPrograms"] = startupProp?.ValueKind == JsonValueKind.Array ? (object)startupProp.Value : null,
                ["updates"] = updateProp?.ValueKind == JsonValueKind.Object ? (object)updateProp.Value : null,
                ["eventLogs"] = eventLogProp?.ValueKind == JsonValueKind.Array ? (object)eventLogProp.Value : null,
                ["loginActivity"] = loginProp?.ValueKind == JsonValueKind.Array ? (object)loginProp.Value : null,
                ["usbActivity"] = usbProp?.ValueKind == JsonValueKind.Array ? (object)usbProp.Value : null,
                ["peripherals"] = peripheralProp?.ValueKind == JsonValueKind.Array ? (object)peripheralProp.Value : null
            };
        }
    }
}
