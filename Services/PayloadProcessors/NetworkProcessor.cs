using System;
using System.Collections.Generic;
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
    public class NetworkProcessor : IPayloadProcessor
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ILogger<NetworkProcessor> _logger;

        public NetworkProcessor(DeskGuardDbContext dbContext, ILogger<NetworkProcessor> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ProcessAsync(Machine machine, JsonElement payload, HealthLog healthLog)
        {
            var adaptersProp = payload.GetPropertyOrNull("networkAdapters") ?? payload.GetPropertyOrNull("network") ?? payload.GetPropertyOrNull("network_adapters");
            if (adaptersProp == null || adaptersProp.Value.ValueKind != JsonValueKind.Array) return;

            var adapters = adaptersProp.Value;

            long totalSent = 0;
            long totalRecv = 0;
            bool hasValidByteData = false;

            // Pre-load existing adapters for this machine to avoid N+1 read queries.
            var existingAdapters = await _dbContext.MachineNetworkAdapters
                .Where(n => n.MachineId == machine.Id)
                .ToListAsync();

            foreach (var adapter in adapters.EnumerateArray())
            {
                var adapterName = adapter.GetStringProperty("adapterName") ?? adapter.GetStringProperty("adapter_name") ?? "Unknown";
                var isConnected = adapter.GetBooleanProperty("isConnected") ?? adapter.GetBooleanProperty("is_connected") ?? false;
                var ipV4 = adapter.GetStringProperty("ipAddressV4") ?? adapter.GetStringProperty("ip_address_v4") ?? adapter.GetStringProperty("ipAddress");
                var mac = adapter.GetStringProperty("macAddress") ?? adapter.GetStringProperty("mac_address");
                var speed = adapter.GetInt64Property("connectionSpeedMbps") ?? adapter.GetInt64Property("connection_speed_mbps");
                var bytesSent = adapter.GetInt64Property("bytesSent") ?? adapter.GetInt64Property("bytes_sent");
                var bytesRecv = adapter.GetInt64Property("bytesReceived") ?? adapter.GetInt64Property("bytes_received");
                var adapterType = adapter.GetStringProperty("adapterType") ?? adapter.GetStringProperty("adapter_type");

                if (bytesSent.HasValue) { totalSent += bytesSent.Value; hasValidByteData = true; }
                if (bytesRecv.HasValue) { totalRecv += bytesRecv.Value; hasValidByteData = true; }

                // In-memory lookup to avoid N+1 queries.
                var dbAdapter = existingAdapters.FirstOrDefault(n => n.AdapterName == adapterName);

                if (dbAdapter == null)
                {
                    dbAdapter = new MachineNetworkAdapter { MachineId = machine.Id, AdapterName = adapterName };
                    await _dbContext.MachineNetworkAdapters.AddAsync(dbAdapter);
                }

                dbAdapter.IpAddress = ipV4;
                dbAdapter.MacAddress = mac;
                dbAdapter.AdapterType = adapterType;
                dbAdapter.Speed = speed;
                dbAdapter.Status = isConnected ? "connected" : "disconnected";
                dbAdapter.UpdatedAt = DateTime.UtcNow;
            }

            // Update MachineCurrentStatus
            var currentStatus = await _dbContext.MachineCurrentStatuses
                .FirstOrDefaultAsync(s => s.MachineId == machine.Id);

            if (currentStatus == null)
            {
                currentStatus = new MachineCurrentStatus { MachineId = machine.Id };
                await _dbContext.MachineCurrentStatuses.AddAsync(currentStatus);
            }

            currentStatus.CollectedAt = DateTime.UtcNow;

            // Only write byte totals when the agent actually provided valid byte data.
            // Otherwise leave the DB value as-is (null) to distinguish "no data" from "0 bytes".
            if (hasValidByteData)
            {
                currentStatus.NetworkSentBytes = totalSent;
                currentStatus.NetworkReceivedBytes = totalRecv;
                healthLog.NetworkReceivedBytes = totalRecv;
                healthLog.NetworkSentBytes = totalSent;
            }

            // Build NetworkInterfaces JSON array for frontend display
            var interfaces = new List<object>();
            foreach (var adapter in adapters.EnumerateArray())
            {
                var adapterName = adapter.GetStringProperty("adapterName") ?? adapter.GetStringProperty("adapter_name") ?? "Unknown";
                var ipV4 = adapter.GetStringProperty("ipAddressV4") ?? adapter.GetStringProperty("ip_address_v4") ?? adapter.GetStringProperty("ipAddress");
                var mac = adapter.GetStringProperty("macAddress") ?? adapter.GetStringProperty("mac_address");
                var isConnected = adapter.GetBooleanProperty("isConnected") ?? adapter.GetBooleanProperty("is_connected") ?? false;

                interfaces.Add(new
                {
                    adapter_name = adapterName,
                    ip_address = ipV4,
                    mac_address = mac,
                    status = isConnected ? "connected" : "disconnected"
                });
            }
            currentStatus.NetworkInterfaces = interfaces.Count > 0 ? JsonSerializer.Serialize(interfaces) : null;

            await _dbContext.SaveChangesAsync();

            _logger.LogDebug("NetworkProcessor: Processed network telemetry for machine {MachineId}", machine.Id);
        }
    }
}
