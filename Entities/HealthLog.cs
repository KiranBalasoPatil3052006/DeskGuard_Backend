/// <summary>
/// Stores historical health metric snapshots collected from agents.
/// Each row represents one collection cycle (one payload from one machine).
/// Used for performance charts and historical analysis on the dashboard.
/// Maps to the 'health_logs' table in PostgreSQL.
/// </summary>
namespace DeskGuardBackend.Entities
{
    public class HealthLog
    {
        public long Id { get; set; }
        public long? CompanyId { get; set; }
        public long MachineId { get; set; }

        // CPU
        public decimal? CpuPercentage { get; set; }
        public decimal? CpuTemperature { get; set; }
        public decimal? CpuClockSpeed { get; set; }

        // Memory
        public long? RamTotalBytes { get; set; }
        public long? RamUsedBytes { get; set; }
        public long? RamAvailableBytes { get; set; }
        public decimal? RamPercentage { get; set; }

        // Disk
        public long? DiskTotalBytes { get; set; }
        public long? DiskUsedBytes { get; set; }
        public long? DiskFreeBytes { get; set; }
        public decimal? DiskPercentage { get; set; }

        // Battery
        public decimal? BatteryPercentage { get; set; }
        public bool? BatteryChargingStatus { get; set; }

        // Network
        public long? NetworkReceivedBytes { get; set; }
        public long? NetworkSentBytes { get; set; }

        /// <summary>Timestamp when the agent collected this data.</summary>
        public DateTime? CollectedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Machine Machine { get; set; } = null!;
        public Company? Company { get; set; }
    }
}
