namespace DeskGuardBackend.Entities
{
    public class AlertProfile
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public AlertThreshold? Threshold { get; set; }
        public ICollection<Company> AssignedCompanies { get; set; } = new List<Company>();
        public ICollection<Machine> CustomAssignedMachines { get; set; } = new List<Machine>();
    }

    public class AlertThreshold
    {
        public long Id { get; set; }
        public long ProfileId { get; set; }

        // Performance
        public decimal? CpuWarningPercent { get; set; }
        public decimal? CpuCriticalPercent { get; set; }
        public int? CpuWarningDurationMinutes { get; set; }
        public decimal? RamWarningPercent { get; set; }
        public decimal? RamCriticalPercent { get; set; }
        public int? RamWarningDurationMinutes { get; set; }
        public decimal? CpuTempWarning { get; set; }
        public decimal? CpuTempCritical { get; set; }

        // Storage
        public decimal? DiskWarningPercent { get; set; }
        public decimal? DiskCriticalPercent { get; set; }
        public bool? DiskSmartWarningEnabled { get; set; }
        public bool? DiskSmartCriticalEnabled { get; set; }

        // Availability
        public int? OfflineWarningMinutes { get; set; }
        public int? OfflineCriticalMinutes { get; set; }

        // Authentication
        public int? FailedLoginWarningCount { get; set; }
        public int? FailedLoginCriticalCount { get; set; }

        // Network
        public int? NetworkDisconnectWarningCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public AlertProfile Profile { get; set; } = null!;
    }
}
