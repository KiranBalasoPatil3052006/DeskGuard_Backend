/// <summary>
/// Stores the latest real-time hardware metrics for each machine (one row per machine).
/// Updated on every agent health payload. Provides instant dashboard access
/// without querying the historical health_logs table.
/// Maps to the 'machine_current_status' table in PostgreSQL.
/// </summary>
namespace DeskGuardBackend.Entities
{
    public class MachineCurrentStatus
    {
        public long Id { get; set; }
        public long MachineId { get; set; }
        public long? CompanyId { get; set; }

        // CPU metrics
        public decimal? CpuPercentage { get; set; }
        public decimal? CpuTemperature { get; set; }
        public decimal? CpuClockSpeed { get; set; }
        public int? CpuCoreCount { get; set; }

        // Memory metrics
        public long? RamTotalBytes { get; set; }
        public long? RamUsedBytes { get; set; }
        public long? RamAvailableBytes { get; set; }
        public decimal? RamPercentage { get; set; }

        // Disk metrics
        public long? DiskTotalBytes { get; set; }
        public long? DiskUsedBytes { get; set; }
        public long? DiskFreeBytes { get; set; }
        public decimal? DiskPercentage { get; set; }
        public string? DiskHealthStatus { get; set; }

        // Battery metrics
        public decimal? BatteryPercentage { get; set; }
        public bool? BatteryChargingStatus { get; set; }
        public decimal? BatteryWearLevel { get; set; }
        public bool? BatteryIsPresent { get; set; }
        public long? BatteryDesignCapacity { get; set; }
        public long? BatteryFullChargeCapacity { get; set; }

        // Network metrics
        public long? NetworkReceivedBytes { get; set; }
        public long? NetworkSentBytes { get; set; }

        // Security fields (added in later migration)
        public string? AntivirusName { get; set; }
        public bool? AntivirusEnabled { get; set; }
        public bool? FirewallEnabled { get; set; }
        public string? NetworkInterfaces { get; set; } // JSON string

        public bool OnlineStatus { get; set; }
        public DateTime? LastCollectedAt { get; set; }
        public DateTime? CollectedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Machine Machine { get; set; } = null!;
    }
}
