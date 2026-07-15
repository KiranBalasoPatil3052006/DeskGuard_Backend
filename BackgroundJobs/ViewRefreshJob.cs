using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using DeskGuardBackend.Data;

namespace DeskGuardBackend.BackgroundJobs
{
    public class ViewRefreshJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ViewRefreshJob> _logger;
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(60);

        public ViewRefreshJob(
            IServiceProvider serviceProvider,
            ILogger<ViewRefreshJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ViewRefreshJob started, refreshing every {Interval}s", RefreshInterval.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<DeskGuardDbContext>();

                    await dbContext.Database.ExecuteSqlRawAsync("REFRESH MATERIALIZED VIEW mv_hourly_health;", stoppingToken);
                    await dbContext.Database.ExecuteSqlRawAsync("REFRESH MATERIALIZED VIEW mv_daily_alerts;", stoppingToken);
                    await dbContext.Database.ExecuteSqlRawAsync("REFRESH MATERIALIZED VIEW mv_company_summary;", stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ViewRefreshJob: materialized view refresh failed, will retry in {Interval}s", RefreshInterval.TotalSeconds);
                }

                await Task.Delay(RefreshInterval, stoppingToken);
            }

            _logger.LogInformation("ViewRefreshJob stopped");
        }
    }
}
