namespace DeskGuardBackend.Entities
{
    /// <summary>
    /// Alert rule defining thresholds that trigger alerts.
    /// Maps to 'alert_rules' table.
    /// </summary>
    public class AlertRule
    {
        public long Id { get; set; }
        public long? CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MetricType { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public decimal ThresholdValue { get; set; }
        public string Severity { get; set; } = "medium";
        public bool IsEnabled { get; set; } = true;
        public int? CooldownMinutes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Company? Company { get; set; }
        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }

    /// <summary>
    /// Generated alert with full lifecycle support (open → acknowledged → resolved).
    /// Maps to 'alerts' table.
    /// </summary>
    public class Alert
    {
        public long Id { get; set; }
        public long CompanyId { get; set; }
        public long MachineId { get; set; }
        public long? AlertRuleId { get; set; }

        /// <summary>Alert type key: cpu_high, ram_high, disk_low, firewall_disabled, antivirus_disabled, offline.</summary>
        public string? AlertType { get; set; }

        /// <summary>Target resource: CPU, RAM, Drive C:, Firewall, Antivirus, Host.</summary>
        public string? Resource { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Severity { get; set; } = "medium";
        public string Status { get; set; } = "open";

        /// <summary>Current metric value when evaluated.</summary>
        public decimal? CurrentValue { get; set; }

        /// <summary>Configured threshold value that triggered alert.</summary>
        public decimal? ThresholdValue { get; set; }

        /// <summary>Maximum peak value recorded during ongoing incident.</summary>
        public decimal? MaxRecordedValue { get; set; }

        /// <summary>Timestamp when incident first occurred.</summary>
        public DateTime? FirstDetectedAt { get; set; }

        /// <summary>Timestamp of most recent telemetry cycle observing the incident.</summary>
        public DateTime? LastDetectedAt { get; set; }

        /// <summary>Total occurrences count while incident remains active.</summary>
        public int OccurrenceCount { get; set; } = 1;

        /// <summary>Total duration in seconds once resolved.</summary>
        public int? DurationSeconds { get; set; }

        public long? AcknowledgedBy { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public long? ResolvedBy { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNote { get; set; }
        public string? Metadata { get; set; } // JSON
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Company Company { get; set; } = null!;
        public Machine Machine { get; set; } = null!;
        public AlertRule? AlertRule { get; set; }
        public User? Acknowledger { get; set; }
        public User? Resolver { get; set; }
    }

    /// <summary>
    /// Hardware/software/security change tracking record.
    /// Maps to 'change_history' table.
    /// </summary>
    public class ChangeHistory
    {
        public long Id { get; set; }
        public long CompanyId { get; set; }
        public long MachineId { get; set; }

        /// <summary>Category: hardware, software, security, network, peripheral, configuration.</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Change type: added, removed, modified, updated, enabled, disabled, connected, disconnected.</summary>
        public string ChangeType { get; set; } = string.Empty;

        public string? ItemIdentifier { get; set; }
        public string? ItemLabel { get; set; }
        public string? PreviousValue { get; set; }
        public string? NewValue { get; set; }
        public string? Description { get; set; }
        public string? Severity { get; set; }
        public string? Status { get; set; }
        public string? Metadata { get; set; } // JSON
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Company Company { get; set; } = null!;
        public Machine Machine { get; set; } = null!;
    }

    /// <summary>Hardware baseline snapshot for change detection. Maps to 'hardware_baselines'.</summary>
    public class HardwareBaseline
    {
        public long Id { get; set; }
        public long MachineId { get; set; }
        public string ComponentType { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public string? Label { get; set; }
        public string? CurrentValue { get; set; }
        public string? Metadata { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Machine Machine { get; set; } = null!;
    }

    /// <summary>Software baseline snapshot. Maps to 'software_baselines'.</summary>
    public class SoftwareBaseline
    {
        public long Id { get; set; }
        public long MachineId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Version { get; set; }
        public string? Publisher { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Machine Machine { get; set; } = null!;
    }

    /// <summary>Security baseline snapshot. Maps to 'security_baselines'.</summary>
    public class SecurityBaseline
    {
        public long Id { get; set; }
        public long MachineId { get; set; }
        public string SettingType { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public string? Label { get; set; }
        public string? CurrentValue { get; set; }
        public string? Metadata { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Machine Machine { get; set; } = null!;
    }

    /// <summary>Configuration baseline snapshot. Maps to 'configuration_baselines'.</summary>
    public class ConfigurationBaseline
    {
        public long Id { get; set; }
        public long MachineId { get; set; }
        public string SettingType { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public string? Label { get; set; }
        public string? CurrentValue { get; set; }
        public string? Metadata { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Machine Machine { get; set; } = null!;
    }
}
