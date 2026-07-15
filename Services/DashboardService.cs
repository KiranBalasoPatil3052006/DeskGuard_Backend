using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeskGuardBackend.Data;
using DeskGuardBackend.Entities;
using DeskGuardBackend.Services.Interfaces;

namespace DeskGuardBackend.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly DeskGuardDbContext _dbContext;
        private readonly ICacheService _cache;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            DeskGuardDbContext dbContext,
            ICacheService cache,
            ILogger<DashboardService> logger)
        {
            _dbContext = dbContext;
            _cache = cache;
            _logger = logger;
        }

        public async Task<object> GetCompanyDashboardAsync(long companyId)
        {
            try
            {
                // Machine counts (simple indexed COUNT queries — fast even without views)
                var totalMachines = await _dbContext.Machines.CountAsync(m => m.CompanyId == companyId);
                var onlineMachines = await _dbContext.Machines.CountAsync(m => m.CompanyId == companyId && m.IsOnline);

                // Alert counts (total & critical open/acknowledged)
                var totalAlerts = await _dbContext.Alerts.CountAsync(a => a.CompanyId == companyId);
                var criticalAlerts = await _dbContext.Alerts
                    .CountAsync(a => a.CompanyId == companyId && a.Severity == "critical" && (a.Status == "open" || a.Status == "acknowledged"));

                var cards = new
                {
                    total_machines = totalMachines,
                    online_count = onlineMachines,
                    offline_count = totalMachines - onlineMachines,
                    total_alerts = totalAlerts,
                    critical_alerts = criticalAlerts
                };

                var chartData = await GetCombinedChartDataInternalAsync(companyId, 24);
                var alertChart = await GetAlertChartDataAsync(companyId, 7);

                // Aggregated health metrics across all company machines
                var statuses = await _dbContext.MachineCurrentStatuses
                    .Where(s => s.Machine.CompanyId == companyId)
                    .ToListAsync();

                var cpuAvg = statuses.Where(s => s.CpuPercentage.HasValue).Average(s => (double?)s.CpuPercentage);
                var ramAvg = statuses.Where(s => s.RamPercentage.HasValue).Average(s => (double?)s.RamPercentage);
                var diskAvg = statuses.Where(s => s.DiskPercentage.HasValue).Average(s => (double?)s.DiskPercentage);

                double? cpuPct = cpuAvg.HasValue ? Math.Round(cpuAvg.Value, 1) : null;
                double? ramPct = ramAvg.HasValue ? Math.Round(ramAvg.Value, 1) : null;
                double? diskPct = diskAvg.HasValue ? Math.Round(diskAvg.Value, 1) : null;

                return new
                {
                    cards = cards,
                    cpu_percentage = cpuPct,
                    ram_percentage = ramPct,
                    disk_percentage = diskPct,
                    network_percentage = (double?)null,
                    cpu_chart = chartData.Cpu,
                    ram_chart = chartData.Ram,
                    alert_chart = alertChart
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DashboardService::GetCompanyDashboardAsync failed for company: {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<object> GetEmployeeDashboardAsync(long userId)
        {
            try
            {
                var machine = await _dbContext.Machines
                    .Include(m => m.CurrentStatus)
                    .FirstOrDefaultAsync(m => m.UserId == userId);

                var recentAlerts = new List<Alert>();
                if (machine != null)
                {
                    recentAlerts = await _dbContext.Alerts
                        .Where(a => a.MachineId == machine.Id)
                        .OrderByDescending(a => a.CreatedAt)
                        .Take(10)
                        .ToListAsync();
                }

                return new
                {
                    machine = machine,
                    current_status = machine?.CurrentStatus,
                    recent_alerts = recentAlerts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DashboardService::GetEmployeeDashboardAsync failed for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<object> GetCpuChartDataAsync(long companyId, int hours = 24)
        {
            var data = await GetCombinedChartDataInternalAsync(companyId, hours);
            return data.Cpu;
        }

        public async Task<object> GetRamChartDataAsync(long companyId, int hours = 24)
        {
            var data = await GetCombinedChartDataInternalAsync(companyId, hours);
            return data.Ram;
        }

        public async Task<object> GetAlertChartDataAsync(long companyId, int days = 7)
        {
            try
            {
                var since = DateTime.UtcNow.AddDays(-days).Date;

                var alertCounts = await _dbContext.Database
                    .SqlQuery<DailyAlertCountRow>(
                        $"SELECT alert_date AS Date, severity AS Severity, alert_count AS Count FROM mv_daily_alerts WHERE company_id = {companyId} AND alert_date >= {since} ORDER BY alert_date")
                    .ToListAsync();

                var labels = new List<string>();
                var critical = new List<int>();
                var warning = new List<int>();
                var info = new List<int>();

                for (int i = 0; i <= days; i++)
                {
                    var date = DateTime.UtcNow.AddDays(-days + i).Date;
                    labels.Add(date.ToString("ddd MMM dd"));

                    critical.Add(alertCounts.FirstOrDefault(c => c.Date == date && c.Severity == "critical")?.Count ?? 0);
                    warning.Add(alertCounts.FirstOrDefault(c => c.Date == date && c.Severity == "warning")?.Count ?? 0);
                    info.Add(alertCounts.FirstOrDefault(c => c.Date == date && c.Severity == "info")?.Count ?? 0);
                }

                return new
                {
                    labels = labels,
                    datasets = new[]
                    {
                        new { label = "Critical", data = critical },
                        new { label = "Warning", data = warning },
                        new { label = "Info", data = info }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DashboardService::GetAlertChartDataAsync failed for company: {CompanyId}", companyId);
                throw;
            }
        }

        private class CombinedChartData
        {
            public object Cpu { get; set; } = null!;
            public object Ram { get; set; } = null!;
        }

        private class HourlyHealthRow
        {
            public long MachineId { get; set; }
            public DateTime HourBucket { get; set; }
            public double? AvgCpu { get; set; }
            public double? AvgRam { get; set; }
            public int DataPoints { get; set; }
        }

        private class DailyAlertCountRow
        {
            public DateTime Date { get; set; }
            public string Severity { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        private async Task<CombinedChartData> GetCombinedChartDataInternalAsync(long companyId, int hours)
        {
            var cacheKey = $"dashboard_chart_{companyId}_{hours}";

            var cachedData = await _cache.GetAsync<CombinedChartData>(cacheKey);
            if (cachedData != null)
            {
                return cachedData;
            }

            try
            {
                var since = DateTime.UtcNow.AddHours(-hours);

                // Query from materialized view mv_hourly_health (refreshed every 60s)
                // Provides pre-aggregated hourly CPU/RAM per machine for the last 48 hours
                var aggregatedData = await _dbContext.Database
                    .SqlQuery<HourlyHealthRow>(
                        $"""SELECT machine_id, hour_bucket, avg_cpu, avg_ram, data_points FROM mv_hourly_health WHERE company_id = {companyId} AND hour_bucket >= {since} ORDER BY hour_bucket""")
                    .ToListAsync();

                if (aggregatedData.Count == 0)
                {
                    var emptyResult = new CombinedChartData
                    {
                        Cpu = new { labels = Array.Empty<string>(), datasets = new object[] { new { label = "No Data", data = Array.Empty<float?>() } } },
                        Ram = new { labels = Array.Empty<string>(), datasets = new object[] { new { label = "No Data", data = Array.Empty<float?>() } } }
                    };
                    await _cache.SetAsync(cacheKey, emptyResult, TimeSpan.FromMinutes(5));
                    return emptyResult;
                }

                // Get machine names (only for machines that have data)
                var machineIds = aggregatedData.Select(x => x.MachineId).Distinct().ToList();
                var machines = await _dbContext.Machines
                    .AsNoTracking()
                    .Where(m => machineIds.Contains(m.Id))
                    .Select(m => new { m.Id, Name = m.Hostname ?? m.DeviceName ?? $"Machine {m.Id}" })
                    .ToDictionaryAsync(m => m.Id, m => m.Name);

                // Compute overall hourly average for company-wide trend
                var companyHourly = aggregatedData
                    .GroupBy(x => x.HourBucket)
                    .Select(g => new
                    {
                        Hour = g.Key,
                        AvgCpu = (float?)Math.Round(g.Average(x => x.AvgCpu) ?? 0, 1),
                        AvgRam = (float?)Math.Round(g.Average(x => x.AvgRam) ?? 0, 1)
                    })
                    .OrderBy(x => x.Hour)
                    .ToList();

                // Per-machine breakdown for the top 5 machines by data points
                var topMachineIds = aggregatedData
                    .GroupBy(x => x.MachineId)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key)
                    .ToHashSet();

                var topMachineData = aggregatedData.Where(x => topMachineIds.Contains(x.MachineId)).ToList();

                var labels = companyHourly.Select(x => x.Hour.ToString("HH:00")).ToList();
                var hourBuckets = companyHourly.Select(x => x.Hour).ToList();

                // Build company-average dataset
                var datasets = new List<object>
                {
                    new { label = "Company Avg (CPU)", data = companyHourly.Select(x => x.AvgCpu).ToArray() },
                };

                // Build per-machine CPU datasets
                foreach (var mid in topMachineIds)
                {
                    var machineName = machines.GetValueOrDefault(mid, $"Machine {mid}");
                    var machinePoints = topMachineData.Where(x => x.MachineId == mid).ToList();
                    var cpuData = hourBuckets.Select(h =>
                    {
                        var match = machinePoints.FirstOrDefault(mp => mp.HourBucket == h);
                        return match?.AvgCpu != null ? (float?)Math.Round(match.AvgCpu.Value, 1) : null;
                    }).ToArray();
                    datasets.Add(new { label = $"{machineName} (CPU)", data = cpuData });
                }

                // Build RAM datasets (same structure)
                var ramDatasets = new List<object>
                {
                    new { label = "Company Avg (RAM)", data = companyHourly.Select(x => x.AvgRam).ToArray() },
                };

                foreach (var mid in topMachineIds)
                {
                    var machineName = machines.GetValueOrDefault(mid, $"Machine {mid}");
                    var machinePoints = topMachineData.Where(x => x.MachineId == mid).ToList();
                    var ramData = hourBuckets.Select(h =>
                    {
                        var match = machinePoints.FirstOrDefault(mp => mp.HourBucket == h);
                        return match?.AvgRam != null ? (float?)Math.Round(match.AvgRam.Value, 1) : null;
                    }).ToArray();
                    ramDatasets.Add(new { label = $"{machineName} (RAM)", data = ramData });
                }

                var result = new CombinedChartData
                {
                    Cpu = new { labels, datasets = datasets.ToArray() },
                    Ram = new { labels, datasets = ramDatasets.ToArray() }
                };

                // Cache for 5 minutes (L1: IMemoryCache, L2: Redis if configured)
                await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DashboardService::GetCombinedChartDataInternalAsync failed for company: {CompanyId}", companyId);
                throw;
            }
        }
    }
}
