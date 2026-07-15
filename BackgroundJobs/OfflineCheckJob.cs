using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DeskGuardBackend.BackgroundJobs
{
    /// <summary>
    /// Background service that runs periodically (every 60 seconds) to identify
    /// and mark offline machines if their heartbeat has expired.
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
                            var offlineCount = await machineService.MarkOfflineMachinesAsync();
                            if (offlineCount > 0)
                            {
                                _logger.LogInformation("OfflineCheckJob updated: {Count} machines marked offline.", offlineCount);
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
