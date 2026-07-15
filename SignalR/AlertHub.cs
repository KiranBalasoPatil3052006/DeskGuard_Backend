using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace DeskGuardBackend.SignalR
{
    public class AlertHub : Hub
    {
        private readonly ILogger<AlertHub> _logger;

        // Tracks active connections per company for deduplication and group management
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> CompanyConnections = new();

        public AlertHub(ILogger<AlertHub> logger)
        {
            _logger = logger;
        }

        public async Task JoinCompanyGroup(string companyId)
        {
            if (string.IsNullOrEmpty(companyId)) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, $"Company_{companyId}");
            TrackConnection("Company", companyId);

            _logger.LogInformation("Connection {ConnectionId} joined Company_{CompanyId}", Context.ConnectionId, companyId);
        }

        public async Task LeaveCompanyGroup(string companyId)
        {
            if (string.IsNullOrEmpty(companyId)) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Company_{companyId}");
            UntrackConnection("Company", companyId);

            _logger.LogInformation("Connection {ConnectionId} left Company_{CompanyId}", Context.ConnectionId, companyId);
        }

        public override async Task OnConnectedAsync()
        {
            // Auto-join company group from query string (set by frontend during connection)
            var companyId = Context.GetHttpContext()?.Request.Query["company_id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(companyId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Company_{companyId}");
                TrackConnection("Company", companyId);
                _logger.LogInformation("Connection {ConnectionId} auto-joined Company_{CompanyId} via query", Context.ConnectionId, companyId);
            }

            _logger.LogInformation("Real-time client connected: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Clean up all tracked groups for this connection
            foreach (var (prefix, connections) in CompanyConnections)
            {
                connections.TryRemove(Context.ConnectionId, out _);
                // Remove empty inner dictionaries to prevent memory leak.
                if (connections.IsEmpty)
                {
                    CompanyConnections.TryRemove(prefix, out _);
                }
            }

            _logger.LogInformation("Real-time client disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public static int GetCompanyConnectionCount(string companyId)
        {
            if (CompanyConnections.TryGetValue($"Company_{companyId}", out var connections))
            {
                return connections.Count;
            }
            return 0;
        }

        private void TrackConnection(string prefix, string id)
        {
            var key = $"{prefix}_{id}";
            var connections = CompanyConnections.GetOrAdd(key, _ => new ConcurrentDictionary<string, byte>());
            connections.TryAdd(Context.ConnectionId, 0);
        }

        private void UntrackConnection(string prefix, string id)
        {
            var key = $"{prefix}_{id}";
            if (CompanyConnections.TryGetValue(key, out var connections))
            {
                connections.TryRemove(Context.ConnectionId, out _);
            }
        }
    }
}
