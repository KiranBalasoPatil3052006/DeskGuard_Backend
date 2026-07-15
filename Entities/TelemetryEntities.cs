namespace DeskGuardBackend.Entities
{
    /// <summary>
    /// Hardware inventory snapshot for a machine.
    /// Stores CPU model, RAM details, BIOS, motherboard info.
    /// Maps to 'hardware_inventory' table.
    /// </summary>
    public class HardwareInventory
    {
        public long Id { get; set; }
        public long MachineId { get; set; }
        public string? CpuModel { get; set; }
        public int? CpuCores { get; set; }
        public int? CpuThreads { get; set; }
        public decimal? CpuMaxClockSpeed { get; set; }
        public string? CpuArchitecture { get; set; }
        public long? TotalRamBytes { get; set; }
        public int? RamSlots { get; set; }
        public string? RamType { get; set; }
        public string? RamSpeed { get; set; }
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
        public string? BiosVersion { get; set; }
        public string? MotherboardModel { get; set; }
        public string? GpuName { get; set; }
        public string? GpuDriverVersion { get; set; }
        public long? GpuMemoryBytes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Machine Machine { get; set; } = null!;
    }

    /// <summary>
    /// Software inventory record. One row per installed application per machine.
    /// Maps to 'software_inventory' table.
    /// </summary>
    public class SoftwareInventory
    {
        public long Id { get; set; }
        public long MachineId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Version { get; set; }
        public string? Publisher { get; set; }
        public string? InstallDate { get; set; }
        public string? InstallLocation { get; set; }
        public long? EstimatedSize { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Machine Machine { get; set; } = null!;
    }

    /// <summary>
    /// Antivirus protection status for a machine.
    /// Maps to 'antivirus_status' table.
    /// </summary>
    public class AntivirusStatus
    {
        public long Id { get; set; }
        public long MachineId { get; set; }
        public string? DisplayName { get; set; }
        public bool? IsRealTimeProtectionEnabled { get; set; }
        public bool? IsSignatureUpToDate { get; set; }
        public string? ProductVersion { get; set; }
        public string? ProductState { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Machine Machine { get; set; } = null!;
    }

    /// <summary>
    /// Windows Firewall status for a machine.
    /// Maps to 'firewall_status' table.
    /// </summary>
    public class FirewallStatus
    {
        public long Id { get; set; }
        public long MachineId { get; set; }
        public string? DisplayName { get; set; }
        public bool? IsDomainFirewallEnabled { get; set; }
        public bool? IsPrivateFirewallEnabled { get; set; }
        public bool? IsPublicFirewallEnabled { get; set; }
        public string? ActiveProfile { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Machine Machine { get; set; } = null!;
    }

    /// <summary>
    /// Login/logout activity events from the Windows Security log.
    /// Maps to 'login_activities' table.
    /// </summary>
    public class LoginActivity
    {
        public long Id { get; set; }
        public long MachineId { get; set; }
        public long? CompanyId { get; set; }
        public string? Username { get; set; }
        public string? EventType { get; set; }
        public bool IsSuccess { get; set; }
        public string? LogonType { get; set; }
        public string? SourceIp { get; set; }
        public string? SessionId { get; set; }
        public DateTime? EventTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Machine Machine { get; set; } = null!;
    }

    /// <summary>
    /// USB device connect/disconnect activity.
    /// Maps to 'usb_activities' table.
    /// </summary>
    public class UsbActivity
    {
        public long Id { get; set; }
        public long MachineId { get; set; }
        public long? CompanyId { get; set; }
        public string? DeviceName { get; set; }
        public string? DeviceSerial { get; set; }
        public string? EventType { get; set; }
        public DateTime? EventTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Machine Machine { get; set; } = null!;
    }

    /// <summary>
    /// Windows service status snapshot. One row per service per machine.
    /// Maps to 'windows_services' table.
    /// </summary>
    public class WindowsService
    {
        public long Id { get; set; }
        public long MachineId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? Status { get; set; }
        public string? StartType { get; set; }
        public string? ServiceType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Machine Machine { get; set; } = null!;
    }

    /// <summary>
    /// Windows Update status. One row per update per machine.
    /// Maps to 'windows_updates' table.
    /// </summary>
    public class WindowsUpdate
    {
        public long Id { get; set; }
        public long MachineId { get; set; }
        public string? UpdateTitle { get; set; }
        public string? KbArticleId { get; set; }
        public bool IsInstalled { get; set; }
        public bool? IsMandatory { get; set; }
        public string? Severity { get; set; }
        public DateTime? InstalledOn { get; set; }
        public int? PendingUpdateCount { get; set; }
        public DateTime? LastCheckedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Machine Machine { get; set; } = null!;
    }

    /// <summary>
    /// Windows event log entries. Maps to 'event_logs' table.
    /// </summary>
    public class EventLog
    {
        public long Id { get; set; }
        public long MachineId { get; set; }
        public string? LogName { get; set; }
        public string? Source { get; set; }
        public int? EventId { get; set; }
        public string? Level { get; set; }
        public string? Message { get; set; }
        public DateTime? TimeGenerated { get; set; }
        public string? Category { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Machine Machine { get; set; } = null!;
    }

    /// <summary>
    /// Auto-start programs configured on a machine.
    /// Maps to 'startup_programs' table.
    /// </summary>
    public class StartupProgram
    {
        public long Id { get; set; }
        public long MachineId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Command { get; set; }
        public string? Location { get; set; }
        public string? User { get; set; }
        public string? RegistryKey { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Machine Machine { get; set; } = null!;
    }

    /// <summary>
    /// Running process snapshot. Maps to 'process_logs' table.
    /// </summary>
    public class ProcessLog
    {
        public long Id { get; set; }
        public long MachineId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public int? ProcessId { get; set; }
        public decimal? CpuUsagePercentage { get; set; }
        public long? WorkingSetBytes { get; set; }
        public decimal? MemoryUsageMb { get; set; }
        public int? ThreadCount { get; set; }
        public string? UserName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Machine Machine { get; set; } = null!;
    }

    /// <summary>
    /// Connected peripheral devices. Maps to 'machine_connected_devices' table.
    /// </summary>
    public class MachineConnectedDevice
    {
        public long Id { get; set; }
        public long MachineId { get; set; }
        public string? DeviceName { get; set; }
        public string? DeviceType { get; set; }
        public string? DeviceId { get; set; }
        public string? Status { get; set; }
        public string? Manufacturer { get; set; }
        public string? DriverVersion { get; set; }
        public string? ConnectionType { get; set; }
        public DateTime? LastSeen { get; set; }
        public bool? HasProblem { get; set; }
        public string? ProblemDescription { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Machine Machine { get; set; } = null!;
    }

    /// <summary>
    /// Device connect/disconnect events. Maps to 'device_events' table.
    /// </summary>
    public class DeviceEvent
    {
        public long Id { get; set; }
        public long MachineId { get; set; }
        public string? DeviceName { get; set; }
        public string? DeviceType { get; set; }
        public string? DeviceId { get; set; }
        public string? EventType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Machine Machine { get; set; } = null!;
    }

    /// <summary>
    /// Network adapter details for a machine. Maps to 'machine_network_adapters' table.
    /// </summary>
    public class MachineNetworkAdapter
    {
        public long Id { get; set; }
        public long MachineId { get; set; }
        public string AdapterName { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? MacAddress { get; set; }
        public string? AdapterType { get; set; }
        public long? Speed { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Machine Machine { get; set; } = null!;
    }

    /// <summary>
    /// Disk drive details for a machine. Maps to 'machine_disks' table.
    /// </summary>
    public class MachineDisk
    {
        public long Id { get; set; }
        public long MachineId { get; set; }
        public string? DriveLetter { get; set; }
        public string? VolumeLabel { get; set; }
        public string? FileSystem { get; set; }
        public string? DriveType { get; set; }
        public decimal? TotalGb { get; set; }
        public decimal? UsedGb { get; set; }
        public decimal? FreeGb { get; set; }
        public string? HealthStatus { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Machine Machine { get; set; } = null!;
    }
}
