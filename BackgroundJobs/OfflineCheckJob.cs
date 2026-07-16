using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DeskGuardBackend.BackgroundJobs
{
    /// <summary>
    /// Background service that runs periodically (every 60 seconds) to identify
    /// and mark offline machines if their heartbeat has expired.
    /// Also generates alerts when machines go offline.
    /// Acts as a built-in replacement for the Laravel scheduler.
    /// </summary>
    public class OfflineCheckJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OfflineCheckJob> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(60);
        private readonly SemaphoreSlim _executionLock = new SemaphoreSlim(1, 1);

        public OfflineCheckJob(IServiceProvider serviceProvider, ILogger<OfflineCheckJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OfflineCheckJob starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                if (await _executionLock.WaitAsync(0))
                {
                    try
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var machineService = scope.ServiceProvider.GetRequiredService<IMachineService>();
                            var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();
                            var dbContext = scope.ServiceProvider.GetRequiredService<DeskGuardBackend.Data.DeskGuardDbContext>();

                            // Mark offline machines and get list of those newly marked
                            var newlyOffline = await machineService.MarkOfflineMachinesAsync();

                            if (newlyOffline > 0)
                            {
                                _logger.LogInformation("OfflineCheckJob updated: {Count} machines marked offline.", newlyOffline);

                                // Create alerts for newly offline machines
                                var threshold = DateTime.UtcNow.AddMinutes(-5);
                                var offlineMachines = await dbContext.Machines
                                    .Where(m => !m.IsOnline && m.LastHeartbeatAt != null && m.LastHeartbeatAt < threshold)
                                    .ToListAsync();

                                foreach (var machine in offlineMachines)
                                {
                                    await alertService.CreateMachineOfflineAlertAsync(machine);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred in OfflineCheckJob execution loop");
                    }
                    finally
                    {
                        _executionLock.Release();
                    }
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("OfflineCheckJob stopped.");
        }
    }
}
